namespace HouseAccess.Game;

/// <summary>Converts the native wheels' one-based choices to their list bounds.</summary>
internal static class WheelChoice
{
	public static bool IsValidNumber(int number, int optionCount, int buttonCount)
	{
		return number > 0 && number <= optionCount && number <= buttonCount;
	}
}
