using System;
using System.Collections.Generic;
using System.Text;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using HouseAccess.World;
using EekCharacterEngine.Canvas;
using EekUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HouseAccess.Game;

public static class RadialBridge
{
	private static UIRadialMenu _menu;

	private static readonly List<string> Labels = new List<string>();

	private static readonly List<int> OptionIndex = new List<int>();

	private static int _cursor = -1;

	private static string _centre;

	private static float _openedAt;

	private static float _lastSeenOpen;

	public static bool Active => (UnityEngine.Object)(object)_menu != (UnityEngine.Object)null && Labels.Count > 0;

	public static void Reset()
	{
		_menu = null;
		Labels.Clear();
		OptionIndex.Clear();
		_cursor = -1;
		_centre = null;
	}

	public static void CloseIfOpen()
	{
		UIRadialMenu menu = _menu ?? GameRefs.Radial;
		if (!Cpp.Alive((UnityEngine.Object)(object)menu))
		{
			return;
		}
		bool flag;
		try
		{
			flag = ((Component)menu).gameObject.activeInHierarchy;
		}
		catch
		{
			return;
		}
		if (!flag)
		{
			return;
		}
		Reset();
		try
		{
			menu.OnCloseInteraction();
		}
		catch (Exception ex)
		{
			Log.Debug("Could not close the self-actions wheel: " + ex.Message);
		}
	}

	public static void NotifyOpened(UIRadialMenu menu, string centerLabel)
	{
		_menu = menu;
		_centre = TextUtil.Clean(centerLabel);
		_openedAt = Time.unscaledTime;
		_lastSeenOpen = Time.unscaledTime;
		Labels.Clear();
		OptionIndex.Clear();
		_cursor = -1;
	}

	public static void NotifyChosen(int option)
	{
		string text = null;
		for (int i = 0; i < OptionIndex.Count; i++)
		{
			if (OptionIndex[i] == option && i < Labels.Count)
			{
				text = Labels[i];
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			Speaker.Say(text + ".", Pri.High);
		}
		Reset();
	}

	private static bool StillOpen(UIRadialMenu menu)
	{
		if ((UnityEngine.Object)(object)menu == (UnityEngine.Object)null)
		{
			return false;
		}
		List<Button> list = Cpp.Read(() => Cpp.ToManaged(menu.FAIFDGOFNIA));
		int num = Cpp.Count<Button>(list);
		for (int num2 = 0; num2 < num; num2++)
		{
			Button b = Cpp.At<Button>(list, num2);
			if (Cpp.Alive((UnityEngine.Object)(object)b))
			{
				GameObject val = Cpp.Read(() => ((Component)b).gameObject);
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null && val.activeInHierarchy)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void Tick()
	{
		if (Finder.Active)
		{
			return;
		}
		UIRadialMenu val = _menu ?? GameRefs.Radial;
		if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
		{
			Reset();
			return;
		}
		bool flag = StillOpen(val);
		if (flag)
		{
			_menu = val;
			_lastSeenOpen = Time.unscaledTime;
		}
		else if (Labels.Count > 0)
		{
			if (Time.unscaledTime - _lastSeenOpen > 0.5f)
			{
				Reset();
			}
			return;
		}
		if (Labels.Count == 0)
		{
			if (flag && !(Time.unscaledTime - _openedAt > 5f))
			{
				Collect(val);
			}
		}
		else
		{
			HandleKeys();
		}
	}

	private static void Collect(UIRadialMenu menu)
	{
		List<Button> list = Cpp.Read(() => Cpp.ToManaged(menu.FAIFDGOFNIA));
		int num = Cpp.Count<Button>(list);
		if (num == 0)
		{
			return;
		}
		bool flag = Cpp.Read(() => menu.UseTextMeshProTexts, fallback: false);
		List<TextMeshProUGUI> list2 = (flag ? Cpp.Read(() => Cpp.ToManaged(menu.BNKAKIJGAIL)) : null);
		List<Text> list3 = (flag ? null : Cpp.Read(() => Cpp.ToManaged(menu.CHGFEAJIIMN)));
		List<string> list4 = new List<string>();
		List<int> list5 = new List<int>();
		for (int num2 = 0; num2 < num; num2++)
		{
			Button b = Cpp.At<Button>(list, num2);
			if (!Cpp.Alive((UnityEngine.Object)(object)b))
			{
				continue;
			}
			GameObject val = Cpp.Read(() => ((Component)b).gameObject);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null || !val.activeInHierarchy)
			{
				continue;
			}
			string s = null;
			if (flag)
			{
				TextMeshProUGUI t = Cpp.At<TextMeshProUGUI>(list2, num2);
				if (Cpp.Alive((UnityEngine.Object)(object)t))
				{
					s = Cpp.Read(() => ((TMP_Text)t).text);
				}
			}
			else
			{
				Text t2 = Cpp.At<Text>(list3, num2);
				if (Cpp.Alive((UnityEngine.Object)(object)t2))
				{
					s = Cpp.Read(() => t2.text);
				}
			}
			s = TextUtil.Clean(s);
			if (!string.IsNullOrWhiteSpace(s))
			{
				bool flag2 = Cpp.Read(() => ((Selectable)b).interactable, fallback: true);
				list4.Add(flag2 ? s : (s + ", unavailable"));
				list5.Add(num2);
			}
		}
		if (list4.Count != 0)
		{
			Labels.AddRange(list4);
			OptionIndex.AddRange(list5);
			_cursor = 0;
			Log.Debug($"Radial: {Labels.Count} options collected.");
			Announce();
		}
	}

	private static void Announce()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (!string.IsNullOrEmpty(_centre))
		{
			stringBuilder.Append(_centre);
			stringBuilder.Append(". ");
		}
		stringBuilder.Append(TextUtil.Pluralise(Labels.Count, "option", "options"));
		stringBuilder.Append(". ");
		for (int i = 0; i < Labels.Count; i++)
		{
			stringBuilder.Append(i + 1);
			stringBuilder.Append(". ");
			stringBuilder.Append(Labels[i]);
			stringBuilder.Append(". ");
		}
		Speaker.Say(TextUtil.Cap(stringBuilder.ToString(), 800), Pri.High);
	}

	/// <summary>
	/// Reads the option under the cursor. <paramref name="force" /> speaks at Critical so a
	/// repeated number key is heard again instead of being dropped as a duplicate.
	/// </summary>
	private static void SpeakCurrent(bool force = false)
	{
		if (_cursor >= 0 && _cursor < Labels.Count)
		{
			Speaker.Say($"{Labels[_cursor]}, {_cursor + 1} of {Labels.Count}.", force ? Pri.Critical : Pri.High);
		}
	}

	private static void HandleKeys()
	{
		if (Keys.Hit(Prefs.KeyCancel))
		{
			Reset();
			Speaker.SayNow("Closed.");
			return;
		}
		if (Keys.Hit(Prefs.KeyUiNext))
		{
			Move(1);
			return;
		}
		if (Keys.Hit(Prefs.KeyUiPrev))
		{
			Move(-1);
			return;
		}
		if (Keys.Hit(Prefs.KeyRepeatTarget))
		{
			SpeakCurrent();
			return;
		}
		if (Keys.Hit(Prefs.KeyUiActivate))
		{
			Choose();
			return;
		}
		if (ListNumbers.Pressed(Labels.Count, out var index, out var pick))
		{
			_cursor = index;
			if (pick)
			{
				Choose();
			}
			else
			{
				SpeakCurrent(force: true);
			}
		}
	}

	private static void Move(int dir)
	{
		if (Labels.Count != 0)
		{
			_cursor = (_cursor + dir + Labels.Count) % Labels.Count;
			SpeakCurrent();
		}
	}

	private static void Choose()
	{
		if ((UnityEngine.Object)(object)_menu == (UnityEngine.Object)null || _cursor < 0 || _cursor >= OptionIndex.Count)
		{
			return;
		}
		int num = OptionIndex[_cursor];
		string text = Labels[_cursor];
		if (text.EndsWith(", unavailable", StringComparison.OrdinalIgnoreCase))
		{
			Speaker.SayNow("That option is unavailable.");
			return;
		}
		Speaker.SayNow(text + ".");
		try
		{
			_menu.OnChoose(num);
		}
		catch (Exception ex)
		{
			Log.Warn("Radial OnChoose failed: " + ex.Message);
		}
		Reset();
	}
}
