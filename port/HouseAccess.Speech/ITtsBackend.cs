namespace HouseAccess.Speech;

public interface ITtsBackend
{
	string Name { get; }

	bool Probe();

	void Speak(string text, bool interrupt);

	void Stop();

	void Braille(string text);

	void Shutdown();
}
