using System.Collections.Generic;
using System.Text;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Interaction;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace HouseAccess.Game;

public static class Watchers
{
	public static List<Character> Observers()
	{
		List<Character> list = new List<Character>();
		PlayerCharacter me = GameRefs.Player;
		if ((UnityEngine.Object)(object)me == (UnityEngine.Object)null)
		{
			return list;
		}
		foreach (Character c in GameRefs.Npcs())
		{
			if (Cpp.Alive((UnityEngine.Object)(object)c) && Cpp.Read(() => c.CanSee(((Il2CppObjectBase)me).TryCast<Character>()), fallback: false))
			{
				list.Add(c);
			}
		}
		return list;
	}

	public static List<Character> ObserversOf(InteractiveItem item)
	{
		List<Character> list = new List<Character>();
		if (!Cpp.Alive((UnityEngine.Object)(object)item))
		{
			return list;
		}
		foreach (Character c in GameRefs.Npcs())
		{
			if (Cpp.Alive((UnityEngine.Object)(object)c) && Cpp.Read(() => c.CanSeeItem(item), fallback: false))
			{
				list.Add(c);
			}
		}
		return list;
	}

	private static string Names(List<Character> people)
	{
		List<string> list = new List<string>();
		foreach (Character person in people)
		{
			string text = GameRefs.NameOf(person);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(text);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		if (list.Count == 1)
		{
			return list[0];
		}
		string text2 = list[list.Count - 1];
		list.RemoveAt(list.Count - 1);
		return string.Join(", ", list) + " and " + text2;
	}

	public static void AnnounceObservers()
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		if (!GameRefs.InPlayScene)
		{
			Speaker.SayNow("Only available in the house.");
			return;
		}
		List<Character> list = Observers();
		if (list.Count == 0)
		{
			Speaker.Say("Nobody can see you.", Pri.High);
			return;
		}
		Vector3 eyePos = GameRefs.EyePos;
		Vector3 forward = GameRefs.Forward;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(TextUtil.Pluralise(list.Count, "person", "people"));
		stringBuilder.Append(" can see you. ");
		foreach (Character item in list)
		{
			string value = GameRefs.NameOf(item);
			if (!string.IsNullOrWhiteSpace(value))
			{
				Vector3 val = GameRefs.AimPointOf(item);
				float metres = Vector3.Distance(eyePos, val);
				stringBuilder.Append(value);
				stringBuilder.Append(", ");
				stringBuilder.Append(TextUtil.Distance(metres));
				stringBuilder.Append(", ");
				stringBuilder.Append(TextUtil.ClockPhrase(TextUtil.ClockBearing(forward, val - eyePos)));
				string value2 = GameRefs.RoomOf(item);
				if (!string.IsNullOrWhiteSpace(value2))
				{
					stringBuilder.Append(", in the ");
					stringBuilder.Append(value2);
				}
				stringBuilder.Append(". ");
			}
		}
		Speaker.Say(TextUtil.Cap(stringBuilder.ToString(), 800), Pri.High);
	}

	public static string WhoWouldSee(InteractiveItem item, string verb)
	{
		if (!Prefs.WarnWhenWatched.Value)
		{
			return null;
		}
		if (!IsSensitive(verb))
		{
			return null;
		}
		List<Character> list = ObserversOf(item);
		if (list.Count == 0)
		{
			list = Observers();
		}
		return (list.Count == 0) ? null : Names(list);
	}

	private static bool IsSensitive(string verb)
	{
		if (string.IsNullOrWhiteSpace(verb))
		{
			return false;
		}
		string text = verb.ToLowerInvariant();
		return text.Contains("take") || text.Contains("steal") || text.Contains("pocket") || text.Contains("open") || text.Contains("drink") || text.Contains("use");
	}
}
