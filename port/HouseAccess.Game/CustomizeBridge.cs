using HouseAccess;
using HouseAccess.Speech;

namespace HouseAccess.Game;

// WORKAROUND: House Access v1.1.0 made the main-menu character customisation screen
// explorable and spoke each control (it tracked MainMenuCharacterCustomization and
// ClickManager). Neither class exists in this GOG v1.1.7 build, and no map-verified
// equivalent for the customisation screen was found, so this subsystem is disabled
// until the current screen can be identified and mapped in-game.
public static class CustomizeBridge
{
	private static bool _warned;

	public static bool Active => false;

	public static void Reset()
	{
	}

	public static void Tick()
	{
	}

	public static void Toggle()
	{
		if (!_warned)
		{
			_warned = true;
			Log.Info("Character customisation support is disabled: this game build does not expose the customisation screen House Access 1.1.0 targeted.");
		}
		Speaker.SayNow("The character customisation screen from this version is not supported yet.");
	}

	public static void CloseIfOpen()
	{
	}
}
