using HouseAccess;

namespace HouseAccess.Game;

// WORKAROUND: House Access v1.1.0 helped blind users operate the developer command
// console of an older House Party build (ConsoleCommandBuilder, argument pickers and
// a searchable command dropdown). None of those classes exist in this GOG v1.1.7
// build, so console command building is disabled until the current console API
// (global::CommandConsole / DebugConsole) is mapped and verified in-game.
public static class ConsoleBuilder
{
	public static bool Active => false;

	public static void Reset()
	{
	}

	public static void Tick()
	{
	}

	public static void Close()
	{
	}

	public static void Open()
	{
	}

	public static void SayUnavailable()
	{
		Log.Info("Console command support is disabled: this game build does not expose the console command UI House Access 1.1.0 targeted.");
	}
}
