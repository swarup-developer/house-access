using System;
using HouseAccess.Game;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Components;
using UnityEngine;

namespace HouseAccess.World;

public static class Hold
{
	private const float PulseSeconds = 0.35f;

	private const float PulseLength = 1f;

	private const float MaxHoldSeconds = 120f;

	private static Character _held;

	private static string _heldName;

	private static float _nextPulse;

	private static float _releaseAt;

	public static bool Active => Cpp.Alive((UnityEngine.Object)(object)_held);

	public static string HeldName => _heldName;

	public static void Reset()
	{
		_held = null;
		_heldName = null;
		_nextPulse = 0f;
		_releaseAt = 0f;
	}

	public static void Toggle()
	{
		if (Active)
		{
			Release("Released " + _heldName + ".");
			return;
		}
		Entry current = Radar.Current;
		if (current == null || current.Kind != EntryKind.Person)
		{
			Speaker.SayNow("Select a person first.");
		}
		else
		{
			Begin(current.Person, announce: true);
		}
	}

	public static void Begin(Character c, bool announce)
	{
		if (Cpp.Alive((UnityEngine.Object)(object)c))
		{
			_held = c;
			_heldName = GameRefs.NameOf(c) ?? "them";
			_nextPulse = 0f;
			_releaseAt = Time.unscaledTime + 120f;
			Pulse();
			if (announce)
			{
				Speaker.SayNow("Holding " + _heldName + ".");
			}
			else
			{
				Speaker.Say("Asking " + _heldName + " to wait.");
			}
		}
	}

	public static void Release(string reason)
	{
		if (!Active)
		{
			_held = null;
			return;
		}
		_held = null;
		_nextPulse = 0f;
		if (!string.IsNullOrEmpty(reason))
		{
			Speaker.Say(reason);
		}
	}

	public static void ReleaseQuiet()
	{
		Release(null);
	}

	public static void Tick()
	{
		if (Active)
		{
			if (Time.unscaledTime >= _releaseAt)
			{
				Release(_heldName + " is free to move again.");
			}
			else if (!(Time.unscaledTime < _nextPulse))
			{
				_nextPulse = Time.unscaledTime + 0.35f;
				Pulse();
			}
		}
	}

	private static void Pulse()
	{
		try
		{
			CMotion val = Cpp.Read(() => _held.Motion);
			if (val == null)
			{
				Release(null);
			}
			else
			{
				// This game build has no CMotion.PauseNavigationFor; the game character keeps
				// whatever course the game itself gave it while being held.
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Hold failed: " + ex.Message);
			Release(null);
		}
	}
}
