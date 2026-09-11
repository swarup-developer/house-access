using System.Collections.Generic;
using HouseAccess.Speech;
using HouseAccess.UI;
using HouseAccess.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HouseAccess.Game;

public static class SettingsBridge
{
	private static global::AudioSettings _audio;
	private static GameObject _returnFocus;
	private static bool _prepared;
	private static readonly List<Selectable> Controls = new List<Selectable>();
	private static readonly List<Navigation> OriginalNavigation = new List<Navigation>();

	public static bool Active => Cpp.Alive(_audio) && _audio.IsShowing
		&& Cpp.Alive(_audio.Canvas) && _audio.Canvas.activeInHierarchy;

	public static void Reset()
	{
		RestoreNavigation();
		_audio = null;
		_returnFocus = null;
	}

	/// <summary>Remember the real menu control that opened the audio page.</summary>
	public static void BeforeAudioToggle(global::AudioSettings audio)
	{
		if (!Cpp.Alive(audio))
		{
			return;
		}
		_audio = audio;
		if (!audio.IsShowing && Cpp.Alive(EventSystem.current))
		{
			_returnFocus = EventSystem.current.currentSelectedGameObject;
		}
	}

	/// <summary>Give keyboard users the focus the game only assigns to controllers.</summary>
	public static void OnAudioToggled(global::AudioSettings audio)
	{
		_audio = audio;
		if (Active)
		{
			PrepareAudio();
		}
		else
		{
			RestoreNavigation();
			if (Cpp.Alive(_returnFocus) && _returnFocus.activeInHierarchy && Cpp.Alive(EventSystem.current))
			{
				EventSystem.current.SetSelectedGameObject(_returnFocus);
			}
			_returnFocus = null;
			Log.Debug("Audio settings closed.");
		}
	}

	public static void Tick()
	{
		if (!Prefs.PatchPreferences.Value)
		{
			return;
		}
		if (!Cpp.Alive(_audio))
		{
			_audio = Cpp.FindOne<global::AudioSettings>(activeOnly: false);
		}
		if (!Active)
		{
			if (_prepared)
			{
				RestoreNavigation();
			}
			return;
		}
		if (!_prepared)
		{
			PrepareAudio();
		}
	}

	/// <summary>Exclude underlying menu controls while audio settings are showing.</summary>
	public static bool AllowsMenuControl(GameObject go)
	{
		return !Active || (Cpp.Alive(go) && go.transform.IsChildOf(_audio.Canvas.transform));
	}

	private static void PrepareAudio()
	{
		if (_prepared || !Active)
		{
			return;
		}
		Controls.Clear();
		foreach (Selectable control in _audio.Canvas.GetComponentsInChildren<Selectable>(false))
		{
			if (Cpp.Alive(control) && control.IsActive() && control.IsInteractable())
			{
				Controls.Add(control);
			}
		}
		if (Controls.Count == 0)
		{
			return;
		}
		Controls.Sort((a, b) =>
		{
			int vertical = b.transform.position.y.CompareTo(a.transform.position.y);
			return vertical != 0 ? vertical : a.transform.position.x.CompareTo(b.transform.position.x);
		});
		for (int i = 0; i < Controls.Count; i++)
		{
			Selectable control = Controls[i];
			OriginalNavigation.Add(control.navigation);
			Navigation navigation = control.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			navigation.selectOnUp = Controls[(i + Controls.Count - 1) % Controls.Count];
			navigation.selectOnDown = Controls[(i + 1) % Controls.Count];
			// Unity Slider.OnMove adjusts its own value when no neighbour exists
			// on that axis. Leave the change and onValueChanged event to the game.
			navigation.selectOnLeft = null;
			navigation.selectOnRight = null;
			control.navigation = navigation;
		}
		_prepared = true;
		// Native AudioSettings.Toggle selects _music only for IsControllerInput().
		// Select the same real control for a keyboard, without moving the mouse.
		Selectable first = _audio._music;
		if (!Cpp.Alive(first) || !first.IsActive() || !first.IsInteractable())
		{
			first = Controls[0];
		}
		first.Select();
		Speaker.SayNow("Audio settings. " + MenuReader.LabelFor(first.gameObject));
		Log.Info($"Audio settings opened with keyboard focus and {Controls.Count} controls.");
	}

	private static void RestoreNavigation()
	{
		for (int i = 0; i < Controls.Count && i < OriginalNavigation.Count; i++)
		{
			if (Cpp.Alive(Controls[i]))
			{
				Controls[i].navigation = OriginalNavigation[i];
			}
		}
		Controls.Clear();
		OriginalNavigation.Clear();
		_prepared = false;
	}
}
