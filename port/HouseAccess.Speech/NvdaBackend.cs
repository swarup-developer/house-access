using System;
using System.Runtime.InteropServices;

namespace HouseAccess.Speech;

public sealed class NvdaBackend : ITtsBackend
{
	private const string Dll = "nvdaControllerClient.dll";

	private bool _ok;

	public string Name => "NVDA";

	[DllImport("nvdaControllerClient.dll", CharSet = CharSet.Unicode, EntryPoint = "nvdaController_testIfRunning")]
	private static extern int TestIfRunning();

	[DllImport("nvdaControllerClient.dll", CharSet = CharSet.Unicode, EntryPoint = "nvdaController_speakText")]
	private static extern int SpeakText([MarshalAs(UnmanagedType.LPWStr)] string text);

	[DllImport("nvdaControllerClient.dll", CharSet = CharSet.Unicode, EntryPoint = "nvdaController_cancelSpeech")]
	private static extern int CancelSpeech();

	[DllImport("nvdaControllerClient.dll", CharSet = CharSet.Unicode, EntryPoint = "nvdaController_brailleMessage")]
	private static extern int BrailleMessage([MarshalAs(UnmanagedType.LPWStr)] string text);

	public bool Probe()
	{
		if (!NativeProbe.Preload("nvdaControllerClient.dll"))
		{
			return false;
		}
		try
		{
			_ok = TestIfRunning() == 0;
		}
		catch (Exception ex)
		{
			Log.Warn("[TTS] NVDA probe failed: " + ex.Message);
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
			if (interrupt)
			{
				CancelSpeech();
			}
			SpeakText(text);
		}
		catch (Exception ex)
		{
			_ok = false;
			Log.Warn("[TTS] NVDA speak failed: " + ex.Message);
		}
	}

	public void Stop()
	{
		if (_ok)
		{
			try
			{
				CancelSpeech();
			}
			catch
			{
			}
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
			BrailleMessage(text);
		}
		catch
		{
		}
	}

	public void Shutdown()
	{
		Stop();
	}
}
