using System;
using System.Collections.Generic;

namespace HouseAccess.Speech;

public static class SpeechTest
{
	private static readonly List<ITtsBackend> Queue = new List<ITtsBackend>();

	private static int _index = -1;

	private static float _nextAt;

	private static bool _running;

	public static bool Running => _running;

	public static void Start()
	{
		Stop();
		List<ITtsBackend> list = new List<ITtsBackend>
		{
			new UniversalSpeechBackend(),
			new NvdaBackend(),
			new ZdsrBackend(),
			new SapiBackend()
		};
		Queue.Clear();
		foreach (ITtsBackend item in list)
		{
			bool flag = false;
			try
			{
				flag = item.Probe();
			}
			catch (Exception ex)
			{
				Log.Warn("[TTS test] " + item.Name + " probe threw: " + ex.Message);
			}
			Log.Info("[TTS test] " + item.Name + ": " + (flag ? "available" : "unavailable"));
			if (flag)
			{
				Queue.Add(item);
			}
		}
		if (Queue.Count == 0)
		{
			Log.Warn("[TTS test] No speech backend is available at all.");
			return;
		}
		_running = true;
		_index = -1;
		_nextAt = 0f;
		Log.Info($"[TTS test] Testing {Queue.Count} backend(s). Listen for a numbered phrase.");
	}

	public static void Stop()
	{
		foreach (ITtsBackend item in Queue)
		{
			try
			{
				item.Stop();
			}
			catch
			{
			}
		}
		Queue.Clear();
		_running = false;
		_index = -1;
	}

	public static void Tick(float unscaledTime)
	{
		if (!_running || unscaledTime < _nextAt)
		{
			return;
		}
		_index++;
		if (_index >= Queue.Count)
		{
			Log.Info("[TTS test] Finished. Set SpeechBackend in MelonPreferences.cfg to the one you heard.");
			_running = false;
			return;
		}
		ITtsBackend ttsBackend = Queue[_index];
		string text = $"Test {_index + 1}. This is {ttsBackend.Name}. To keep this voice, set speech backend to {ConfigWord(ttsBackend)} in preferences.";
		Log.Info("[TTS test] Speaking through " + ttsBackend.Name + "...");
		try
		{
			ttsBackend.Speak(text, interrupt: true);
		}
		catch (Exception ex)
		{
			Log.Warn("[TTS test] " + ttsBackend.Name + " threw while speaking: " + ex.Message);
		}
		_nextAt = unscaledTime + 6f;
	}

	private static string ConfigWord(ITtsBackend b)
	{
		if (b is UniversalSpeechBackend)
		{
			return "universalspeech";
		}
		if (b is NvdaBackend)
		{
			return "nvda";
		}
		if (b is ZdsrBackend)
		{
			return "zdsr";
		}
		if (b is SapiBackend)
		{
			return "sapi";
		}
		return "auto";
	}
}
