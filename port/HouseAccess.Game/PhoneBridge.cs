using System;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace HouseAccess.Game;

// Phone-based reading (photo gallery, camera viewer, phone screen text) is not portable
// to this game build: the MadisonPhone and camera-view internals the original mod read
// were stripped from the shipped assemblies. The public surface is preserved so callers
// keep compiling, and every feature reports "not available" instead of failing.
public static class PhoneBridge
{
	private static bool _wasUp;

	public static bool IsUp
	{
		get
		{
			try
			{
				// The player character exposes whether they are looking at a phone.
				Character p = ((Il2CppObjectBase)GameRefs.Player).TryCast<Character>();
				if (!Cpp.Alive((UnityEngine.Object)(object)p))
				{
					return false;
				}
				return Cpp.Read(() => p.IsOnPhone, fallback: false);
			}
			catch
			{
				return false;
			}
		}
	}

	public static bool Active => false;

	public static bool ViewerOpen => false;

	public static void Reset()
	{
		_wasUp = false;
	}

	public static void ReadCurrentPhoto()
	{
		Speaker.Say("Photo reading is not supported on this version of the game.");
	}

	public static void StepPhoto(int dir)
	{
	}

	public static string ScreenTime()
	{
		// The phone's clock text is not readable on this game build.
		return null;
	}

	public static void ReadCurrentShot()
	{
		Speaker.Say("Camera reading is not supported on this version of the game.");
	}

	public static void StepShot(int dir)
	{
	}

	public static void Tick()
	{
		bool up = IsUp;
		if (up == _wasUp)
		{
			return;
		}
		_wasUp = up;
		try
		{
			if (up)
			{
				Speaker.Say("Phone. Phone apps are not readable on this version of the game.");
			}
		}
		catch (Exception ex)
		{
			Log.Debug("Phone tick failed: " + ex.Message);
		}
	}
}
