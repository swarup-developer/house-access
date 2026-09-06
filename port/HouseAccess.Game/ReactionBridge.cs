using System;
using System.Collections.Generic;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using UnityEngine;

namespace HouseAccess.Game;

public static class ReactionBridge
{
	private static readonly List<string> DefaultInclude = new List<string> { "Caught", "Attack", "Knock", "Projectile", "Locked", "Impacts", "Gropes", "EntersZone" };

	private static readonly List<string> AlwaysNoisy = new List<string> { "Vicinity", "Vision", "IsEnabled", "ItemFunction", "CutScene" };

	private static float _lastSpokeAt;

	public static void Reset()
	{
		_lastSpokeAt = 0f;
	}

	public static void OnReaction(Character reactee, string typeName, string value)
	{
		if (Prefs.SpeakReactions.Value && !DialogueBridge.Active && !DialogueBridge.SpeakerTalking && !string.IsNullOrWhiteSpace(typeName) && Wanted(typeName) && !(Time.unscaledTime - _lastSpokeAt < 0.6f))
		{
			string text = GameRefs.NameOf(reactee);
			string text2 = Phrase(typeName);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				_lastSpokeAt = Time.unscaledTime;
				Speaker.Say(string.IsNullOrWhiteSpace(text) ? text2 : (text + " " + text2 + "."));
			}
		}
	}

	private static bool Wanted(string typeName)
	{
		string text = (Prefs.Reactions?.Value ?? "notable").Trim().ToLowerInvariant();
		if (text == "off")
		{
			return false;
		}
		foreach (string item in AlwaysNoisy)
		{
			if (typeName.IndexOf(item, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return text == "all";
			}
		}
		if (text == "all")
		{
			return true;
		}
		foreach (string item2 in DefaultInclude)
		{
			if (typeName.IndexOf(item2, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static string Phrase(string typeName)
	{
		if (Has(typeName, "GetsCaught"))
		{
			string rawName = typeName.Replace("GetsCaught", string.Empty).Replace("ByMe", string.Empty);
			return "noticed you saw them " + TextUtil.Humanize(rawName).ToLowerInvariant();
		}
		if (Has(typeName, "GetsKnockedOut"))
		{
			return "is knocked out";
		}
		if (Has(typeName, "GetsAttacked"))
		{
			return "is attacked";
		}
		if (Has(typeName, "GetsHitWithProjectile"))
		{
			return "is hit";
		}
		if (Has(typeName, "IsBlockedByLockedDoor"))
		{
			return "is stuck at a locked door";
		}
		if (Has(typeName, "ImpactsWall"))
		{
			return "walks into a wall";
		}
		if (Has(typeName, "ImpactsGround"))
		{
			return "falls over";
		}
		if (Has(typeName, "EntersZone"))
		{
			return "comes in";
		}
		if (Has(typeName, "Gropes"))
		{
			return "reacts to being touched";
		}
		return TextUtil.Humanize(typeName).ToLowerInvariant();
	}

	private static bool Has(string haystack, string needle)
	{
		return haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
	}
}
