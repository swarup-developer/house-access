using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using UnityEngine;

namespace HouseAccess.Game;

// Character descriptions: the mod lets the player keep a written description per
// character (UserData/HouseAccess/descriptions.txt) and announces nudity state.
//
// WORKAROUND: House Access 1.1.0 also read the current outfit of a character
// (Clothes/ClothingAsset/ClothingAssetData/ClothingTypes + per-slot colours). Those
// classes do not exist under those names in this GOG v1.1.7 build; the build exposes
// ClothingList/ClothingItem/ClothingConfiguration instead, whose mapping to the old
// slot/friendly-name/colour API has not been verified in-game. Outfit announcements
// are therefore disabled here until that mapping can be confirmed.
public static class Appearance
{
	private static Dictionary<string, string> _written;

	public static string FilePath => Path.Combine(Diagnostics.Directory, "descriptions.txt");

	public static void Load(IEnumerable<string> knownNames)
	{
		_written = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (!File.Exists(FilePath))
			{
				WriteTemplate(knownNames);
				return;
			}
			string[] array = File.ReadAllLines(FilePath);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i]?.Trim();
				if (string.IsNullOrEmpty(text) || text.StartsWith("#"))
				{
					continue;
				}
				int num = text.IndexOf('=');
				if (num > 0)
				{
					string text2 = text.Substring(0, num).Trim();
					string text3 = text.Substring(num + 1).Trim();
					if (text2.Length != 0 && text3.Length != 0)
					{
						_written[text2] = text3;
					}
				}
			}
			Log.Info($"Loaded {_written.Count} character descriptions from {FilePath}");
		}
		catch (Exception ex)
		{
			Log.Warn("Could not load character descriptions: " + ex.Message);
		}
	}

	private static void WriteTemplate(IEnumerable<string> knownNames)
	{
		try
		{
			Directory.CreateDirectory(Diagnostics.Directory);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("# House Access character descriptions.");
			stringBuilder.AppendLine("#");
			stringBuilder.AppendLine("# One person per line, in the form:   Name = description");
			stringBuilder.AppendLine("# The description is read out when you press the describe key on them,");
			stringBuilder.AppendLine("# between their location and what they are wearing.");
			stringBuilder.AppendLine("#");
			stringBuilder.AppendLine("# Lines starting with a hash are ignored. Entries left empty are skipped.");
			stringBuilder.AppendLine();
			foreach (string knownName in knownNames)
			{
				if (!string.IsNullOrWhiteSpace(knownName))
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2);
					handler.AppendFormatted(knownName.Trim());
					handler.AppendLiteral(" = ");
					stringBuilder2.AppendLine(ref handler);
				}
			}
			File.WriteAllText(FilePath, stringBuilder.ToString());
			Log.Info("Created an empty description file at " + FilePath);
		}
		catch (Exception ex)
		{
			Log.Warn("Could not create the description file: " + ex.Message);
		}
	}

	private static string WrittenFor(string name)
	{
		if (_written == null || string.IsNullOrWhiteSpace(name))
		{
			return null;
		}
		string value;
		return (_written.TryGetValue(name.Trim(), out value) && !string.IsNullOrWhiteSpace(value)) ? value : null;
	}

	/// <summary>Nudity state of a character (outfit details are unavailable on this build).</summary>
	public static string ClothingOf(Character c)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)c))
		{
			return null;
		}
		try
		{
			if (Cpp.Read(() => c.IsNaked, fallback: false))
			{
				return "naked";
			}
			if (Cpp.Read(() => c.IsTopless, fallback: false))
			{
				return "topless";
			}
			if (Cpp.Read(() => c.IsBottomless, fallback: false))
			{
				return "bottomless";
			}
		}
		catch
		{
			return null;
		}
		return null;
	}

	public static void Describe(Character c)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)c))
		{
			Speaker.SayNow("Select a person first.");
			return;
		}
		string text = GameRefs.NameOf(c) ?? "Someone";
		List<string> list = new List<string> { text };
		string text2 = GameRefs.RoomOf(c);
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add("in the " + TextUtil.Humanize(text2));
		}
		string text3 = WrittenFor(text);
		if (!string.IsNullOrWhiteSpace(text3))
		{
			list.Add(TextUtil.Clean(text3));
		}
		string text4 = Activity.Describe(c);
		if (!string.IsNullOrWhiteSpace(text4))
		{
			list.Add(text4);
		}
		string text5 = ClothingOf(c);
		if (!string.IsNullOrWhiteSpace(text5))
		{
			list.Add(text5);
		}
		// Note: this game build does not expose a "last thing said" line on characters.
		Speaker.SayParts(Pri.High, list.ToArray());
	}
}
