using System;
using System.Runtime.InteropServices;

namespace HouseAccess.Speech;

public sealed class ZdsrBackend : ITtsBackend
{
	private const string Dll = "ZDSRAPI.dll";

	private bool _ok;

	public string Name => "ZDSR";

	[DllImport("ZDSRAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	private static extern int InitTTS(int type, [MarshalAs(UnmanagedType.LPWStr)] string channelName, bool keyDownInterrupt);

	[DllImport("ZDSRAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "Speak")]
	private static extern int SpeakNative([MarshalAs(UnmanagedType.LPWStr)] string text, bool interrupt);

	[DllImport("ZDSRAPI.dll", CallingConvention = CallingConvention.StdCall)]
	private static extern int StopSpeak();

	public bool Probe()
	{
		if (!NativeProbe.Preload("ZDSRAPI.dll"))
		{
			return false;
		}
		try
		{
			_ok = InitTTS(0, "HouseAccess", keyDownInterrupt: false) == 0;
		}
		catch (Exception ex)
		{
			Log.Info("[TTS] ZDSR unavailable: " + ex.Message);
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
			SpeakNative(text, interrupt);
		}
		catch (Exception ex)
		{
			_ok = false;
			Log.Warn("[TTS] ZDSR speak failed: " + ex.Message);
		}
	}

	public void Stop()
	{
		if (_ok)
		{
			try
			{
				StopSpeak();
			}
			catch
			{
			}
		}
	}

	public void Braille(string text)
	{
	}

	public void Shutdown()
	{
		Stop();
	}
}
