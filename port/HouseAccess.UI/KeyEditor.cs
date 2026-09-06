using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using MelonLoader;
using UnityEngine;

namespace HouseAccess.UI;

public static class KeyEditor
{
	private static readonly List<FieldInfo> Fields = new List<FieldInfo>();

	private static int _index = -1;

	private static bool _open;

	private static bool _listening;

	private static readonly KeyCode[] GameKeys;

	public static bool Active => _open;

	public static void Reset()
	{
		_open = false;
		_listening = false;
		_index = -1;
		Fields.Clear();
	}

	public static void Toggle()
	{
		if (_open)
		{
			Close();
			return;
		}
		Collect();
		if (Fields.Count == 0)
		{
			Speaker.SayNow("No keys to change.");
			return;
		}
		_open = true;
		_index = 0;
		_listening = false;
		Speaker.SayNow($"Key setup. {Fields.Count} keys. Arrows to move, enter to change one, " + "comma to close.");
		SpeakCurrent();
	}

	private static void Close()
	{
		_open = false;
		_listening = false;
		try
		{
			MelonPreferences.Save();
		}
		catch
		{
		}
		Speaker.SayNow("Key setup closed.");
	}

	private static void Collect()
	{
		Fields.Clear();
		FieldInfo[] fields = typeof(Prefs).GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.Name.StartsWith("Key") && fieldInfo.GetValue(null) is MelonPreferences_Entry<string>)
			{
				Fields.Add(fieldInfo);
			}
		}
		Fields.Sort((FieldInfo a, FieldInfo b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
	}

	private static MelonPreferences_Entry<string> EntryAt(int i)
	{
		if (i < 0 || i >= Fields.Count)
		{
			return null;
		}
		return Fields[i].GetValue(null) as MelonPreferences_Entry<string>;
	}

	private static string NameAt(int i)
	{
		if (i < 0 || i >= Fields.Count)
		{
			return null;
		}
		string text = Fields[i].Name;
		if (text.StartsWith("Key"))
		{
			text = text.Substring(3);
		}
		return TextUtil.Humanize(text).ToLowerInvariant();
	}

	/// <summary>
	/// Reads the row under the cursor. <paramref name="force" /> speaks at Critical so a
	/// repeated number key is heard again rather than suppressed as a duplicate.
	/// </summary>
	private static void SpeakCurrent(bool force = false)
	{
		MelonPreferences_Entry<string> val = EntryAt(_index);
		if (val != null)
		{
			string value = (string.IsNullOrWhiteSpace(val.Value) ? "not set" : Spoken(val.Value));
			Speaker.Say($"{NameAt(_index)}, {value}, {_index + 1} of {Fields.Count}.", force ? Pri.Critical : Pri.High);
		}
	}

	private static string Spoken(string binding)
	{
		return binding.Replace("+", " ").Replace("PageUp", "page up").Replace("PageDown", "page down")
			.Replace("Ctrl", "control")
			.Replace("Alt", "alt")
			.Replace("Shift", "shift")
			.ToLowerInvariant();
	}

	private static void BeginListening()
	{
		_listening = true;
		Speaker.SayNow("Press the new key for " + NameAt(_index) + ". Comma to keep the one it has.");
	}

	private static void ListenTick()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Invalid comparison between Unknown and I4
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Invalid comparison between Unknown and I4
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (Keys.Down((KeyCode)44))
		{
			_listening = false;
			Speaker.SayNow("Left as it was.");
			SpeakCurrent();
			return;
		}
		KeyCode val = (KeyCode)0;
		foreach (KeyCode value2 in Enum.GetValues(typeof(KeyCode)))
		{
			if ((int)value2 == 0 || IsModifier(value2) || !Keys.Down(value2))
			{
				continue;
			}
			val = value2;
			break;
		}
		if ((int)val == 0)
		{
			return;
		}
		string text = Describe(val);
		MelonPreferences_Entry<string> val3 = EntryAt(_index);
		if (val3 == null)
		{
			_listening = false;
			return;
		}
		val3.Value = text;
		Prefs.ClearChordCache();
		try
		{
			MelonPreferences.Save();
		}
		catch
		{
		}
		_listening = false;
		string value = WarnAbout(text, _index);
		Speaker.SayNow(string.IsNullOrWhiteSpace(value) ? (NameAt(_index) + " is now " + Spoken(text) + ".") : $"{NameAt(_index)} is now {Spoken(text)}. {value}");
	}

	private static bool IsModifier(KeyCode c)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		return (int)c == 306 || (int)c == 305 || (int)c == 308 || (int)c == 307 || (int)c == 304 || (int)c == 303;
	}

	private unsafe static string Describe(KeyCode pressed)
	{
		string text = string.Empty;
		if (Keys.Ctrl)
		{
			text += "Ctrl+";
		}
		if (Keys.Alt)
		{
			text += "Alt+";
		}
		if (Keys.Shift)
		{
			text += "Shift+";
		}
		return text + ((object)(*(KeyCode*)(&pressed))/*cast due to .constrained prefix*/).ToString();
	}

	private static string WarnAbout(string binding, int skip)
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		List<string> list = new List<string>();
		for (int i = 0; i < Fields.Count; i++)
		{
			if (i != skip)
			{
				MelonPreferences_Entry<string> val = EntryAt(i);
				if (val != null && string.Equals(val.Value, binding, StringComparison.OrdinalIgnoreCase))
				{
					list.Add(NameAt(i));
				}
			}
		}
		string b = (binding.Contains("+") ? binding.Substring(binding.LastIndexOf('+') + 1) : binding);
		bool flag = false;
		KeyCode[] gameKeys = GameKeys;
		for (int j = 0; j < gameKeys.Length; j++)
		{
			if (string.Equals(((object)gameKeys[j]/*cast due to .constrained prefix*/).ToString(), b, StringComparison.OrdinalIgnoreCase))
			{
				flag = true;
				break;
			}
		}
		List<string> list2 = new List<string>();
		if (list.Count > 0)
		{
			list2.Add("Also used by " + string.Join(" and ", list) + ".");
		}
		if (flag)
		{
			list2.Add("The game uses this key too, so it will do both things.");
		}
		return (list2.Count == 0) ? null : string.Join(" ", list2);
	}

	public static void Tick()
	{
		if (_open)
		{
			if (_listening)
			{
				ListenTick();
			}
			else if (Keys.Hit(Prefs.KeyUiNext))
			{
				_index = (_index + 1) % Fields.Count;
				SpeakCurrent();
			}
			else if (Keys.Hit(Prefs.KeyUiPrev))
			{
				_index = (_index - 1 + Fields.Count) % Fields.Count;
				SpeakCurrent();
			}
			else if (Keys.Hit(Prefs.KeyUiActivate))
			{
				BeginListening();
			}
			else if (Keys.Hit(Prefs.KeyRepeatTarget))
			{
				SpeakCurrent();
			}
			else if (Keys.Hit(Prefs.KeyCancel))
			{
				Close();
			}
			// Numbers jump to a row here as well, so the rule the help describes holds even
			// in the mod's own key list. It sits after the checks above because _listening
			// short-circuits the whole chain: while a new key is being captured, a digit is
			// a binding the user is choosing, not a row number.
			else if (ListNumbers.Pressed(Fields.Count, out var index, out var pick))
			{
				_index = index;
				SpeakCurrent(force: true);
				if (pick)
				{
					BeginListening();
				}
			}
		}
	}

	// The original build shipped the game's own bound keys in this array (used only to
	// warn when a mod key collides with a game key). That metadata could not be recovered
	// for this game build, so the warning is disabled by leaving the list empty.
	static KeyEditor()
	{
		GameKeys = new KeyCode[0];
	}
}
