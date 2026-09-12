using System.Collections.Generic;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Components;
using EekCharacterEngine.Interaction;
using EekEvents.Support;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

// Inventory lives in the split EekCharacterEngine assembly on current builds.
using InventoryComp = EekCharacterEngine.Components.CInventory;

namespace HouseAccess.Game;

public static class Activity
{
	private static readonly Dictionary<int, Character> Conversations = new Dictionary<int, Character>();

	private static float _conversationsAt;

	public static string Describe(Character c)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)c))
		{
			return null;
		}
		List<string> list = new List<string>();
		string text = Posture(c);
		if (text != null)
		{
			list.Add(text);
		}
		if (IsDancing(c))
		{
			list.Add("dancing");
		}
		string text2 = UsingWhat(c);
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add("at the " + text2);
		}
		if (Cpp.Read(() => c.IsOnPhone, fallback: false))
		{
			list.Add("on the phone");
		}
		if (Cpp.Read(() => c.IsChanging, fallback: false))
		{
			list.Add("getting changed");
		}
		if (Cpp.Read(() => c.IsNaked, fallback: false))
		{
			list.Add("naked");
		}
		else if (Cpp.Read(() => c.IsTopless, fallback: false))
		{
			list.Add("topless");
		}
		else if (Cpp.Read(() => c.IsBottomless, fallback: false))
		{
			list.Add("bottomless");
		}
		string text3 = Busy(c);
		if (text3 != null)
		{
			list.Add(text3);
		}
		return (list.Count == 0) ? null : string.Join(", ", list);
	}

	private static bool IsDancing(Character c)
	{
		// This game build does not expose a dancing state on characters.
		return false;
	}

	private static bool IsMoving(Character c)
	{
		try
		{
			float num = Cpp.Read(() => c.CurrentMovementSpeed, 0f);
			return num > 0.05f;
		}
		catch
		{
			return false;
		}
	}

	private static string UsingWhat(Character c)
	{
		try
		{
			InteractiveItem val = Cpp.Read(() => (InteractiveItem)(object)c.CurrentActionItem);
			if (!Cpp.Alive((UnityEngine.Object)(object)val))
			{
				return null;
			}
			return GameRefs.DisplayLabel(val) ?? GameRefs.NameOf(val);
		}
		catch
		{
			return null;
		}
	}

	private static string Posture(Character c)
	{
		// This game build exposes sitting/crouching/falling/standing states only; lying
		// and kneeling are not exposed, so those postures are not described.
		if (Cpp.Read(() => c.IsSitting, fallback: false) && !IsMoving(c))
		{
			return "sitting";
		}
		if (Cpp.Read(() => c.IsCrouching, fallback: false))
		{
			return "crouching";
		}
		if (Cpp.Read(() => c.IsFalling, fallback: false))
		{
			return "falling";
		}
		return null;
	}

	private static string Busy(Character c)
	{
		Character val = TalkingTo(c);
		if (Cpp.Alive((UnityEngine.Object)(object)val))
		{
			string text = GameRefs.NameOf(val);
			return string.IsNullOrWhiteSpace(text) ? "in conversation" : ("talking to " + text);
		}
		Character val2 = TalkedToBy(c);
		if (Cpp.Alive((UnityEngine.Object)(object)val2))
		{
			string text2 = GameRefs.NameOf(val2);
			return string.IsNullOrWhiteSpace(text2) ? "in conversation" : ("being talked to by " + text2);
		}
		// This game build has no CharacterBase.IsBusy flag; conversation state above is
		// the only busy indicator it exposes.
		return null;
	}

	private static Character TalkingTo(Character c)
	{
		try
		{
			NonPlayerCharacter npc = ((Il2CppObjectBase)c).TryCast<NonPlayerCharacter>();
			if ((UnityEngine.Object)(object)npc == (UnityEngine.Object)null)
			{
				return null;
			}
			CSocialize social = Cpp.Read(() => npc.Socialize);
			if (social == null)
			{
				return null;
			}
			return Cpp.Read(() => social.IsTalkingTo);
		}
		catch
		{
			return null;
		}
	}

	private static Character TalkedToBy(Character c)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)c))
		{
			return null;
		}
		RefreshConversations();
		try
		{
			int instanceID = ((UnityEngine.Object)c).GetInstanceID();
			foreach (KeyValuePair<int, Character> conversation in Conversations)
			{
				if (conversation.Key == instanceID)
				{
					return conversation.Value;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static void RefreshConversations()
	{
		if (Time.unscaledTime - _conversationsAt < 0.5f)
		{
			return;
		}
		_conversationsAt = Time.unscaledTime;
		Conversations.Clear();
		foreach (Character item in GameRefs.Npcs())
		{
			Character val = TalkingTo(item);
			if (Cpp.Alive((UnityEngine.Object)(object)val))
			{
				try
				{
					Conversations[((UnityEngine.Object)val).GetInstanceID()] = item;
				}
				catch
				{
				}
			}
		}
	}

	public static string ClockTime()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!GameRefs.InPlayScene)
			{
				return null;
			}
			string text = PhoneBridge.ScreenTime();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
			// This game build keeps its game clock inside an opaque interop struct with no
			// readable hour/minute members, so the time is only spoken from the phone.
			return null;
		}
		catch
		{
			return null;
		}
	}

	public static string Holding()
	{
		try
		{
			PlayerCharacter p = GameRefs.Player;
			if ((UnityEngine.Object)(object)p == (UnityEngine.Object)null)
			{
				return null;
			}
			InventoryComp bag = Cpp.Read(() => p.Inventory);
			if (bag == null)
			{
				return null;
			}
			// Use the inventory's current display slot, not a cached item name.
			InventoryObject shown = Cpp.Read(() => bag.OnDisplay);
			if (shown == null)
			{
				return null;
			}
			string text = Cpp.Read(() => shown.Name);
			return string.IsNullOrWhiteSpace(text) ? null : TextUtil.Clean(text);
		}
		catch
		{
			return null;
		}
	}
}
