using System;
using System.Runtime.InteropServices;

namespace HouseAccess.Speech;

public sealed class UniversalSpeechBackend : ITtsBackend
{
	private const string Dll = "UniversalSpeech.dll";

	private bool _ok;

	public string Name => "UniversalSpeech";

	public bool Unverified { get; private set; }

	[DllImport("UniversalSpeech.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	private static extern int speechSay([MarshalAs(UnmanagedType.LPWStr)] string str, int interrupt);

	[DllImport("UniversalSpeech.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern int speechStop();

	[DllImport("UniversalSpeech.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	private static extern int brailleDisplay([MarshalAs(UnmanagedType.LPWStr)] string str);

	[DllImport("UniversalSpeech.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern int speechSetValue(int what, int value);

	[DllImport("UniversalSpeech.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern int speechGetValue(int what);

	public bool Probe()
	{
		if (!NativeProbe.Preload("UniversalSpeech.dll"))
		{
			Log.Warn("[TTS] UniversalSpeech.dll was not found beside the game, so NVDA cannot be used. Copy it next to HouseParty.exe.");
			return false;
		}
		try
		{
			_ok = speechSay(string.Empty, 0) != 0;
			Unverified = _ok;
			if (!_ok)
			{
				Log.Info("[TTS] UniversalSpeech loaded but reported no available engine.");
			}
		}
		catch (Exception ex)
		{
			Log.Warn("[TTS] UniversalSpeech probe failed: " + ex.Message);
			_ok = false;
		}
		return _ok;
	}

	public void Speak(string text, bool interrupt)
	{
		if (!_ok || string.IsNullOrEmpty(text))
		{
			return;
		}
		try
		{
			speechSay(text, interrupt ? 1 : 0);
		}
		catch (Exception ex)
		{
			_ok = false;
			Log.Warn("[TTS] UniversalSpeech speak failed: " + ex.Message);
		}
	}

	public void Stop()
	{
		if (!_ok)
		{
			return;
		}
		try
		{
			speechStop();
		}
		catch
		{
		}
	}

	public void Braille(string text)
	{
		if (!_ok || string.IsNullOrEmpty(text))
		{
			return;
		}
		try
		{
			brailleDisplay(text);
		}
		catch
		{
		}
	}

	public int SetValue(int what, int value)
	{
		try
		{
			return speechSetValue(what, value);
		}
		catch
		{
			return 0;
		}
	}

	public int GetValue(int what)
	{
		try
		{
			return speechGetValue(what);
		}
		catch
		{
			return 0;
		}
	}

	public void Shutdown()
	{
		Stop();
	}
}
