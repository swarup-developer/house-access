using System;
using System.Collections.Generic;
using System.Text;
using MelonLoader;

namespace HouseAccess.Speech;

public static class Speaker
{
	private struct Pending
	{
		public string Text;

		public Pri Priority;
	}

	private static readonly object Gate = new object();

	private static readonly List<Pending> Buffer = new List<Pending>();

	private static readonly Dictionary<string, float> RecentlySaid = new Dictionary<string, float>();

	private static ITtsBackend _backend;

	private static float _lastFlush;

	private static float _now;

	private static readonly string[] Order = new string[5] { "auto", "nvda", "universalspeech", "zdsr", "sapi" };

	public static string BackendName => (_backend != null) ? _backend.Name : "none";

	public static float LastSpokeAt { get; private set; }

	public static void CycleBackend()
	{
		string value = (Prefs.Backend?.Value ?? "auto").Trim().ToLowerInvariant();
		int num = Array.IndexOf(Order, value);
		if (num < 0)
		{
			num = 0;
		}
		string text = Order[(num + 1) % Order.Length];
		Prefs.Backend.Value = text;
		try
		{
			MelonPreferences.Save();
		}
		catch
		{
		}
		Init();
		SayNow(BackendName + ". Speech engine " + text + ".");
		Log.Info($"[TTS] switched to {text}, resolved as {BackendName}.");
	}

	public static void Init()
	{
		switch ((Prefs.Backend?.Value ?? "auto").Trim().ToLowerInvariant())
		{
		case "universalspeech":
			_backend = Pick(new UniversalSpeechBackend());
			break;
		case "nvda":
			_backend = Pick(new NvdaBackend());
			break;
		case "zdsr":
			_backend = Pick(new ZdsrBackend());
			break;
		case "sapi":
			_backend = Pick(new SapiBackend());
			break;
		case "log":
			_backend = Pick(new NullBackend());
			break;
		default:
			_backend = Auto();
			break;
		}
		if (_backend == null)
		{
			_backend = new NullBackend();
		}
		if (_backend is SapiBackend sapiBackend)
		{
			sapiBackend.SetRate(Prefs.SapiRate.Value);
			sapiBackend.SetVolume(Prefs.SapiVolume.Value);
		}
		Log.Info("[TTS] Using backend: " + _backend.Name);
	}

	private static ITtsBackend Auto()
	{
		NvdaBackend nvdaBackend = new NvdaBackend();
		if (Try(nvdaBackend))
		{
			return nvdaBackend;
		}
		ZdsrBackend zdsrBackend = new ZdsrBackend();
		if (Try(zdsrBackend))
		{
			return zdsrBackend;
		}
		UniversalSpeechBackend universalSpeechBackend = new UniversalSpeechBackend();
		bool flag = Try(universalSpeechBackend);
		SapiBackend sapiBackend = new SapiBackend();
		bool flag2 = Try(sapiBackend);
		if (flag && !Prefs.PreferSapiWhenUnverified.Value)
		{
			if (universalSpeechBackend.Unverified)
			{
				Log.Warn("[TTS] UniversalSpeech loaded but no screen reader could be confirmed. If you hear nothing, press the speech test key, or set SpeechBackend to sapi.");
			}
			return universalSpeechBackend;
		}
		if (flag2)
		{
			if (flag)
			{
				Log.Info("[TTS] UniversalSpeech was unverified; using SAPI so speech is audible.");
			}
			return sapiBackend;
		}
		if (flag)
		{
			return universalSpeechBackend;
		}
		return null;
	}

	private static bool Try(ITtsBackend b)
	{
		try
		{
			return b.Probe();
		}
		catch (Exception ex)
		{
			Log.Warn("[TTS] " + b.Name + " probe threw: " + ex.Message);
			return false;
		}
	}

	private static ITtsBackend Pick(ITtsBackend b)
	{
		return Try(b) ? b : null;
	}

	public static void UseBackend(ITtsBackend b)
	{
		if (b != null)
		{
			_backend = b;
			Log.Info("[TTS] Switched backend to: " + b.Name);
		}
	}

	public static void Shutdown()
	{
		try
		{
			_backend?.Shutdown();
		}
		catch
		{
		}
		_backend = null;
	}

	public static void Say(string text, Pri priority = Pri.Normal)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		text = text.Trim();
		lock (Gate)
		{
			if (priority < Pri.Critical && IsDuplicate(text))
			{
				return;
			}
			Buffer.Add(new Pending
			{
				Text = text,
				Priority = priority
			});
		}
		if (priority == Pri.Critical)
		{
			Flush(interrupt: true);
		}
	}

	public static void SayNow(string text)
	{
		Say(text, Pri.Critical);
	}

	public static void SayParts(Pri priority, params string[] parts)
	{
		if (parts == null || parts.Length == 0)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string text in parts)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(text.Trim());
			}
		}
		if (stringBuilder.Length > 0)
		{
			Say(stringBuilder.ToString(), priority);
		}
	}

	public static void Stop()
	{
		lock (Gate)
		{
			Buffer.Clear();
		}
		try
		{
			_backend?.Stop();
		}
		catch
		{
		}
	}

	public static void Tick(float unscaledTime)
	{
		_now = unscaledTime;
		float num = (float)Math.Max(0, Prefs.CoalesceMs.Value) / 1000f;
		if (!(_now - _lastFlush < num))
		{
			Flush(interrupt: false);
		}
	}

	private static void Flush(bool interrupt)
	{
		bool flag = false;
		string text;
		lock (Gate)
		{
			if (Buffer.Count == 0)
			{
				_lastFlush = _now;
				return;
			}
			Pri pri = Pri.Low;
			foreach (Pending item in Buffer)
			{
				if (item.Priority > pri)
				{
					pri = item.Priority;
				}
			}
			flag = pri == Pri.Critical;
			StringBuilder stringBuilder = new StringBuilder();
			foreach (Pending item2 in Buffer)
			{
				if (pri <= Pri.Low || item2.Priority != Pri.Low)
				{
					if (stringBuilder.Length > 0)
					{
						stringBuilder.Append(". ");
					}
					stringBuilder.Append(item2.Text);
				}
			}
			Buffer.Clear();
			text = stringBuilder.ToString();
		}
		_lastFlush = _now;
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		try
		{
			_backend?.Speak(text, interrupt || flag);
			LastSpokeAt = _now;
			if (Prefs.BrailleOutput.Value)
			{
				_backend?.Braille(text);
			}
		}
		catch (Exception e)
		{
			Log.Error("Speech backend threw while speaking", e);
		}
	}

	private static bool IsDuplicate(string text)
	{
		float num = (float)Math.Max(0, Prefs.DedupeMs.Value) / 1000f;
		if (num <= 0f)
		{
			return false;
		}
		if (RecentlySaid.TryGetValue(text, out var value) && _now - value < num)
		{
			return true;
		}
		RecentlySaid[text] = _now;
		if (RecentlySaid.Count > 128)
		{
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, float> item in RecentlySaid)
			{
				if (_now - item.Value > num * 4f)
				{
					list.Add(item.Key);
				}
			}
			foreach (string item2 in list)
			{
				RecentlySaid.Remove(item2);
			}
		}
		return false;
	}
}
