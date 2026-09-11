using System;
using System.Collections.Generic;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using HouseAccess.World;
using EekCharacterEngine;
using EekCharacterEngine.Canvas;
using EekCharacterEngine.Interaction;
using EekUI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HouseAccess.Game;

public static class UseWithBridge
{
	private static UseSelectUI _ui;

	private static readonly List<string> Names = new List<string>();

	private static readonly List<Button> Buttons = new List<Button>();

	private static int _signature;

	private static int _index = -1;

	private static bool _wasOpen;

	private static float _nextScan;

	public static bool Active => Names.Count > 0 && IsOpen();

	private static UseSelectUI Ui
	{
		get
		{
			if (Cpp.Alive((UnityEngine.Object)(object)_ui))
			{
				return _ui;
			}
			_ui = Cpp.FindOne<UseSelectUI>(activeOnly: false);
			return _ui;
		}
	}

	public static void Reset()
	{
		_ui = null;
		Names.Clear();
		Buttons.Clear();
		_signature = 0;
		_index = -1;
		_wasOpen = false;
		_nextScan = 0f;
	}

	private static bool IsOpen()
	{
		// The manager remains active even when the inventory selection is hidden.
		UseSelectUI u = Ui;
		if (!Cpp.Alive((UnityEngine.Object)(object)u))
		{
			return false;
		}
		try
		{
			return u.IsShowing;
		}
		catch
		{
			return false;
		}
	}

	private static string Target()
	{
		UseSelectUI u = Ui;
		if (!Cpp.Alive((UnityEngine.Object)(object)u))
		{
			return null;
		}
		InteractiveItem val = Cpp.Read(() => u._interactingWith);
		return Cpp.Alive((UnityEngine.Object)(object)val) ? GameRefs.NameOf(val) : null;
	}

	private static bool Refresh()
	{
		UseSelectUI ui = Ui;
		if (!Cpp.Alive((UnityEngine.Object)(object)ui))
		{
			return false;
		}
		List<Button> list = new List<Button>();
		List<string> list2 = new List<string>();
		try
		{
			Il2CppArrayBase<Button> componentsInChildren = ((Component)ui).GetComponentsInChildren<Button>(false);
			if (componentsInChildren == null)
			{
				return false;
			}
			foreach (Button b in componentsInChildren)
			{
				if (Cpp.Alive((UnityEngine.Object)(object)b) && Cpp.Read(() => ((UIBehaviour)b).IsActive(), fallback: false) && Cpp.Read(() => ((Selectable)b).IsInteractable(), fallback: false))
				{
					string text = LabelOf(b);
					if (!string.IsNullOrWhiteSpace(text))
					{
						list.Add(b);
						list2.Add(text);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("Use-with scan failed: " + ex.Message);
			return false;
		}
		int num = list2.Count * 397;
		foreach (string item in list2)
		{
			num = (num * 31) ^ item.GetHashCode();
		}
		if (num == _signature)
		{
			return false;
		}
		_signature = num;
		Buttons.Clear();
		Buttons.AddRange(list);
		Names.Clear();
		Names.AddRange(list2);
		if (_index >= Names.Count)
		{
			_index = Names.Count - 1;
		}
		if (_index < 0 && Names.Count > 0)
		{
			_index = 0;
		}
		return true;
	}

	private static string LabelOf(Button b)
	{
		try
		{
			GameObject val = Cpp.Read(() => ((Component)b).gameObject);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				return null;
			}
			Il2CppArrayBase<Text> componentsInChildren = val.GetComponentsInChildren<Text>(false);
			if (componentsInChildren != null)
			{
				foreach (Text t in componentsInChildren)
				{
					if (!((UnityEngine.Object)(object)t == (UnityEngine.Object)null))
					{
						string text = TextUtil.Clean(Cpp.Read(() => t.text));
						if (!string.IsNullOrWhiteSpace(text))
						{
							return text;
						}
					}
				}
			}
			Il2CppArrayBase<TMP_Text> componentsInChildren2 = val.GetComponentsInChildren<TMP_Text>(false);
			if (componentsInChildren2 != null)
			{
				foreach (TMP_Text t2 in componentsInChildren2)
				{
					if (!((UnityEngine.Object)(object)t2 == (UnityEngine.Object)null))
					{
						string text2 = TextUtil.Clean(Cpp.Read(() => t2.text));
						if (!string.IsNullOrWhiteSpace(text2))
						{
							return text2;
						}
					}
				}
			}
			return TextUtil.Humanize(((UnityEngine.Object)val).name);
		}
		catch
		{
			return null;
		}
	}

	public static void Tick()
	{
		if (!IsOpen())
		{
			if (_wasOpen)
			{
				_wasOpen = false;
				_index = -1;
				Names.Clear();
			}
			return;
		}
		if (!_wasOpen)
		{
			_wasOpen = true;
			_index = 0;
			Refresh();
			AnnounceOpened();
		}
		else if (Time.unscaledTime >= _nextScan)
		{
			_nextScan = Time.unscaledTime + 0.5f;
			Refresh();
		}
		if (Names.Count > 0)
		{
			HandleKeys();
		}
	}

	private static void AnnounceOpened()
	{
		string text = Target();
		string text2 = (string.IsNullOrWhiteSpace(text) ? string.Empty : (" on " + text));
		if (Names.Count == 0)
		{
			Speaker.Say("Nothing to use" + text2 + ".", Pri.High);
			return;
		}
		bool flag = false;
		try
		{
			UseSelectUI u = Ui;
			InteractiveItem val = (Cpp.Alive((UnityEngine.Object)(object)u) ? Cpp.Read(() => u._interactingWith) : null);
			flag = Cpp.Alive((UnityEngine.Object)(object)val) && Cpp.Alive((UnityEngine.Object)(object)((Component)val).GetComponentInParent<Character>());
		}
		catch
		{
		}
		string text3 = (flag ? ("Give what" + text2 + "?") : ("Use what" + text2 + "?"));
		Speaker.Say(text3 + " " + TextUtil.Pluralise(Names.Count, "item", "items") + ".", Pri.High);
		SpeakCurrent();
		SyncHover();
	}

	/// <summary>
	/// Reads the target under the cursor. <paramref name="force" /> speaks at Critical so a
	/// repeated number key is heard again rather than suppressed as a duplicate.
	/// </summary>
	private static void SpeakCurrent(bool force = false)
	{
		if (_index >= 0 && _index < Names.Count)
		{
			Speaker.Say($"{Names[_index]}, {_index + 1} of {Names.Count}.", force ? Pri.Critical : Pri.High);
		}
	}

	private static void SyncHover()
	{
		if (_index < 0 || _index >= Buttons.Count)
		{
			return;
		}
		Button val = Buttons[_index];
		if (!Cpp.Alive((UnityEngine.Object)(object)val))
		{
			return;
		}
		try
		{
			((Selectable)val).Select();
		}
		catch
		{
		}
	}

	private static void Move(int dir)
	{
		if (Names.Count != 0)
		{
			_index = (_index + dir + Names.Count) % Names.Count;
			SpeakCurrent();
			SyncHover();
		}
	}

	private static void Close()
	{
		UseSelectUI ui = Ui;
		bool wasOpen = false;
		try
		{
			wasOpen = Cpp.Alive((UnityEngine.Object)(object)ui) && ui.IsShowing;
		}
		catch
		{
		}
		Reset();
		try
		{
			// This build has no UseSelectUI.OnClose; Toggle() is the game's own close path.
			if (wasOpen && Cpp.Alive((UnityEngine.Object)(object)ui))
			{
				ui.Toggle();
			}
		}
		catch
		{
		}
		Speaker.SayNow("Closed.");
	}

	private static void Choose()
	{
		if (_index < 0 || _index >= Names.Count || _index >= Buttons.Count)
		{
			return;
		}
		Button val = Buttons[_index];
		if (!Cpp.Alive((UnityEngine.Object)(object)val))
		{
			return;
		}
		string text = Names[_index];
		string text2 = Target();
		Speaker.SayNow(string.IsNullOrWhiteSpace(text2) ? (text + ".") : (text + " on " + text2 + "."));
		try
		{
			((UnityEvent)val.onClick).Invoke();
		}
		catch (Exception ex)
		{
			Log.Warn("Use-with click failed: " + ex.Message);
			Speaker.SayNow("That did not work.");
		}
	}

	private static void HandleKeys()
	{
		if (ActionPicker.Active || WheelBridge.Active || WheelBridge.Opening || Hands.Active || ActionPicker.Active || WheelBridge.Active || WheelBridge.Opening || Hands.Active || ActionPicker.Active || Hands.Active)
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
		if (Keys.Down((KeyCode)275))
		{
			Move(1);
			return;
		}
		if (Keys.Down((KeyCode)276))
		{
			Move(-1);
			return;
		}
		if (Keys.Hit(Prefs.KeyCancel))
		{
			Close();
			return;
		}
		if (Keys.Hit(Prefs.KeyUiActivate))
		{
			Choose();
			return;
		}
		if (Keys.Hit(Prefs.KeyRepeatTarget))
		{
			SpeakCurrent();
			return;
		}
		// Same rule as everywhere else: the number reads the target, control and the number
		// uses the held item on it. Before this, a number here only moved the cursor, so the
		// spoken help promised a pick that never happened.
		if (ListNumbers.Pressed(Names.Count, out var index, out var pick))
		{
			_index = index;
			SpeakCurrent(force: true);
			SyncHover();
			if (pick)
			{
				Choose();
			}
		}
	}
}
