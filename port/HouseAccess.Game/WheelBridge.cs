using System;
using System.Collections.Generic;
using System.Text;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using HouseAccess.World;
using EekCharacterEngine.Canvas;
using EekCharacterEngine.Interaction;
using EekUI;
using UnityEngine;

namespace HouseAccess.Game;

public static class WheelBridge
{
	private static RadialMenu _menu;

	private static readonly List<string> Labels = new List<string>();

	private static readonly List<bool> Enabled = new List<bool>();

	private static readonly List<int> SourceIndex = new List<int>();

	private static int _index = -1;

	private static float _expiresAt;

	private static string _title;

	private static InteractiveItem _item;

	private static float _openingUntil;

	private static string _confirmVerb;

	private static float _confirmUntil;

	public static bool Opening => Time.unscaledTime < _openingUntil;

	public static bool Active => _index >= 0 && Cpp.Alive((UnityEngine.Object)(object)_menu) && Time.unscaledTime <= _expiresAt;

	public static void NotifyOpening()
	{
		_openingUntil = Time.unscaledTime + 1.5f;
	}

	public static void Reset()
	{
		_menu = null;
		_item = null;
		_confirmVerb = null;
		Labels.Clear();
		SourceIndex.Clear();
		Enabled.Clear();
		_index = -1;
		_title = null;
	}

	public static void NotifyOpened(RadialMenu menu, string centerLabel, InteractiveItem item)
	{
		_openingUntil = 0f;
		_menu = menu;
		_item = item;
		_confirmVerb = null;
		_title = TextUtil.Clean(centerLabel);
		_expiresAt = Time.unscaledTime + 30f;
		_index = -2;
	}

	public static void NotifyChosen()
	{
		Reset();
	}

	private static void Collect()
	{
		Labels.Clear();
		Enabled.Clear();
		SourceIndex.Clear();
		if (!Cpp.Alive((UnityEngine.Object)(object)_menu))
		{
			return;
		}
		// The wheel's current options live in a stripped-named member on this build; it
		// is read as its (label, enabled) list.
		Il2CppSystem.Collections.Generic.List<Il2CppSystem.ValueTuple<string, bool>> list =
			Cpp.Read(() => _menu.BHNCIKDJNOO);
		int num = Cpp.CountOf<Il2CppSystem.ValueTuple<string, bool>>(list);
		for (int num2 = 0; num2 < num; num2++)
		{
			Il2CppSystem.ValueTuple<string, bool> val = Cpp.AtOf<Il2CppSystem.ValueTuple<string, bool>>(list, num2);
			string item = (val == null) ? null : Cpp.Read(() => val.Item1);
			if (!string.IsNullOrWhiteSpace(item))
			{
				Labels.Add(TextUtil.Humanize(item));
				Enabled.Add(val != null && Cpp.Read(() => val.Item2, fallback: false));
				SourceIndex.Add(num2);
			}
		}
	}

	private static void Announce()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(_title))
		{
			stringBuilder.Append(_title);
			stringBuilder.Append(". ");
		}
		if (Labels.Count == 0)
		{
			stringBuilder.Append("No options.");
			Speaker.Say(stringBuilder.ToString(), Pri.High);
			return;
		}
		for (int i = 0; i < Labels.Count; i++)
		{
			stringBuilder.Append(i + 1);
			stringBuilder.Append(". ");
			stringBuilder.Append(Describe(i));
			stringBuilder.Append(". ");
		}
		string value = Prefs.KeyCancel?.Value;
		if (!string.IsNullOrWhiteSpace(value))
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendFormatted(value);
			handler.AppendLiteral(" to close.");
			stringBuilder2.Append(ref handler);
		}
		Speaker.Say(TextUtil.Cap(stringBuilder.ToString(), 900), Pri.High);
		SpeakCurrent();
	}

	private static string Describe(int i)
	{
		if (i < 0 || i >= Labels.Count)
		{
			return null;
		}
		return (i < Enabled.Count && !Enabled[i]) ? (Labels[i] + ", unavailable") : Labels[i];
	}

	/// <summary>
	/// Reads the wheel slot under the cursor. <paramref name="force" /> speaks at Critical so
	/// a repeated number key is heard again rather than suppressed as a duplicate.
	/// </summary>
	private static void SpeakCurrent(bool force = false)
	{
		if (_index >= 0 && _index < Labels.Count)
		{
			Speaker.Say($"{Describe(_index)}, {_index + 1} of {Labels.Count}.", force ? Pri.Critical : Pri.High);
		}
	}

	private static void Move(int dir)
	{
		if (Labels.Count != 0)
		{
			_index = (_index + dir + Labels.Count) % Labels.Count;
			_expiresAt = Time.unscaledTime + 30f;
			SpeakCurrent();
		}
	}

	private static void Choose(int i)
	{
		if (i < 0 || i >= Labels.Count || !Cpp.Alive((UnityEngine.Object)(object)_menu))
		{
			return;
		}
		if (i < Enabled.Count && !Enabled[i])
		{
			Speaker.SayNow(Labels[i] + " is not available.");
			return;
		}
		string text = Labels[i];
		if (!string.Equals(_confirmVerb, text, StringComparison.OrdinalIgnoreCase) || Time.unscaledTime > _confirmUntil)
		{
			string text2 = Watchers.WhoWouldSee(_item, text);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				_confirmVerb = text;
				_confirmUntil = Time.unscaledTime + 6f;
				_expiresAt = Time.unscaledTime + 30f;
				Speaker.SayNow(text2 + " can see this. Press again to " + text.ToLowerInvariant() + " anyway.");
				return;
			}
		}
		RadialMenu menu = _menu;
		Reset();
		Speaker.SayNow(text + ".");
		int num = ((i < SourceIndex.Count) ? SourceIndex[i] : i);
		try
		{
			menu.OnChoose(num);
		}
		catch (Exception ex)
		{
			Log.Warn("Wheel choice failed: " + ex.Message);
			Speaker.SayNow("That did not work.");
		}
	}

	public static void CloseIfOpen()
	{
		RadialMenu menu = null;
		try
		{
			foreach (RadialMenu item in Cpp.FindAll<RadialMenu>(activeOnly: true))
			{
				if (Cpp.Alive((UnityEngine.Object)(object)item))
				{
					menu = item;
					break;
				}
			}
		}
		catch
		{
			return;
		}
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
			Log.Debug("Could not close the wheel: " + ex.Message);
		}
	}

	private static void Close()
	{
		RadialMenu menu = _menu;
		Reset();
		try
		{
			if (Cpp.Alive((UnityEngine.Object)(object)menu))
			{
				menu.OnCloseInteraction();
			}
		}
		catch
		{
		}
		Speaker.SayNow("Closed.");
	}

	public static void Tick()
	{
		if (Finder.Active)
		{
			return;
		}
		if (_index == -2)
		{
			Collect();
			if (Labels.Count != 0 || !(Time.unscaledTime < _expiresAt - 29.5f))
			{
				_index = ((Labels.Count <= 0) ? (-1) : 0);
				Announce();
			}
		}
		else
		{
			if (!Active)
			{
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
			if (Keys.Hit(Prefs.KeyUiActivate))
			{
				Choose(_index);
				return;
			}
			if (Keys.Hit(Prefs.KeyRepeatTarget))
			{
				SpeakCurrent();
				return;
			}
			if (Keys.Hit(Prefs.KeyCancel) || Keys.Hit(Prefs.KeyInteract))
			{
				Close();
				return;
			}
			if (ListNumbers.Pressed(Labels.Count, out var index, out var pick))
			{
				if (pick)
				{
					Choose(index);
					return;
				}
				_index = index;
				SpeakCurrent(force: true);
			}
		}
	}
}
