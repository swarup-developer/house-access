using System;
using System.Collections.Generic;
using System.Text;
using HouseAccess.Game;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using UnityEngine;

namespace HouseAccess.World;

public static class Finder
{
	private static readonly List<Entry> Matches = new List<Entry>();

	private static string _typed = string.Empty;

	private static int _index = -1;

	private static bool _open;

	public static bool Active => _open;

	public static void Reset()
	{
		Matches.Clear();
		_typed = string.Empty;
		_index = -1;
		_open = false;
	}

	public static void Toggle()
	{
		if (_open)
		{
			Close("Search closed.");
			return;
		}
		if (!GameRefs.InPlayScene)
		{
			Speaker.SayNow("Only in the house.");
			return;
		}
		_open = true;
		_typed = string.Empty;
		_index = -1;
		Matches.Clear();
		Speaker.SayNow("Search. Type a name. Arrows to move, enter to choose, escape to cancel.");
	}

	private static void Close(string say)
	{
		Reset();
		if (!string.IsNullOrWhiteSpace(say))
		{
			Speaker.SayNow(say);
		}
	}

	private static void Refresh()
	{
		Matches.Clear();
		_index = -1;
		if (_typed.Length == 0)
		{
			return;
		}
		string value = _typed.Trim().ToLowerInvariant();
		foreach (Entry item in Radar.All)
		{
			if (item != null && item.Alive)
			{
				string label = item.Label;
				if (!string.IsNullOrWhiteSpace(label) && label.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					Matches.Add(item);
				}
			}
		}
		Matches.Sort((Entry a, Entry b) => a.Distance.CompareTo(b.Distance));
		if (Matches.Count > 0)
		{
			_index = 0;
		}
	}

	private static void SayMatches()
	{
		if (Matches.Count == 0)
		{
			Speaker.Say(_typed + ". Nothing matches.", Pri.High);
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(TextUtil.Pluralise(Matches.Count, "match", "matches"));
		stringBuilder.Append(". ");
		if (Matches.Count <= 5)
		{
			foreach (Entry match in Matches)
			{
				stringBuilder.Append(match.Label);
				stringBuilder.Append(". ");
			}
		}
		else
		{
			stringBuilder.Append(Matches[0].Label);
			stringBuilder.Append(", nearest.");
		}
		Speaker.Say(TextUtil.Cap(stringBuilder.ToString(), 700), Pri.High);
	}

	private static void SayCurrent()
	{
		if (_index >= 0 && _index < Matches.Count)
		{
			Entry entry = Matches[_index];
			Speaker.Say($"{entry.Label}, {TextUtil.Distance(entry.Distance)}, {_index + 1} of {Matches.Count}.", Pri.High);
		}
	}

	private static void Choose()
	{
		if (_index < 0 || _index >= Matches.Count)
		{
			Close("Nothing chosen.");
			return;
		}
		Entry entry = Matches[_index];
		string label = entry.Label;
		Reset();
		if (Radar.SelectEntry(entry))
		{
			return;
		}
		foreach (Entry item in Radar.All)
		{
			if (item == null || !item.Alive || !string.Equals(item.Label, label, StringComparison.OrdinalIgnoreCase) || !Radar.SelectEntry(item))
			{
				continue;
			}
			return;
		}
		Speaker.SayNow(label + " is no longer nearby.");
	}

	public static void Tick()
	{
		if (_open)
		{
			OpportunityBridge.CloseIfOpen();
			InventoryBridge.CloseIfOpen();
			WheelBridge.CloseIfOpen();
			RadialBridge.CloseIfOpen();
			if (Keys.Down((KeyCode)27) || Keys.Hit(Prefs.KeyCancel))
			{
				Close("Search cancelled.");
			}
			else if (Keys.Hit(Prefs.KeyUiActivate))
			{
				Choose();
			}
			else if (Keys.Hit(Prefs.KeyUiNext) && Matches.Count > 0)
			{
				_index = (_index + 1) % Matches.Count;
				SayCurrent();
			}
			else if (Keys.Hit(Prefs.KeyUiPrev) && Matches.Count > 0)
			{
				_index = (_index - 1 + Matches.Count) % Matches.Count;
				SayCurrent();
			}
			else if (Keys.Hit(Prefs.KeyRepeatTarget))
			{
				Speaker.SayNow((_typed.Length == 0) ? "Nothing typed yet." : _typed);
			}
			else if (ListNumbers.PickPressed(Matches.Count, out var index))
			{
				// Control and a number only. A bare digit belongs in the name being typed,
				// so this list is the one place where a number does not read an item; it has
				// to sit before TypeTick so the chosen match is not also typed. Choose ends
				// in Radar.SelectEntry, which announces the target it settles on, so there
				// is nothing to speak here.
				_index = index;
				Choose();
			}
			else
			{
				TypeTick();
			}
		}
	}

	private static void TypeTick()
	{
		string inputString;
		try
		{
			inputString = Input.inputString;
		}
		catch
		{
			return;
		}
		if (string.IsNullOrEmpty(inputString))
		{
			return;
		}
		bool flag = false;
		string text = inputString;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			switch (c)
			{
			case '\b':
				if (_typed.Length != 0)
				{
					_typed = _typed.Substring(0, _typed.Length - 1);
					flag = true;
				}
				break;
			default:
				if (c != '\r')
				{
					if (!char.IsControl(c))
					{
						_typed += c;
						flag = true;
					}
					break;
				}
				goto case '\n';
			case '\n':
				Choose();
				return;
			}
		}
		if (flag)
		{
			Refresh();
			SayMatches();
		}
	}
}
