using System;
using System.Collections.Generic;
using System.IO;
using HouseAccess.Speech;
using HouseAccess.UI;
using HouseAccess.Util;
using EekCharacterEngine.Canvas;
using HouseParty;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Speaker = HouseAccess.Speech.Speaker;

namespace HouseAccess.Game;

public static class SettingsBridge
{
	// The SettingsManager is patched directly in Patches.cs via Harmony postfixes
	// (OnSettingsInitialized / OnSettingsChanged). This bridge exists only to detect
	// the settings canvas via the MenuReader when PatchPreferences is off.

	private static readonly HashSet<string> ScannedCanvases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public static bool Active => MenuReader.Active && IsSettingsCanvas();

	public static void Reset()
	{
		ScannedCanvases.Clear();
	}

	public static void Tick()
	{
	}

	/// <summary>
	/// Reads the current values of all visible settings controls on screen and
	/// announces them. Called after a settings change is applied.
	/// </summary>
	public static void AnnounceCurrentSettings()
	{
		try
		{
			// Find settings-related canvases.
			foreach (CanvasBase canvas in Cpp.FindAll<CanvasBase>(activeOnly: true))
			{
				if (!Cpp.Alive((UnityEngine.Object)(object)canvas))
					continue;
				try
				{
					GameObject go = Cpp.Read(() => ((Component)canvas).gameObject);
					if ((UnityEngine.Object)(object)go == (UnityEngine.Object)null || !go.activeInHierarchy)
						continue;
	
					string canvasName = go.name ?? string.Empty;
					if (canvasName.IndexOf("setting", StringComparison.OrdinalIgnoreCase) < 0
						&& canvasName.IndexOf("option", StringComparison.OrdinalIgnoreCase) < 0)
						continue;
	
	
					// Collect all sliders with their values.
					List<string> announcements = new List<string>();
					Il2CppArrayBase<Slider> sliders = go.GetComponentsInChildren<Slider>(true);
					if (sliders != null)
					{
						foreach (Slider s in sliders)
						{
							if ((UnityEngine.Object)(object)s == (UnityEngine.Object)null
								|| !((UIBehaviour)s).IsActive())
								continue;
							try
							{
								string label = ActiveTextIn(((Component)s).gameObject);
								if (string.IsNullOrWhiteSpace(label))
									label = Overrides.For(((UnityEngine.Object)((Component)s).gameObject).name) ?? ((UnityEngine.Object)((Component)s).gameObject).name;
								label = TextUtil.Clean(label);
								if (string.IsNullOrWhiteSpace(label))
									continue;
	
								float val = Cpp.Read(() => s.value, fallback: 0f);
								float min = Cpp.Read(() => s.minValue, fallback: 0f);
								float max = Cpp.Read(() => s.maxValue, fallback: 1f);
								if (Mathf.Approximately(min, max))
									continue;
	
	
								// Normalise and format.
								float pct = (val - min) / (max - min);
								announcements.Add(label + ", " + (pct * 100f).ToString("0") + " percent.");
							}
							catch
							{
							}
						}
					}
	
					// Collect all toggles.
					Il2CppArrayBase<Toggle> toggles = go.GetComponentsInChildren<Toggle>(true);
					if (toggles != null)
					{
						foreach (Toggle t in toggles)
						{
							if ((UnityEngine.Object)(object)t == (UnityEngine.Object)null
								|| !((UIBehaviour)t).IsActive())
								continue;
							try
							{
								string label = ActiveTextIn(((Component)t).gameObject);
								if (string.IsNullOrWhiteSpace(label))
									label = Overrides.For(((UnityEngine.Object)((Component)t).gameObject).name) ?? ((UnityEngine.Object)((Component)t).gameObject).name;
								label = TextUtil.Clean(label);
								if (string.IsNullOrWhiteSpace(label))
									continue;
	
	
								bool isOn = Cpp.Read(() => t.isOn, fallback: false);
								announcements.Add(label + ", " + (isOn ? "on" : "off") + ".");
							}
							catch
							{
							}
						}
					}
	
					if (announcements.Count > 0)
					{
						Speaker.Say("Settings: " + string.Join(", ", announcements), Pri.High);
						return;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		Speaker.Say("Settings changed.", Pri.High);
	}

	/// <summary>
	/// Called once at mod startup. Scans the current screen for settings-related UI
	/// controls that have no readable text, and logs their raw names to unlabeled.txt
	/// so the user can add label overrides in labels.txt.
	/// </summary>
	public static void ScanAndLogSettingsControls()
	{
		try
		{
			foreach (CanvasBase canvas in Cpp.FindAll<CanvasBase>(activeOnly: true))
			{
				if (!Cpp.Alive((UnityEngine.Object)(object)canvas))
					continue;
				try
				{
					GameObject go = Cpp.Read(() => ((Component)canvas).gameObject);
					if ((UnityEngine.Object)(object)go == (UnityEngine.Object)null || !go.activeInHierarchy)
						continue;

					string canvasName = go.name ?? string.Empty;

					// Scan canvases that look like settings/options panels, or any canvas
					// whose children include settings-like controls (sliders, toggles for
					// audio/graphics/gameplay/censorship).
					bool isSettingsCanvas = canvasName.IndexOf("setting", StringComparison.OrdinalIgnoreCase) >= 0
						|| canvasName.IndexOf("option", StringComparison.OrdinalIgnoreCase) >= 0;
					if (!ScannedCanvases.Add(canvasName) && !isSettingsCanvas)
					{
						// Even if we've seen this canvas before, re-scan if it appears to be a
						// settings panel that was missed on a previous scan.
						if (!isSettingsCanvas)
							continue;
					}

					// Build a set of all Selectable children.
					List<Selectable> selectables = new List<Selectable>();
					Il2CppArrayBase<Selectable> all = go.GetComponentsInChildren<Selectable>(true);
					if (all != null)
					{
						foreach (Selectable s in all)
						{
							if ((UnityEngine.Object)(object)s != (UnityEngine.Object)null
								&& ((UIBehaviour)s).IsActive())
							{
								selectables.Add(s);
							}
						}
					}

					if (selectables.Count == 0)
						continue;

					if (Log.Verbose)
					{
						Log.Info("Settings scan: canvas '" + canvasName + "' has " + selectables.Count + " selectables.");

						// List the names of all selectables for debugging.
						foreach (Selectable sel in selectables)
						{
							string n = ((UnityEngine.Object)((Component)sel).gameObject).name ?? string.Empty;
							if (!string.IsNullOrEmpty(n))
								Log.Debug("  settings widget: " + n);
						}
					}

					// For each selectable with no readable text, log it.
					foreach (Selectable sel in selectables)
					{
						GameObject widget = ((Component)sel).gameObject;
						string rawName = widget.name ?? string.Empty;
						if (string.IsNullOrWhiteSpace(rawName))
							continue;

						// Skip if it already has text.
						string widgetText = ActiveTextIn(widget);
						if (!string.IsNullOrWhiteSpace(widgetText))
							continue;

						// Skip if a label override already exists.
						if (!string.IsNullOrWhiteSpace(Overrides.For(rawName)))
							continue;							// Log it.
							LogUnlabeled(rawName);
					}
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Settings scan failed: " + ex.Message);
		}
	}

	private static void LogUnlabeled(string rawName)
	{
		if (string.IsNullOrWhiteSpace(rawName))
			return;
		try
		{
			string directory = Diagnostics.Directory;
			Directory.CreateDirectory(directory);
			string path = Path.Combine(directory, "unlabeled.txt");
			if (!File.Exists(path))
			{
				File.WriteAllText(path, "# Controls House Access found with no readable text of their own." + Environment.NewLine + "# The name on the left is the game's own; edit the part after the = and" + Environment.NewLine + "# copy the line into labels.txt to change what is spoken." + Environment.NewLine);
			}
			File.AppendAllText(path, rawName + " = " + TextUtil.Humanize(rawName) + Environment.NewLine);
			Log.Debug("Unlabelled control recorded: " + rawName);
		}
		catch (Exception ex)
		{
			Log.Warn("Could not record the unlabelled control '" + rawName + "': " + ex.Message);
		}
	}

	private static bool IsSettingsCanvas()
	{
		try
		{
			foreach (CanvasBase c in Cpp.FindAll<CanvasBase>(activeOnly: true))
			{
				if (!Cpp.Alive((UnityEngine.Object)(object)c))
					continue;
				try
				{
					if (Cpp.Read(() => ((Component)c).gameObject.activeSelf))
					{
						string name = ((UnityEngine.Object)((Component)c).gameObject).name ?? string.Empty;
						if (name.IndexOf("setting", StringComparison.OrdinalIgnoreCase) >= 0
							|| name.IndexOf("option", StringComparison.OrdinalIgnoreCase) >= 0)
							return true;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static string ActiveTextIn(GameObject go)
	{
		if ((UnityEngine.Object)(object)go == (UnityEngine.Object)null)
			return null;
		try
		{
			Il2CppArrayBase<Text> texts = go.GetComponentsInChildren<Text>(false);
			if (texts != null)
			{
				foreach (Text t in texts)
				{
					if ((UnityEngine.Object)(object)t == (UnityEngine.Object)null)
						continue;
					string text = Cpp.Read(() => t.text);
					if (!string.IsNullOrWhiteSpace(text))
						return text;
				}
			}
		}
		catch
		{
		}
		return null;
	}
}
