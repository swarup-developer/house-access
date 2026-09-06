using System;
using System.Collections.Generic;
using HouseAccess.Util;
using UnityEngine;
using UnityEngine.UI;

namespace HouseAccess.Game;

public static class StatusBars
{
	public static List<string> Read()
	{
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (UltimateStatusBar bar in Cpp.FindAll<UltimateStatusBar>(activeOnly: true))
		{
			if (!Cpp.Alive((UnityEngine.Object)(object)bar))
			{
				continue;
			}
			string s = Cpp.Read(() => bar.statusBarName);
			s = TextUtil.Clean(s);
			if (string.IsNullOrWhiteSpace(s) || !hashSet.Add(s))
			{
				continue;
			}
			string text = ValueOf(bar);
			if (!string.IsNullOrWhiteSpace(text))
			{
				string value = TextUtil.Clean(Cpp.Read(() => bar.additionalText));
				list.Add(string.IsNullOrWhiteSpace(value) ? (TextUtil.Humanize(s).ToLowerInvariant() + " " + text) : $"{TextUtil.Humanize(s).ToLowerInvariant()} {text} {value}");
			}
		}
		return list;
	}

	private static string ValueOf(UltimateStatusBar bar)
	{
		try
		{
			if (Cpp.Read(() => bar.showText, fallback: false))
			{
				Text label = Cpp.Read(() => bar.statusBarText);
				if (Cpp.Alive((UnityEngine.Object)(object)label))
				{
					string text = TextUtil.Clean(Cpp.Read(() => label.text));
					if (!string.IsNullOrWhiteSpace(text))
					{
						return text;
					}
				}
			}
			Image fill = Cpp.Read(() => bar.statusBar);
			if (Cpp.Alive((UnityEngine.Object)(object)fill))
			{
				float num = Cpp.Read(() => fill.fillAmount, -1f);
				if (num >= 0f)
				{
					return $"{Mathf.RoundToInt(num * 100f)} percent";
				}
			}
		}
		catch
		{
		}
		return null;
	}
}
