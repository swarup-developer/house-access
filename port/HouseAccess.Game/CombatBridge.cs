using System;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Components;
using UnityEngine;

namespace HouseAccess.Game;

// The combat interaction surface of this game build (punch/block/attack entry points
// and the puppet-master model behind them) was reworked with stripped member names, so
// the original mod's fighting commands cannot be driven safely. What is preserved is
// fight start/end awareness from the player's own combat component, so announcements
// stay honest. The public surface of the original bridge is kept for callers.
public static class CombatBridge
{
	private static bool _wasFighting;

	public static bool Active => false;

	public static void Reset()
	{
		_wasFighting = false;
	}

	public static void Tick()
	{
		bool flag = false;
		try
		{
			PlayerCharacter p = GameRefs.Player;
			if ((UnityEngine.Object)(object)p != (UnityEngine.Object)null)
			{
				CCombat combat = Cpp.Read(() => ((Character)p).Combat);
				flag = combat != null && Cpp.Read(() => combat.IsAttacking, fallback: false);
			}
		}
		catch (Exception ex)
		{
			Log.Debug("Combat tick failed: " + ex.Message);
		}
		if (flag && !_wasFighting)
		{
			_wasFighting = true;
			Speaker.Say("A fight has started. Fighting commands are not available on this version of the game.", Pri.High);
		}
		else if (!flag && _wasFighting)
		{
			_wasFighting = false;
			Speaker.Say("The fight is over.", Pri.High);
		}
	}

	public static void ResolveFight()
	{
		Speaker.SayNow("Fighting commands are not supported on this version of the game.");
	}

	public static void ReadStatus()
	{
		Speaker.SayNow("Combat status is not readable on this version of the game.");
	}
}
