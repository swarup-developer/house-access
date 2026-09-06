using System;
using HouseAccess.Speech;
using HouseAccess.UI;
using HouseAccess.Util;
using EekCharacterEngine.Canvas;
using HouseParty;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

using Speaker = HouseAccess.Speech.Speaker;

namespace HouseAccess.Game;

public static class SettingsBridge
{
	public static bool Active => MenuReader.Active && IsSettingsCanvas();

	public static void Reset()
	{
	}

	public static void Tick()
	{
	}

	private static bool IsSettingsCanvas()
	{
		try
		{
			foreach (CanvasBase c in Cpp.FindAll<CanvasBase>(activeOnly: true))
			{
				if (!Cpp.Alive((UnityEngine.Object)(object)c))
					continue;
				try
				{
					if (Cpp.Read(() => ((Component)c).gameObject.activeSelf))
					{
						string name = ((UnityEngine.Object)((Component)c).gameObject).name ?? string.Empty;
						if (name.IndexOf("setting", StringComparison.OrdinalIgnoreCase) >= 0
							|| name.IndexOf("option", StringComparison.OrdinalIgnoreCase) >= 0)
							return true;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return false;
	}
}
