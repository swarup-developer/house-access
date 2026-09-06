using HouseAccess.Speech;
using HouseAccess.Util;

namespace HouseAccess.InputLayer;

/// <summary>
/// The one number-key rule shared by every list the mod reads out: a number on its own
/// reads that item, the same number with control picks it, and a number past the end of
/// the list says how many items there are.
///
/// It lives beside <see cref="Keys" /> rather than in each list because the lists used to
/// disagree - some picked on a bare number, some only moved the cursor, some ignored the
/// keypad - and a rule that changes between menus is worse than no rule at all. It speaks
/// the out-of-range message itself for the same reason: a pressed key answered with silence
/// is indistinguishable from a mod that has stopped working, and the wording has to be the
/// same wherever it happens.
/// </summary>
public static class ListNumbers
{
	/// <summary>
	/// True when a number key went down this frame, without needing to know how long the list
	/// is. <paramref name="number" /> is 1 to 10, counting 0 as the tenth item, and
	/// <paramref name="pick" /> is set when control was held.
	///
	/// Split out from <see cref="Pressed" /> for the lists that have to be gathered before they
	/// can be counted: MenuReader has to sweep every control on screen, and doing that on every
	/// frame just to find out no number was pressed would cost far more than the feature.
	/// </summary>
	public static bool Wanted(out int number, out bool pick)
	{
		number = Keys.NumberDown();
		pick = number != 0 && Keys.Ctrl;
		return number != 0;
	}

	/// <summary>
	/// Turns a number from <see cref="Wanted" /> into a zero-based row in a list of
	/// <paramref name="count" /> items, speaking the reason when it cannot. A number past the
	/// end has to say something: a key press answered with silence is indistinguishable from a
	/// mod that has stopped working.
	/// </summary>
	public static bool Resolve(int number, int count, out int index)
	{
		index = -1;
		if (number <= 0)
		{
			return false;
		}
		if (count <= 0)
		{
			Speaker.SayNow("Nothing in this list.");
			return false;
		}
		if (number > count)
		{
			Speaker.SayNow("Only " + TextUtil.Pluralise(count, "item", "items") + " here.");
			return false;
		}
		index = number - 1;
		return true;
	}

	/// <summary>
	/// True when a number key selected an item in a list of <paramref name="count" /> items.
	/// <paramref name="index" /> is zero-based; <paramref name="pick" /> is set when control
	/// was held, meaning the caller should act on the item rather than just read it.
	/// </summary>
	public static bool Pressed(int count, out int index, out bool pick)
	{
		index = -1;
		if (!Wanted(out int number, out pick))
		{
			return false;
		}
		return Resolve(number, count, out index);
	}

	/// <summary>
	/// The picking half on its own, for the one list that types: in the search box a bare
	/// number belongs in the name being typed, so only control and a number choose a match.
	/// </summary>
	public static bool PickPressed(int count, out int index)
	{
		index = -1;
		if (!Wanted(out int number, out bool pick) || !pick)
		{
			return false;
		}
		return Resolve(number, count, out index);
	}
}
