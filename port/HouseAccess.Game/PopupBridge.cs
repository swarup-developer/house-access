using System;
using EekCharacterEngine;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.UI;
using HouseAccess.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HouseAccess.Game;

/// <summary>Gives the game's visible popup priority over the UI underneath it.</summary>
public static class PopupBridge
{
	private static PopupManager _popup;
	private static global::InstructionsManager _instructions;
	private static GameObject _returnFocus;
	private static GameObject _shownRoot;
	private static string _spokenText;
	private static bool _displayPending;
	private static int _closedFrame = -1;
	private static int _readFrame = -1;
	private static bool _wasActive;

	/// <summary>The currently visible popup or instruction panel.</summary>
	public static GameObject Root
	{
		get
		{
			if (Cpp.Alive(_popup) && _popup.IsShowing && Cpp.Alive(_popup.Canvas) && _popup.Canvas.activeInHierarchy)
				return _popup.Canvas;
			if (Cpp.Alive(_instructions) && _instructions.IsShowing && Cpp.Alive(_instructions.Canvas) && _instructions.Canvas.activeInHierarchy)
				return _instructions.Canvas;
			return null;
		}
	}

	/// <summary>Whether a popup currently owns reading and menu input.</summary>
	public static bool Active => Cpp.Alive(Root);

	/// <summary>Also reserve its closing frame so one key cannot act on two panels.</summary>
	public static bool BlocksGameplay => Active || _wasActive || _closedFrame == Time.frameCount;

	/// <summary>Opening speech includes the controls, so selection callbacks must not repeat them.</summary>
	public static bool ReadingOpening => _displayPending || _readFrame == Time.frameCount;

	/// <summary>Clear scene references.</summary>
	public static void Reset()
	{
		_popup = null;
		_instructions = null;
		_returnFocus = null;
		_shownRoot = null;
		_spokenText = null;
		_displayPending = false;
		_closedFrame = -1;
		_readFrame = -1;
		_wasActive = false;
	}

	/// <summary>Capture focus before DisplayText selects the native OK button.</summary>
	public static void BeforeDisplay(PopupManager popup)
	{
		if (!Active && Cpp.Alive(EventSystem.current))
			_returnFocus = EventSystem.current.currentSelectedGameObject;
		_popup = popup;
	}

	/// <summary>All translated and plain display overloads finish in DisplayText.</summary>
	public static void AfterDisplay(PopupManager popup)
	{
		_popup = popup;
		_displayPending = true;
	}

	/// <summary>Only controls belonging to the visible popup may be read or selected.</summary>
	public static bool Allows(GameObject go)
	{
		GameObject root = Root;
		return !Cpp.Alive(root) || (Cpp.Alive(go) && go.transform.IsChildOf(root.transform));
	}

	/// <summary>Read visible text once per display, including a panel opened without DisplayText.</summary>
	public static void Tick()
	{
		if (!Cpp.Alive(_popup)) _popup = Cpp.FindOne<PopupManager>(activeOnly: false);
		if (!Cpp.Alive(_instructions)) _instructions = Cpp.FindOne<global::InstructionsManager>(activeOnly: false);
		GameObject root = Root;
		if (!Cpp.Alive(root))
		{
			if (_wasActive)
			{
				_closedFrame = Time.frameCount;
				if (Cpp.Alive(_returnFocus) && _returnFocus.activeInHierarchy && Cpp.Alive(EventSystem.current))
					EventSystem.current.SetSelectedGameObject(_returnFocus);
				Log.Info("Popup closed; returning input to the underlying context.");
			}
			_shownRoot = null;
			_wasActive = false;
			_returnFocus = null;
			_spokenText = null;
			_displayPending = false;
			return;
		}
		_wasActive = true;
		if (root != _shownRoot || _displayPending)
		{
			_shownRoot = root;
			_displayPending = false;
			_spokenText = null;
			MenuReader.Reset();
			Log.Info("Popup owns input: " + root.name);
		}
		string text = MenuReader.TextUnder(root);
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, _spokenText, StringComparison.Ordinal))
		{
			_spokenText = text;
			_readFrame = Time.frameCount;
			if (Prefs.SpeakUi.Value)
			{
				Speaker.Stop();
				Speaker.SayNow(text);
			}
		}
		// DisplayText already focuses OK. Instructions can also be opened by Toggle.
		GameObject focused = Cpp.Alive(EventSystem.current) ? EventSystem.current.currentSelectedGameObject : null;
		if (!Cpp.Alive(focused) || !Allows(focused))
		{
			foreach (Selectable control in root.GetComponentsInChildren<Selectable>(false))
			{
				if (control.IsActive() && control.IsInteractable()) { control.Select(); break; }
			}
		}
	}

	/// <summary>Keep explicit reading and diagnostics available while the popup owns input.</summary>
	public static void HandleKeys()
	{
		if (!Active) return;
		if (Keys.Hit(Prefs.KeyReadScreen)) MenuReader.ReadAll();
		else if (Keys.Hit(Prefs.KeyCancel))
		{
			Speaker.Stop();
			HouseAccess.World.Navigator.Stop(null);
			Speaker.SayNow("Stopped. The popup is still open.");
		}
		else if (Keys.Hit(Prefs.KeyHelp)) Speaker.SayNow("Popup. " + Prefs.KeyReadScreen.Value + " reads it. Numbers read controls; Control and a number activates one. Enter activates the focused control.");
		else if (Keys.Hit(Prefs.KeyDump)) Diagnostics.Dump();
		else if (Keys.Hit(Prefs.KeyHookReport)) Diagnostics.DumpHooks();
	}
}
