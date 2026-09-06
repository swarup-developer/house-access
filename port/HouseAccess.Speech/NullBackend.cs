namespace HouseAccess.Speech;

public sealed class NullBackend : ITtsBackend
{
	public string Name => "Log only";

	public bool Probe()
	{
		return true;
	}

	public void Speak(string text, bool interrupt)
	{
		Log.Info("[SPEECH] " + text);
	}

	public void Stop()
	{
	}

	public void Braille(string text)
	{
	}

	public void Shutdown()
	{
	}
}
