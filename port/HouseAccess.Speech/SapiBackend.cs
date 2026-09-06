using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HouseAccess.Speech;

public sealed class SapiBackend : ITtsBackend
{
	private const int SVSFlagsAsync = 1;

	private const int SVSFPurgeBeforeSpeak = 2;

	private object _voice;

	private Type _type;

	public string Name => "SAPI5";

	public bool Probe()
	{
		try
		{
			_type = Type.GetTypeFromProgID("SAPI.SpVoice");
			if (_type == null)
			{
				return false;
			}
			_voice = Activator.CreateInstance(_type);
			return _voice != null;
		}
		catch (Exception ex)
		{
			Log.Info("[TTS] SAPI unavailable: " + ex.Message);
			_voice = null;
			return false;
		}
	}

	public void Speak(string text, bool interrupt)
	{
		if (_voice == null || string.IsNullOrEmpty(text))
		{
			return;
		}
		int num = 1 | (interrupt ? 2 : 0);
		try
		{
			_type.InvokeMember("Speak", BindingFlags.InvokeMethod, null, _voice, new object[2] { text, num });
		}
		catch (Exception ex)
		{
			Log.Warn("[TTS] SAPI speak failed: " + ex.Message);
		}
	}

	public void Stop()
	{
		if (_voice == null)
		{
			return;
		}
		try
		{
			_type.InvokeMember("Speak", BindingFlags.InvokeMethod, null, _voice, new object[2]
			{
				string.Empty,
				3
			});
		}
		catch
		{
		}
	}

	public void SetRate(int rate)
	{
		if (_voice == null)
		{
			return;
		}
		try
		{
			_type.InvokeMember("Rate", BindingFlags.SetProperty, null, _voice, new object[1] { Math.Max(-10, Math.Min(10, rate)) });
		}
		catch
		{
		}
	}

	public void SetVolume(int volume)
	{
		if (_voice == null)
		{
			return;
		}
		try
		{
			_type.InvokeMember("Volume", BindingFlags.SetProperty, null, _voice, new object[1] { Math.Max(0, Math.Min(100, volume)) });
		}
		catch
		{
		}
	}

	public void Braille(string text)
	{
	}

	public void Shutdown()
	{
		Stop();
		try
		{
			if (_voice != null && Marshal.IsComObject(_voice))
			{
				Marshal.ReleaseComObject(_voice);
			}
		}
		catch
		{
		}
		_voice = null;
	}
}
