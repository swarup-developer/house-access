using HouseAccess;

namespace HouseAccess.Game;

// WORKAROUND: House Access v1.1.0 tracked the "opportunities" quest-style UI of an
// older House Party build (EekEvents.Opportunities.*). This GOG v1.1.7 build
// only exposes global::OpportunityWindowManager with an unverified, mostly-unmapped
// API. The opportunities announcements are therefore disabled until the current UI
// can be mapped and verified in-game.
public static class OpportunityBridge
{
	private static bool _warned;

	public static bool Active => false;

	public static void Reset()
	{
	}

	public static void Tick()
	{
	}

	public static void CloseIfOpen()
	{
	}

	public static void NotifyOpened()
	{
	}

	public static void NotifyChosen()
	{
	}

	private static void WarnOnce()
	{
		if (!_warned)
		{
			_warned = true;
			Log.Info("Opportunities announcements are disabled: this game build does not expose the opportunities UI API House Access 1.1.0 targeted.");
		}
	}
}
