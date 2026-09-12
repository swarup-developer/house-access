using System;
using System.Collections.Generic;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using TMPro;
using UnityEngine;

namespace HouseAccess.Game;

public static class InputConfigBridge
{
	private static readonly List<RebindActionUI> Rows = new List<RebindActionUI>();

	private static int _index = -1;

	private static bool _wasOpen;

	private static bool _rebinding;

	private static float _nextScan;

	private static bool _openCached;

	private static float _openCheckedAt;

	public static bool Active => _index >= 0 && IsOpen();

	public static void Reset()
	{
		Rows.Clear();
		_index = -1;
		_wasOpen = false;
		_rebinding = false;
	}

	private static bool IsOpen()
	{
		if (Time.unscaledTime - _openCheckedAt < 0.3f)
		{
			return _openCached;
		}
		_openCheckedAt = Time.unscaledTime;
		_openCached = LookForScreen();
		return _openCached;
	}

	private static bool LookForScreen()
	{
		try
		{
			foreach (RebindActionUI r in Cpp.FindAll<RebindActionUI>(activeOnly: true))
			{
				if (Cpp.Alive((UnityEngine.Object)(object)r))
				{
					GameObject val = Cpp.Read(() => ((Component)r).gameObject);
					if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null && val.activeInHierarchy)
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static void Collect()
	{
		Rows.Clear();
		try
		{
			foreach (RebindActionUI r in Cpp.FindAll<RebindActionUI>(activeOnly: true))
			{
				if (Cpp.Alive((UnityEngine.Object)(object)r))
				{
					GameObject val = Cpp.Read(() => ((Component)r).gameObject);
					if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null) && val.activeInHierarchy)
					{
						Rows.Add(r);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("Rebind rows could not be read: " + ex.Message);
		}
		if (_index >= Rows.Count)
		{
			_index = Rows.Count - 1;
		}
		if (_index < 0 && Rows.Count > 0)
		{
			_index = 0;
		}
	}

	private static string LabelOf(RebindActionUI row)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)row))
		{
			return null;
		}
		try
		{
			TextMeshProUGUI label = Cpp.Read(() => row.m_ActionLabel);
			if (Cpp.Alive((UnityEngine.Object)(object)label))
			{
				string text = TextUtil.Clean(Cpp.Read(() => ((TMP_Text)label).text));
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
			// This build exposes no entry-name label and no display-name on the bound
			// action, so rows without a label simply report as unnamed.
		}
		catch
		{
		}
		return null;
	}

	private static string BindingOf(RebindActionUI row)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)row))
		{
			return null;
		}
		try
		{
			TextMeshProUGUI text = Cpp.Read(() => row.m_BindingText);
			if (!Cpp.Alive((UnityEngine.Object)(object)text))
			{
				return null;
			}
			string text2 = TextUtil.Clean(Cpp.Read(() => ((TMP_Text)text).text));
			return string.IsNullOrWhiteSpace(text2) ? "not set" : text2;
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Reads the row under the cursor. <paramref name="force" /> speaks at Critical so a
	/// repeated number key is heard again rather than suppressed as a duplicate.
	/// </summary>
	private static void SpeakCurrent(bool force = false)
	{
		if (_index >= 0 && _index < Rows.Count)
		{
			string value = LabelOf(Rows[_index]) ?? "unnamed";
			string value2 = BindingOf(Rows[_index]) ?? "not set";
			Speaker.Say($"{value}, {value2}, {_index + 1} of {Rows.Count}.", force ? Pri.Critical : Pri.High);
		}
	}

	private static void BeginRebind()
	{
		if (_index < 0 || _index >= Rows.Count)
		{
			return;
		}
		RebindActionUI val = Rows[_index];
		if (!Cpp.Alive((UnityEngine.Object)(object)val))
		{
			return;
		}
		string text = LabelOf(val) ?? "this action";
		Speaker.SayNow("Press the new key for " + text + ". Escape to keep the old one.");
		_rebinding = true;
		try
		{
			val.StartInteractiveRebind();
		}
		catch (Exception ex)
		{
			Log.Warn("Rebind could not start: " + ex.Message);
			_rebinding = false;
			Speaker.SayNow("That could not be changed.");
		}
	}

	private static void ResetOne()
	{
		if (_index < 0 || _index >= Rows.Count)
		{
			return;
		}
		RebindActionUI val = Rows[_index];
		if (Cpp.Alive((UnityEngine.Object)(object)val))
		{
			try
			{
				val.ResetToDefault();
			}
			catch (Exception ex)
			{
				Log.Warn("Reset failed: " + ex.Message);
				return;
			}
			string text = LabelOf(val) ?? "it";
			string text2 = BindingOf(val) ?? "the default";
			Speaker.SayNow(text + " reset to " + text2 + ".");
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
				_rebinding = false;
				Rows.Clear();
			}
			return;
		}
		if (!_wasOpen)
		{
			_wasOpen = true;
			_index = 0;
			Collect();
			Speaker.Say("Input configuration. " + TextUtil.Pluralise(Rows.Count, "action", "actions") + ". Arrows to move, enter to change a key.", Pri.High);
			SpeakCurrent();
			return;
		}
		if (_rebinding)
		{
			if (Time.unscaledTime < _nextScan)
			{
				return;
			}
			_nextScan = Time.unscaledTime + 0.4f;
			bool flag = false;
			try
			{
				// This build keeps no separate overlay object; a rebind is in progress while
				// the row's interactive-rebinding operation is still live.
				flag = Cpp.Read(() => Rows[_index].ongoingRebind, null) != null || Cpp.Read(() => Rows[_index].m_RebindOperation, null) != null;
			}
			catch
			{
			}
			if (!flag)
			{
				_rebinding = false;
				SpeakCurrent();
			}
			return;
		}
		if (Time.unscaledTime >= _nextScan)
		{
			_nextScan = Time.unscaledTime + 1f;
			if (Rows.Count == 0)
			{
				Collect();
			}
		}
		if (Rows.Count != 0)
		{
			if (Keys.Hit(Prefs.KeyUiNext))
			{
				_index = (_index + 1) % Rows.Count;
				SpeakCurrent();
			}
			else if (Keys.Hit(Prefs.KeyUiPrev))
			{
				_index = (_index - 1 + Rows.Count) % Rows.Count;
				SpeakCurrent();
			}
			else if (Keys.Hit(Prefs.KeyUiActivate))
			{
				BeginRebind();
			}
			else if (Keys.Hit(Prefs.KeyRepeatTarget))
			{
				SpeakCurrent();
			}
			else if (Keys.Hit(Prefs.KeyCancel))
			{
				ResetOne();
			}
			// A number reads a row, control and a number starts changing that key. The
			// _rebinding branch above returns before this, so while the game is waiting for
			// a key the digits belong to the game, not to this list.
			else if (ListNumbers.Pressed(Rows.Count, out var index, out var pick))
			{
				_index = index;
				SpeakCurrent(force: true);
				if (pick)
				{
					BeginRebind();
				}
			}
		}
	}
}
