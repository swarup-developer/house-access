using System;
using System.Collections.Generic;
using System.Text;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using HouseAccess.World;
using EekCharacterEngine.Canvas;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HouseAccess.Game;

public static class OpportunityBridge
{
	private static OpportunityWindowManager _window;

	private static readonly List<string> Entries = new List<string>();

	private static int _index = -1;

	private static bool _wasOpen;

	private static float _nextScan;

	public static bool Active => Entries.Count > 0 && IsOpen();

	private static OpportunityWindowManager Window
	{
		get
		{
			if (Cpp.Alive((UnityEngine.Object)(object)_window))
			{
				return _window;
			}
			_window = Cpp.FindOne<OpportunityWindowManager>(activeOnly: false);
			return _window;
		}
	}

	public static void Reset()
	{
		_window = null;
		Entries.Clear();
		_index = -1;
		_wasOpen = false;
		_nextScan = 0f;
	}

	public static void CloseIfOpen()
	{
		OpportunityWindowManager w = Window;
		if (!Cpp.Alive((UnityEngine.Object)(object)w))
		{
			return;
		}
		try
		{
			if (IsOpen())
			{
				w.Toggle();
			}
		}
		catch (Exception ex)
		{
			Log.Debug("Could not close the opportunity window: " + ex.Message);
		}
		Entries.Clear();
	}

	private static bool IsOpen()
	{
		OpportunityWindowManager w = Window;
		if (!Cpp.Alive((UnityEngine.Object)(object)w))
		{
			return false;
		}
		try
		{
			CanvasBase cb = ((Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase)w).TryCast<CanvasBase>();
			if (Cpp.Alive((UnityEngine.Object)(object)cb))
			{
				GameObject canvas = Cpp.Read(() => cb.Canvas);
				if (Cpp.Alive((UnityEngine.Object)(object)canvas))
				{
					return canvas.activeInHierarchy;
				}
				return Cpp.Read(() => cb.KHJBOFBICPF, fallback: false);
			}
			return ((Component)w).gameObject.activeInHierarchy;
		}
		catch
		{
			return false;
		}
	}

	private static bool ShowingMemories()
	{
		OpportunityWindowManager w = Window;
		if (!Cpp.Alive((UnityEngine.Object)(object)w))
		{
			return false;
		}
		try
		{
			if (Cpp.Read(() => w.DGHOLHNLLLJ, fallback: false))
			{
				return true;
			}
			ScrollRect sr = Cpp.Read(() => w._memoriesScrollRect);
			if (Cpp.Alive((UnityEngine.Object)(object)sr))
			{
				return ((Component)sr).gameObject.activeInHierarchy;
			}
		}
		catch
		{
		}
		return false;
	}

	private static void AddPart(string text, float y, HashSet<string> seen, List<KeyValuePair<float, string>> list)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string text2 = TextUtil.Clean(text);
		if (!string.IsNullOrWhiteSpace(text2) && seen.Add(text2))
		{
			list.Add(new KeyValuePair<float, string>(y, text2));
		}
	}

	private static string RowText(GameObject go)
	{
		List<KeyValuePair<float, string>> list = new List<KeyValuePair<float, string>>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (Text t in go.GetComponentsInChildren<Text>(false))
			{
				if (Cpp.Alive((UnityEngine.Object)(object)t))
				{
					AddPart(Cpp.Read(() => t.text), Cpp.Read(() => ((Component)t).transform.position.y, 0f), seen, list);
				}
			}
			foreach (TMP_Text t in go.GetComponentsInChildren<TMP_Text>(false))
			{
				if (Cpp.Alive((UnityEngine.Object)(object)t))
				{
					AddPart(Cpp.Read(() => t.text), Cpp.Read(() => ((Component)t).transform.position.y, 0f), seen, list);
				}
			}
		}
		catch
		{
		}
		list.Sort((KeyValuePair<float, string> a, KeyValuePair<float, string> b) => b.Key.CompareTo(a.Key));
		StringBuilder sb = new StringBuilder();
		foreach (KeyValuePair<float, string> kvp in list)
		{
			if (sb.Length > 0)
			{
				sb.Append(", ");
			}
			sb.Append(kvp.Value);
		}
		return sb.ToString();
	}

	private static bool SameAs(List<string> other)
	{
		if (other.Count != Entries.Count)
		{
			return false;
		}
		for (int i = 0; i < other.Count; i++)
		{
			if (!string.Equals(other[i], Entries[i], StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		return true;
	}

	private static bool Build()
	{
		OpportunityWindowManager w = Window;
		if (!Cpp.Alive((UnityEngine.Object)(object)w))
		{
			return false;
		}
		bool memories = ShowingMemories();
		Il2CppSystem.Collections.Generic.List<GameObject> list = (memories ? Cpp.Read(() => w.OEHJHFHEHBB) : Cpp.Read(() => w.KMOEKPKKJKP));
		int num = Cpp.CountOf<GameObject>(list);
		List<string> list2 = new List<string>();
		for (int i = 0; i < num; i++)
		{
			GameObject val = Cpp.AtOf<GameObject>(list, i);
			if (Cpp.Alive((UnityEngine.Object)(object)val) && val.activeInHierarchy)
			{
				string text = RowText(val);
				if (!string.IsNullOrWhiteSpace(text))
				{
					list2.Add(text);
				}
			}
		}

		if (list2.Count > 0)
		{
			list2.Insert(0, memories ? $"Memories, {list2.Count}." : $"Opportunities, {list2.Count}.");
		}
		else
		{
			list2.Add(memories ? "No memories recorded yet." : "No opportunities recorded yet.");
		}

		if (SameAs(list2))
		{
			return false;
		}
		Entries.Clear();
		Entries.AddRange(list2);
		if (_index >= Entries.Count)
		{
			_index = Entries.Count - 1;
		}
		if (_index < 0 && Entries.Count > 0)
		{
			_index = 0;
		}
		return true;
	}

	public static void Tick()
	{
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
			_index = 0;
			Entries.Clear();
			Build();
			AnnounceOpened();
		}
		else if (Time.unscaledTime >= _nextScan)
		{
			_nextScan = Time.unscaledTime + 0.6f;
			Build();
		}
		if (Entries.Count > 0)
		{
			HandleKeys();
		}
	}

	private static void AnnounceOpened()
	{
		if (Entries.Count == 0)
		{
			Speaker.Say(ShowingMemories() ? "Memories. Empty." : "Opportunities. Empty.", Pri.High);
			return;
		}
		Speaker.Say(ShowingMemories() ? "Memories. Arrow keys to read them, Tab to switch tab." : "Opportunities. Arrow keys to read them, Tab to switch tab.", Pri.High);
		SpeakCurrent();
	}

	private static void SpeakCurrent()
	{
		if (_index >= 0 && _index < Entries.Count)
		{
			Speaker.Say($"{Entries[_index]} {_index + 1} of {Entries.Count}.", Pri.High);
		}
	}

	private static void Move(int dir)
	{
		if (Entries.Count != 0)
		{
			_index = (_index + dir + Entries.Count) % Entries.Count;
			SpeakCurrent();
		}
	}

	public static void ReadAll()
	{
		if (Entries.Count == 0)
		{
			Build();
		}
		if (Entries.Count == 0)
		{
			Speaker.Say("No entries to read.", Pri.High);
			return;
		}
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < Entries.Count && i < 20; i++)
		{
			sb.Append(Entries[i]);
			sb.Append(". ");
		}
		Speaker.Say(TextUtil.Cap(sb.ToString(), 1600), Pri.High);
	}

	private static void SwitchTab()
	{
		OpportunityWindowManager w = Window;
		if (!Cpp.Alive((UnityEngine.Object)(object)w))
		{
			return;
		}
		try
		{
			if (ShowingMemories())
			{
				w.ShowOpportunities();
			}
			else
			{
				w.ShowMemories();
			}
			Entries.Clear();
			_index = 0;
			Build();
			AnnounceOpened();
		}
		catch (Exception ex)
		{
			Log.Warn("Could not switch tab: " + ex.Message);
		}
	}

	private static void HandleKeys()
	{
		if (ActionPicker.Active || WheelBridge.Active || Hands.Active)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			CloseIfOpen();
			return;
		}
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			SwitchTab();
			return;
		}
		if (Keys.Hit(Prefs.KeyUiNext) || Input.GetKeyDown(KeyCode.DownArrow))
		{
			Move(1);
			return;
		}
		if (Keys.Hit(Prefs.KeyUiPrev) || Input.GetKeyDown(KeyCode.UpArrow))
		{
			Move(-1);
			return;
		}
		if (Keys.Hit(Prefs.KeyRepeatTarget))
		{
			SpeakCurrent();
			return;
		}
		if (Keys.Hit(Prefs.KeyReadScreen))
		{
			ReadAll();
			return;
		}
		for (int i = 0; i < Math.Min(9, Entries.Count); i++)
		{
			if (Input.GetKeyDown((KeyCode)(KeyCode.Alpha1 + i)) || Input.GetKeyDown((KeyCode)(KeyCode.Keypad1 + i)))
			{
				_index = i;
				SpeakCurrent();
				break;
			}
		}
	}
}
