using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HouseAccess.UI;

public static class Overrides
{
	private static Dictionary<string, string> _map;

	private static readonly string[][] Defaults = new string[23][]
	{
		new string[2] { "LanguageButton", "Language" },
		new string[2] { "GlobeButton", "Language" },
		new string[2] { "DiscordButton", "Discord, click to join" },
		new string[2] { "LeftArrow", "Previous character" },
		new string[2] { "RightArrow", "Next character" },
		new string[2] { "ArrowLeft", "Previous character" },
		new string[2] { "ArrowRight", "Next character" },
		new string[2] { "CustomizeButton", "Customize character" },
		new string[2] { "CustomStoriesButton", "Custom stories" },
		new string[2] { "Audio", "Audio settings" },
		new string[2] { "Graphics", "Graphics settings" },
		new string[2] { "Gameplay", "Gameplay settings" },
		new string[2] { "Settings", "Settings" },
		new string[2] { "SettingsManager", "Settings" },
		new string[2] { "SaveSettings", "Save settings" },
		new string[2] { "InitializeSettings", "Settings" },
		new string[2] { "MainSettings", "Main settings" },
		new string[2] { "Censorship", "Censorship settings" },
		new string[2] { "Slider", "Slider" },
		new string[2] { "Toggle", "Toggle" },
		new string[2] { "Dropdown", "Dropdown" },
		new string[2] { "Button", "Button" },
		new string[2] { "Apply", "Apply" }
	};

	public static string FilePath => Path.Combine(Diagnostics.Directory, "labels.txt");

	public static void Load()
	{
		_map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (!File.Exists(FilePath))
			{
				WriteTemplate();
			}
			if (!File.Exists(FilePath))
			{
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
						_map[text2] = text3;
					}
				}
			}
			Log.Info($"Loaded {_map.Count} label overrides.");
		}
		catch (Exception ex)
		{
			Log.Warn("Could not load label overrides: " + ex.Message);
		}
	}

	private static void WriteTemplate()
	{
		try
		{
			Directory.CreateDirectory(Diagnostics.Directory);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("# Spoken names for buttons that have no text of their own.");
			stringBuilder.AppendLine("#");
			stringBuilder.AppendLine("# One per line:   ObjectName = Spoken name");
			stringBuilder.AppendLine("#");
			stringBuilder.AppendLine("# If a control announces itself as something meaningless, add its name");
			stringBuilder.AppendLine("# here. Matching ignores case, and a partial match is enough, so");
			stringBuilder.AppendLine("# \"Arrow\" will catch ArrowLeft_01 as well.");
			stringBuilder.AppendLine();
			string[][] defaults = Defaults;
			foreach (string[] array in defaults)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2);
				handler.AppendFormatted(array[0]);
				handler.AppendLiteral(" = ");
				handler.AppendFormatted(array[1]);
				stringBuilder2.AppendLine(ref handler);
			}
			File.WriteAllText(FilePath, stringBuilder.ToString());
			Log.Info("Created a label override file at " + FilePath);
		}
		catch (Exception ex)
		{
			Log.Warn("Could not create the label file: " + ex.Message);
		}
	}

	public static string For(string objectName)
	{
		if (_map == null || string.IsNullOrWhiteSpace(objectName))
		{
			return null;
		}
		if (_map.TryGetValue(objectName.Trim(), out var value))
		{
			return value;
		}
		foreach (KeyValuePair<string, string> item in _map)
		{
			if (objectName.IndexOf(item.Key, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return item.Value;
			}
		}
		return null;
	}
}
