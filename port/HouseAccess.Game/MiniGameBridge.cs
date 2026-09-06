using HouseAccess;

namespace HouseAccess.Game;

// WORKAROUND: House Access v1.1.0 was built against a House Party version whose
// mini-game classes (MiniGame, MiniGameNames, mini-game start/score/end events) are
// not present in this GOG v1.1.7 build (only MiniGameManager / MiniGameCanvas exist,
// with no map-verified equivalent for the events the mod announced). Rather than
// guess at the new API, mini-game announcements are disabled here until the current
// API can be verified in-game. All other subsystems are unaffected.
public static class MiniGameBridge
{
	private static bool _warned;

	public static bool Active => false;

	public static void Reset()
	{
	}

	public static void OnStarted()
	{
	}

	public static void OnEnded()
	{
	}

	public static bool ReadStatus()
	{
		if (!_warned)
		{
			_warned = true;
			Log.Info("Mini-game announcements are disabled: this game build does not expose the mini-game API House Access 1.1.0 targeted.");
		}
		return false;
	}
}
