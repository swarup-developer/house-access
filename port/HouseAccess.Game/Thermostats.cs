using HouseAccess.Speech;
using HouseAccess.Util;
using HouseParty;
using UnityEngine;

using Speaker = HouseAccess.Speech.Speaker;

namespace HouseAccess.Game;

public static class Thermostats
{
	private static Thermostat _pending;

	private static float _readAt;

	public static void Reset()
	{
		_pending = null;
	}

	public static void NotifyTampered(Thermostat unit)
	{
		_pending = unit;
		_readAt = Time.unscaledTime + 0.4f;
	}

	public static string Reading(Thermostat unit)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)unit))
		{
			return null;
		}
		string text = TextUtil.Clean(Cpp.Read(() => unit.PMKHNGAALCE));
		return string.IsNullOrWhiteSpace(text) ? null : text;
	}

	public static bool ReadNearest()
	{
		Thermostat unit = Cpp.FindOne<Thermostat>(activeOnly: false);
		if (!Cpp.Alive((UnityEngine.Object)(object)unit))
		{
			return false;
		}
		string text = Reading(unit);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		Speaker.Say(Cpp.Read(() => unit.BHOLPDGBDLO, fallback: false) ? ("Thermostat set to " + text + ", and it has been tampered with.") : ("Thermostat set to " + text + "."), Pri.High);
		return true;
	}

	public static void Tick()
	{
		if (!((UnityEngine.Object)(object)_pending == (UnityEngine.Object)null) && !(Time.unscaledTime < _readAt))
		{
			Thermostat pending = _pending;
			_pending = null;
			if (Cpp.Alive((UnityEngine.Object)(object)pending))
			{
				string text = Reading(pending);
				Speaker.Say(string.IsNullOrWhiteSpace(text) ? "Thermostat changed." : ("Thermostat now set to " + text + "."), Pri.High);
			}
		}
	}
}
