using HouseAccess;
using HouseAccess.Speech;

namespace HouseAccess.Game;

// WORKAROUND: see ConsoleBuilder.cs. Console output reading is disabled on this
// game build because the console classes House Access 1.1.0 targeted do not exist
// here; it will be revisited once the current console API is mapped in-game.
public static class ConsoleReader
{
	private static bool _warned;

	public static bool IsOpen => false;

	public static void Reset()
	{
	}

	public static void Tick()
	{
	}

	/// <summary>
	/// Answers the console-reading key. It has to speak, not only log: the key is bound by
	/// default, and a press met with silence is indistinguishable from a mod that has stopped
	/// working. <see cref="CustomizeBridge.Toggle" /> answers its own disabled key the same way.
	/// </summary>
	public static void ReadFeedback()
	{
		if (!_warned)
		{
			_warned = true;
			Log.Info("Console reading is disabled: this game build does not expose the console UI House Access 1.1.0 targeted.");
		}
		Speaker.SayNow("The developer console is not readable on this version of the game.");
	}
}
