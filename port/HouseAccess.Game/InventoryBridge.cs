using System;
using System.Collections.Generic;
using System.Text;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using HouseAccess.World;
using EekCharacterEngine;
using EekCharacterEngine.Canvas;
using EekCharacterEngine.Interaction;
using UnityEngine;

// The inventory component class name on this game build (interop-assembly naming).
using InventoryComp = HKJNFHAKLIE;

namespace HouseAccess.Game;

public static class InventoryBridge
{
	private static InventoryUI _ui;

	private static readonly List<string> Names = new List<string>();

	private static readonly List<string> RawNames = new List<string>();

	private static readonly List<InteractiveItem> Items = new List<InteractiveItem>();

	private static int _index = -1;

	private static int _signature;

	private static bool _wasOpen;

	private static float _nextScan;

	public static bool Active => Names.Count > 0 && IsOpen();

	private static InventoryUI Ui
	{
		get
		{
			if (Cpp.Alive((UnityEngine.Object)(object)_ui))
			{
				return _ui;
			}
			_ui = Cpp.FindOne<InventoryUI>(activeOnly: false);
			return _ui;
		}
	}

	private static InventoryComp Bag
	{
		get
		{
			PlayerCharacter p = GameRefs.Player;
			if ((UnityEngine.Object)(object)p == (UnityEngine.Object)null)
			{
				return null;
			}
			return Cpp.Read(() => p.Inventory);
		}
	}

	public static void Reset()
	{
		_ui = null;
		Names.Clear();
		RawNames.Clear();
		Items.Clear();
		_index = -1;
		_signature = 0;
		_wasOpen = false;
		_nextScan = 0f;
	}

	// The inventory canvas UI state: CanvasBase.Canvas GameObject is active while open.
	private static bool IsOpen()
	{
		InventoryUI u = Ui;
		if (!Cpp.Alive((UnityEngine.Object)(object)u))
		{
			return false;
		}
		try
		{
			CanvasBase cb = ((Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase)u).TryCast<CanvasBase>();
			if (Cpp.Alive((UnityEngine.Object)(object)cb))
			{
				GameObject canvas = Cpp.Read(() => cb.Canvas);
				if (Cpp.Alive((UnityEngine.Object)(object)canvas))
				{
					return canvas.activeInHierarchy;
				}
				return Cpp.Read(() => cb.KHJBOFBICPF, fallback: false);
			}
			return ((Component)u).gameObject.activeInHierarchy;
		}
		catch
		{
			return false;
		}
	}

	public static void CloseIfOpen()
	{
		if (!IsOpen())
		{
			return;
		}
		InventoryUI u = Ui;
		if (!Cpp.Alive((UnityEngine.Object)(object)u))
		{
			return;
		}
		try
		{
			u.Toggle();
		}
		catch (Exception ex)
		{
			Log.Debug("Could not close the bag: " + ex.Message);
		}
	}

	private static bool Refresh()
	{
		// First try reading items currently displayed on the InventoryUI canvas slots.
		List<string> list2 = new List<string>();
		List<InteractiveItem> list3 = new List<InteractiveItem>();
		InventoryUI ui = Ui;
		if (Cpp.Alive((UnityEngine.Object)(object)ui))
		{
			try
			{
				Il2CppSystem.Collections.Generic.List<InventoryUI.LBEMAJLOJKH> display = Cpp.Read(() => ui.EHJCCCEFIHK);
				int count = Cpp.CountOf<InventoryUI.LBEMAJLOJKH>(display);
				for (int i = 0; i < count; i++)
				{
					InventoryUI.LBEMAJLOJKH slot = Cpp.AtOf<InventoryUI.LBEMAJLOJKH>(display, i);
					if (slot == null)
					{
						continue;
					}
					InteractiveItem item = Cpp.Read(() => slot.IDMOLAKGIGK);
					if (Cpp.Alive((UnityEngine.Object)(object)item))
					{
						string name = GameRefs.NameOf(item);
						if (!string.IsNullOrWhiteSpace(name))
						{
							list2.Add(name);
							list3.Add(item);
						}
					}
				}
			}
			catch
			{
			}
		}

		// Fallback to reading player's carried items from the inventory component.
		if (list2.Count == 0)
		{
			InventoryComp bag = Bag;
			if (bag != null)
			{
				Il2CppSystem.Collections.Generic.List<InventoryObject> inv = Cpp.Read(() => bag.HHIDMHMDDIG);
				int n = Cpp.CountOf<InventoryObject>(inv);
				for (int i = 0; i < n; i++)
				{
					InventoryObject obj = Cpp.AtOf<InventoryObject>(inv, i);
					if (obj == null)
					{
						continue;
					}
					string text = Cpp.Read(() => obj.Name);
					if (string.IsNullOrWhiteSpace(text))
					{
						continue;
					}
					list2.Add(text);
					list3.Add(Cpp.Read(() => obj.ScriptReference));
				}
			}
		}
		int num5 = list2.Count * 397;
		foreach (string item in list2)
		{
			num5 = (num5 * 31) ^ item.GetHashCode();
		}
		if (num5 == _signature)
		{
			return false;
		}
		_signature = num5;
		Names.Clear();
		RawNames.Clear();
		Items.Clear();
		for (int num6 = 0; num6 < list2.Count; num6++)
		{
			Names.Add(TextUtil.Clean(list2[num6]));
			RawNames.Add(list2[num6]);
			Items.Add((num6 < list3.Count) ? list3[num6] : null);
		}
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

	public static List<string> Snapshot()
	{
		Refresh();
		return new List<string>(Names);
	}

	public static string DiffAgainst(List<string> before)
	{
		List<string> list = Snapshot();
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>(before);
		foreach (string item in list)
		{
			if (!list3.Remove(item))
			{
				list2.Add(item);
			}
		}
		if (list2.Count > 0)
		{
			return $"Taken. {string.Join(", ", list2)}. Carrying {list.Count}.";
		}
		if (list3.Count > 0)
		{
			return $"{string.Join(", ", list3)} gone. Carrying {list.Count}.";
		}
		return null;
	}

	public static void ReadContents()
	{
		Refresh();
		string text = Activity.Holding();
		if (Names.Count == 0)
		{
			Speaker.SayNow(string.IsNullOrWhiteSpace(text) ? "You are not carrying anything." : ("Holding up " + text + ". Nothing else in the bag."));
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(text))
		{
			stringBuilder.Append("Holding up ");
			stringBuilder.Append(text);
			stringBuilder.Append(". ");
		}
		stringBuilder.Append("Carrying ");
		stringBuilder.Append(TextUtil.Pluralise(Names.Count, "item", "items"));
		stringBuilder.Append(". ");
		for (int i = 0; i < Names.Count && i < 25; i++)
		{
			stringBuilder.Append(Names[i]);
			stringBuilder.Append(". ");
		}
		Speaker.Say(TextUtil.Cap(stringBuilder.ToString(), 1200), Pri.High);
	}

	public static void Tick()
	{
		if (Finder.Active)
		{
			return;
		}
		if (!IsOpen())
		{
			if (_wasOpen)
			{
				_wasOpen = false;
				_index = -1;
			}
			return;
		}
		if (!_wasOpen)
		{
			_wasOpen = true;
			_signature = 0;
			_index = 0;
			if (Refresh() || Names.Count > 0)
			{
				AnnounceOpened();
			}
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
		if (Names.Count == 0)
		{
			Speaker.Say("Inventory. Empty.", Pri.High);
			return;
		}
		Speaker.Say($"Inventory. {TextUtil.Pluralise(Names.Count, "item", "items")}. Enter to use, {Prefs.KeyListInteractions.Value} for options.", Pri.High);
		SpeakCurrent();
		SyncHover();
	}

	/// <summary>
	/// Reads the item under the cursor. <paramref name="force" /> speaks at Critical so a
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
		if (_index < 0 || _index >= Names.Count)
		{
			return;
		}
		InventoryUI ui = Ui;
		if (!Cpp.Alive((UnityEngine.Object)(object)ui))
		{
			return;
		}
		InteractiveItem val = ((_index < Items.Count) ? Items[_index] : null);
		try
		{
			if (Cpp.Alive((UnityEngine.Object)(object)val))
			{
				ui.FocusOnItem(val);
			}
		}
		catch (Exception ex)
		{
			Log.Debug("Inventory hover sync failed: " + ex.Message);
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

	private static void Choose()
	{
		if (_index < 0 || _index >= Names.Count)
		{
			return;
		}
		InventoryUI ui = Ui;
		if (!Cpp.Alive((UnityEngine.Object)(object)ui))
		{
			return;
		}
		string text = Names[_index];
		string text2 = ((_index < RawNames.Count) ? RawNames[_index] : text);
		Speaker.SayNow(text + ".");
		try
		{
			InteractiveItem val = ((_index < Items.Count) ? Items[_index] : null);
			if (Cpp.Alive((UnityEngine.Object)(object)val))
			{
				ui.FocusOnItem(val);
			}
			ui.OnItemClick(text2);
		}
		catch (Exception ex)
		{
			Log.Warn("Inventory click failed: " + ex.Message);
			Speaker.SayNow("That did not work.");
		}
	}

	public static void OpenActions()
	{
		if (_index < 0 || _index >= Items.Count)
		{
			Speaker.SayNow("Nothing selected.");
			return;
		}
		InteractiveItem val = Items[_index];
		if (!Cpp.Alive((UnityEngine.Object)(object)val))
		{
			Speaker.SayNow(Names[_index] + ". No actions available.");
		}
		else
		{
			ActionPicker.Open(val, Names[_index]);
		}
	}

	private static void HandleKeys()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			CloseIfOpen();
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
			Choose();
			return;
		}
		if (Keys.Hit(Prefs.KeyRepeatTarget))
		{
			SpeakCurrent();
			return;
		}
		if (Keys.Hit(Prefs.KeyListInteractions))
		{
			OpenActions();
			return;
		}
		if (Keys.Hit(Prefs.KeyReadScreen))
		{
			ReadContents();
			return;
		}
		// A number used to only move the cursor here, which meant the help was wrong and
		// the keypad did nothing; control and a number now picks the item, the same way
		// it does in dialogue and in the action picker.
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
