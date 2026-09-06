using System;
using System.Collections.Generic;
using System.Text;
using HouseAccess.Game;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using HouseAccess.World;
using EekCharacterEngine.Canvas;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HouseAccess.UI;

public static class MenuReader
{
	/// <summary>
	/// How long a scene is given to build its UI before highlighted controls are spoken.
	/// Unity fires OnSelect while widgets are still being assembled; anything highlighted
	/// inside this window is held by <see cref="HoldControl"/> and spoken once it closes,
	/// so the item a menu opens on is delayed, never lost.
	/// </summary>
	public const float SettleSeconds = 1f;

	/// <summary>
	/// Only suppresses the same label twice in a row within this window, which is there
	/// to stop the OnSelect patch and the EventSystem watcher announcing one control
	/// twice in the same frame. Anything longer would make returning to an item silent.
	/// Repeats beyond this window are left to Speaker's own DedupeMs setting.
	/// </summary>
	private const float RepeatWindow = 0.5f;

	/// <summary>Quiet time a changing value must hold before it is spoken.</summary>
	private const float ValueQuiet = 0.15f;

	private static int _lastSelectedId;

	private static string _lastSpoken;

	private static float _lastSpokenAt;

	private static float _nextCheck;

	private static int _lastControlId;

	private static GameObject _lastGo;

	private static GameObject _heldGo;

	private static int _valueId;

	private static string _lastValue;

	private static string _pendingValue;

	private static float _pendingValueAt;

	/// <summary>
	/// Raw names already written to unlabeled.txt. Deliberately not cleared by
	/// <see cref="Reset" />: it tracks what the file already holds, not scene state.
	/// </summary>
	private static readonly HashSet<string> Unlabeled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public static bool Active { get; private set; }

	public static void Reset()
	{
		_lastSelectedId = 0;
		_lastSpoken = null;
		_lastSpokenAt = 0f;
		Active = false;
		_nextCheck = 0f;
		_lastControlId = 0;
		_lastGo = null;
		_heldGo = null;
		_valueId = 0;
		_lastValue = null;
		_pendingValue = null;
		_pendingValueAt = 0f;
	}

	public static void Tick()
	{
		if (Time.unscaledTime >= _nextCheck)
		{
			_nextCheck = Time.unscaledTime + 0.3f;
			Active = MenuIsShowing();
		}
		FlushHeldControl();
		WatchSelection();
		TrackValue();
		SpeakPendingValue();
		WatchKeys();
	}

	/// <summary>
	/// The keys the mod answers inside the game's own menus: KeyRepeatTarget says the focused
	/// control again, and a number key reads or picks a control by its position on screen.
	/// HouseAccessMod leaves KeyRepeatTarget unclaimed while a menu is showing (its Radar
	/// handler is gated on !MenuReader.Active), so no binding is taken over. The bridges below
	/// own their own lists and answer both keys themselves, so they are left alone while up.
	/// The repeat key is checked before the numbers, exactly as in the mod's own lists, so
	/// binding it to a digit keeps working.
	/// </summary>
	private static void WatchKeys()
	{
		if (!Prefs.SpeakUi.Value || !Active || BridgeOwnsTheList() || KeyEditor.Active || InputConfigBridge.Active || Finder.Active || DialogueBridge.Active || DialogueBridge.RepliesHeld)
		{
			return;
		}
		if (Keys.Hit(Prefs.KeyRepeatTarget))
		{
			RepeatFocused();
			return;
		}
		NumberKeys();
	}

	/// <summary>
	/// Re-reads the focused control, bypassing every duplicate filter. An explicit key
	/// press that produces silence is indistinguishable from a broken mod, so this always
	/// speaks - either the control or the reason there is nothing to say.
	/// </summary>
	public static void RepeatFocused()
	{
		GameObject val = FocusedObject();
		string text = (((UnityEngine.Object)(object)val == (UnityEngine.Object)null) ? null : LabelFor(val));
		if (string.IsNullOrWhiteSpace(text))
		{
			Speaker.SayNow("Nothing focused.");
			return;
		}
		_lastSpoken = text;
		_lastSpokenAt = Time.unscaledTime;
		Speaker.SayNow(text);
	}

	/// <summary>
	/// A number reads the control at that position on screen, control and a number moves the
	/// game's own focus there and presses it. The list is gathered only once a number key is
	/// actually down: sweeping every control on screen costs far more than this feature is
	/// worth to spend on every frame.
	/// </summary>
	private static void NumberKeys()
	{
		if (!ListNumbers.Wanted(out int number, out bool pick))
		{
			return;
		}
		List<Numbered> list = NumberedControls();
		if (!ListNumbers.Resolve(number, list.Count, out int index))
		{
			return;
		}
		Numbered numbered = list[index];
		string text = LabelFor(numbered.Go, note: false) ?? "unnamed";
		// Marked as spoken before the focus moves, so the highlight the game raises a frame
		// later is not read out a second time.
		MarkSpoken(numbered, text);
		MoveFocus(numbered);
		if (!pick)
		{
			Speaker.SayNow($"{text}, {index + 1} of {list.Count}.");
			return;
		}
		bool flag = Press(numbered);
		string text2 = (Cpp.Alive((UnityEngine.Object)(object)numbered.Go) ? LabelFor(numbered.Go, note: false) : null);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = text;
		}
		// Marked again with the label the control has now that it has been pressed. Both the
		// value watcher and the game's own highlight compare against what was last spoken, so
		// the result of the press is announced once, here, rather than by three places at once.
		MarkSpoken(numbered, text2);
		// A slider or a text field implements no submit, so say what did happen rather than
		// claim something was pressed.
		Speaker.SayNow(flag ? (text2 + ".") : (text2 + ", focused."));
	}

	/// <summary>
	/// Records a control as just spoken, so the game's own highlight arriving a frame later is
	/// not read out again. The value is recorded from the object the focus was actually pointed
	/// at, because that is the object <see cref="FocusedObject" /> will report and the value
	/// watcher only speaks a change on the control it is already watching.
	/// </summary>
	private static void MarkSpoken(Numbered row, string label)
	{
		_lastSpoken = label;
		_lastSpokenAt = Time.unscaledTime;
		if (Cpp.Alive((UnityEngine.Object)(object)row.Go))
		{
			_lastControlId = ((UnityEngine.Object)row.Go).GetInstanceID();
			_lastGo = row.Go;
			GameObject val = (Cpp.Alive((UnityEngine.Object)(object)row.Focus) ? row.Focus : row.Go);
			_lastSelectedId = ((UnityEngine.Object)val).GetInstanceID();
			_valueId = _lastSelectedId;
			_lastValue = ValueOf(val);
			_pendingValue = null;
		}
	}

	/// <summary>
	/// Points the game's own focus at the control, which is what makes the game's keys work on
	/// it: arrows carry on from there and its submit key presses it, so a number leaves the
	/// menu in the same state as arrowing onto the item by hand. Selectable.Select is the call
	/// the mod already uses for this in the inventory and use-with lists; the EventSystem is
	/// set directly only for a widget that has no Unity control to select, so that arrow
	/// navigation is never left stranded.
	/// </summary>
	private static void MoveFocus(Numbered row)
	{
		try
		{
			if (Cpp.Alive((UnityEngine.Object)(object)row.Control))
			{
				row.Control.Select();
				return;
			}
			EventSystem current = EventSystem.current;
			if (Cpp.Alive((UnityEngine.Object)(object)current) && Cpp.Alive((UnityEngine.Object)(object)row.Go))
			{
				current.SetSelectedGameObject(row.Go);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Menu focus could not be moved: " + ex.Message);
		}
	}

	/// <summary>
	/// Presses the control the way the game's own submit key does. Unity delivers OnSubmit to
	/// the focused control, and this calls that same method on that same control:
	/// Button.OnSubmit presses the button, Toggle.OnSubmit flips it, and Dropdown.OnSubmit or
	/// TMP_Dropdown.OnSubmit opens the option list. Slider declares no OnSubmit at all in
	/// BepInEx\interop\UnityEngine.UI.dll, and a text field's submit only ends editing, so both
	/// are focused and left alone rather than nudged by a value this mod invented.
	///
	/// ExecuteEvents.Execute is the obvious "click anything" call and is deliberately not used:
	/// it is a generic il2cpp method taking a managed delegate, which is what re-arms the boot
	/// crash this build is patched around.
	///
	/// The game's own EekUI widgets are not submitted to either. EekUIButton and EekUIDropdown
	/// do declare OnSubmit, but whether those perform the action or only add the click sound
	/// cannot be read from the interop assembly, and pressing both a widget and the Unity
	/// control it drives could act twice. The Unity control is the one whose behaviour is
	/// documented, so that is the one pressed.
	/// </summary>
	private static bool Press(Numbered row)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)row.Control))
		{
			return false;
		}
		try
		{
			if (!Cpp.Read(() => row.Control.IsInteractable(), fallback: false))
			{
				return false;
			}
			EventSystem current = EventSystem.current;
			if (!Cpp.Alive((UnityEngine.Object)(object)current))
			{
				return false;
			}
			BaseEventData val = new BaseEventData(current);
			Button val2 = ((Il2CppObjectBase)row.Control).TryCast<Button>();
			if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
			{
				val2.OnSubmit(val);
				return true;
			}
			Toggle val3 = ((Il2CppObjectBase)row.Control).TryCast<Toggle>();
			if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null)
			{
				val3.OnSubmit(val);
				return true;
			}
			Dropdown val4 = ((Il2CppObjectBase)row.Control).TryCast<Dropdown>();
			if ((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null)
			{
				val4.OnSubmit(val);
				return true;
			}
			TMP_Dropdown val5 = ((Il2CppObjectBase)row.Control).TryCast<TMP_Dropdown>();
			if ((UnityEngine.Object)(object)val5 != (UnityEngine.Object)null)
			{
				val5.OnSubmit(val);
				return true;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Control could not be pressed: " + ex.Message);
		}
		return false;
	}

	/// <summary>
	/// Every control on screen a number can reach, in the order ReadAll already reads the
	/// screen: top to bottom, then left to right. The game's own EekUI widgets are gathered
	/// first because they are the objects the mod labels and announces everywhere else, and the
	/// Unity control each one drives is recorded as that widget's press target rather than
	/// added as a second entry - so one widget is never two numbers, and the announcement a
	/// number suppresses is the one the widget's own highlight would have made. Plain
	/// Selectables no widget covers are added after, which is what picks up the option list of
	/// an open dropdown.
	///
	/// Nothing here is cached: a menu adds and removes controls while it is open, and a stale
	/// list would hand out numbers for controls that are no longer on screen.
	/// </summary>
	private static List<Numbered> NumberedControls()
	{
		List<Numbered> list = new List<Numbered>();
		HashSet<int> hashSet = new HashSet<int>();
		try
		{
			foreach (EekUIButton w in Cpp.FindAll<EekUIButton>(activeOnly: true))
			{
				Add(list, hashSet, Cpp.Read(() => ((Component)w).gameObject), Cpp.Read(() => w.JMLGMPJOJNN));
			}
			foreach (EekUIToggle w2 in Cpp.FindAll<EekUIToggle>(activeOnly: true))
			{
				Add(list, hashSet, Cpp.Read(() => ((Component)w2).gameObject), Cpp.Read(() => w2.FHIDCFFFIJM));
			}
			foreach (EekUISlider w3 in Cpp.FindAll<EekUISlider>(activeOnly: true))
			{
				Add(list, hashSet, Cpp.Read(() => ((Component)w3).gameObject), Cpp.Read(() => w3.OGDONAFLGNI));
			}
			foreach (EekUIDropdown w4 in Cpp.FindAll<EekUIDropdown>(activeOnly: true))
			{
				Add(list, hashSet, Cpp.Read(() => ((Component)w4).gameObject), Cpp.Read(() => w4.LIKJKKDDHLD));
			}
			foreach (Selectable s in Cpp.FindAll<Selectable>(activeOnly: true))
			{
				Add(list, hashSet, Cpp.Read(() => ((Component)s).gameObject), s);
			}
		}
		catch (Exception ex)
		{
			Log.Debug("Menu controls could not be listed: " + ex.Message);
		}
		// Sorted after the sweeps rather than during them, so a widget and a plain Selectable
		// end up in one screen order instead of two blocks.
		list.Sort(delegate(Numbered a, Numbered b)
		{
			int num = b.Y.CompareTo(a.Y);
			return (num != 0) ? num : a.X.CompareTo(b.X);
		});
		return list;
	}

	/// <summary>
	/// Records one control, keeping the list to things a sighted player could click.
	/// <paramref name="control" /> is the Unity control the caller already knows about; when a
	/// widget names none, the widget's own object is asked for one, because an EekUI widget and
	/// the control it drives sometimes share a GameObject and sometimes do not. Both objects are
	/// marked as seen, so the Selectable sweep afterwards cannot number the same control a
	/// second time under a different name.
	/// </summary>
	private static void Add(List<Numbered> into, HashSet<int> seen, GameObject go, Selectable control)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)go))
		{
			return;
		}
		if (!Cpp.Alive((UnityEngine.Object)(object)control))
		{
			control = go.GetComponent<Selectable>();
		}
		bool flag = Cpp.Alive((UnityEngine.Object)(object)control);
		// Interactable is the game's own answer to "can this be clicked", so a greyed out
		// control is not one to hand a number to. A widget with no Unity control at all is
		// still listed: it can be read and focused, which is more than silence.
		if (flag && !Cpp.Read(() => control.IsInteractable(), fallback: false))
		{
			return;
		}
		GameObject val = (flag ? Cpp.Read(() => ((Component)control).gameObject) : null);
		if (!seen.Add(((UnityEngine.Object)go).GetInstanceID()))
		{
			return;
		}
		bool flag2 = Cpp.Alive((UnityEngine.Object)(object)val);
		if (flag2)
		{
			seen.Add(((UnityEngine.Object)val).GetInstanceID());
		}
		into.Add(new Numbered
		{
			Go = go,
			Focus = (flag2 ? val : go),
			Control = control,
			Y = Cpp.Read(() => go.transform.position.y, 0f),
			X = Cpp.Read(() => go.transform.position.x, 0f)
		});
	}

	/// <summary>
	/// One control a number can reach: the object its name is read from, the object the focus is
	/// pointed at, the Unity control that gets pressed, and where it sits on screen. References
	/// only - the label and the value are read live when the number is pressed, so a list built
	/// one frame and used the next can never speak a stale value.
	/// </summary>
	private sealed class Numbered
	{
		public GameObject Go;

		public GameObject Focus;

		public Selectable Control;

		public float Y;

		public float X;
	}

	private static bool MenuIsShowing()
	{
		try
		{
			foreach (CanvasBase c in Cpp.FindAll<CanvasBase>(activeOnly: true))
			{
				if (!Cpp.Alive((UnityEngine.Object)(object)c) || !Cpp.Read(() => c.Modal, fallback: false) || IsBackgroundCanvas(c))
				{
					continue;
				}
				try
				{
					if (((Component)c).gameObject.activeSelf)
					{
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
		try
		{
			return Cursor.visible;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsBackgroundCanvas(CanvasBase c)
	{
		try
		{
			GameObject val = Cpp.Read(() => ((Component)c).gameObject);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				return true;
			}
			string text = ((UnityEngine.Object)val).name ?? string.Empty;
			// Skip debug, console, and HUD canvases, but never skip settings panels.
			return text.IndexOf("debug", StringComparison.OrdinalIgnoreCase) >= 0
				|| text.IndexOf("console", StringComparison.OrdinalIgnoreCase) >= 0
				|| text.IndexOf("hud", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private static void WatchSelection()
	{
		if (!Prefs.SpeakUi.Value)
		{
			return;
		}
		try
		{
			EventSystem es = EventSystem.current;
			if (!Cpp.Alive((UnityEngine.Object)(object)es))
			{
				return;
			}
			GameObject val = Cpp.Read(() => es.currentSelectedGameObject);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				_lastSelectedId = 0;
				return;
			}
			int instanceID = ((UnityEngine.Object)val).GetInstanceID();
			if (instanceID != _lastSelectedId)
			{
				_lastSelectedId = instanceID;
				if (!DialogueBridge.RepliesHeld && !DialogueBridge.SpeakerTalking && !BridgeOwnsTheList())
				{
					AnnounceControl(val);
				}
			}
		}
		catch
		{
		}
	}

	public static void Announce(string label)
	{
		Announce(label, force: false);
	}

	/// <summary>
	/// Speaks a control label. <paramref name="force" /> marks an announcement the user
	/// asked for, which skips both duplicate filters - this one and Speaker's - so a key
	/// press can never be answered with silence.
	/// </summary>
	public static void Announce(string label, bool force)
	{
		if (string.IsNullOrWhiteSpace(label))
		{
			return;
		}
		if (!force && string.Equals(label, _lastSpoken, StringComparison.OrdinalIgnoreCase) && Time.unscaledTime - _lastSpokenAt < RepeatWindow)
		{
			return;
		}
		_lastSpoken = label;
		_lastSpokenAt = Time.unscaledTime;
		Speaker.Say(label, force ? Pri.Critical : Pri.High);
	}

	private static bool BridgeOwnsTheList()
	{
		return InventoryBridge.Active || UseWithBridge.Active || WheelBridge.Active || RadialBridge.Active || OpportunityBridge.Active || CustomizeBridge.Active || ActionPicker.Active || Hands.Active;
	}

	public static void AnnounceControl(GameObject go)
	{
		AnnounceControl(go, force: false);
	}

	public static void AnnounceControl(GameObject go, bool force)
	{
		if ((UnityEngine.Object)(object)go == (UnityEngine.Object)null)
		{
			return;
		}
		string text = LabelFor(go);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		int instanceID = ((UnityEngine.Object)go).GetInstanceID();
		if (instanceID != _lastControlId && string.Equals(text, _lastSpoken, StringComparison.OrdinalIgnoreCase))
		{
			string text2 = TextUtil.Humanize(((UnityEngine.Object)go).name);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = text + ", " + text2;
			}
		}
		_lastControlId = instanceID;
		_lastGo = go;
		// The label just spoken already carried this control's value, so the watcher
		// starts from that value and only speaks the next change.
		_valueId = instanceID;
		_lastValue = ValueOf(go);
		_pendingValue = null;
		Announce(text, force);
	}

	public static string LabelFor(GameObject go)
	{
		return LabelFor(go, note: true);
	}

	/// <summary>
	/// The spoken name of a control. <paramref name="note" /> is false when the caller is
	/// looking at every control on screen rather than one the user landed on, so numbering a
	/// menu does not fill unlabeled.txt with widgets nobody was told the name of - that file
	/// exists to list the controls a user actually heard a raw object name for.
	/// </summary>
	public static string LabelFor(GameObject go, bool note)
	{
		if ((UnityEngine.Object)(object)go == (UnityEngine.Object)null)
		{
			return null;
		}
		// The game's ClickManager control identifiers do not exist on this build, so the
		// name is taken from the widget's own visible text, then labels.txt, then text the
		// widget only shows in another state, then the row it sits in, and only as a last
		// resort from the raw object name - which is what "odd names" sounds like.
		string text = ActiveTextIn(go);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = Overrides.For(((UnityEngine.Object)go).name);
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = TextIn(go);
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = RowText(go);
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			if (note)
			{
				NoteUnlabeled(((UnityEngine.Object)go).name);
			}
			text = TextUtil.Humanize(((UnityEngine.Object)go).name);
		}
		text = TextUtil.Clean(text);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (TryValue(go, out string role, out string value))
		{
			if (string.Equals(role, "dropdown", StringComparison.Ordinal))
			{
				return text + ", " + value + ", dropdown";
			}
			if (string.Equals(role, "text field", StringComparison.Ordinal))
			{
				return text + ", text field";
			}
			return text + ", " + role + ", " + value;
		}
		return text;
	}

	/// <summary>
	/// Reads the widget kind and its current value off the game's own component. Shared by
	/// the focus announcement and the value watcher so the two can never disagree, and the
	/// value is always read live - never cached - so nothing stale is ever spoken.
	/// </summary>
	private static bool TryValue(GameObject go, out string role, out string value)
	{
		role = null;
		value = null;
		if ((UnityEngine.Object)(object)go == (UnityEngine.Object)null)
		{
			return false;
		}
		try
		{
			Selectable component = go.GetComponent<Selectable>();
			if (Cpp.Alive((UnityEngine.Object)(object)component))
			{
				Toggle toggle = ((Il2CppObjectBase)component).TryCast<Toggle>();
				if ((UnityEngine.Object)(object)toggle != (UnityEngine.Object)null)
				{
					role = "checkbox";
					value = (Cpp.Read(() => toggle.isOn, fallback: false) ? "checked" : "unchecked");
					return true;
				}
				Slider slider = ((Il2CppObjectBase)component).TryCast<Slider>();
				if ((UnityEngine.Object)(object)slider != (UnityEngine.Object)null)
				{
					role = "slider";
					value = $"{Cpp.Read(() => slider.value, 0f):0.##}";
					return true;
				}
				Dropdown val = ((Il2CppObjectBase)component).TryCast<Dropdown>();
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					role = "dropdown";
					value = SelectedOption(val);
					return true;
				}
				TMP_Dropdown val2 = ((Il2CppObjectBase)component).TryCast<TMP_Dropdown>();
				if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
				{
					role = "dropdown";
					value = SelectedOptionTmp(val2);
					return true;
				}
				if ((UnityEngine.Object)(object)((Il2CppObjectBase)component).TryCast<InputField>() != (UnityEngine.Object)null || (UnityEngine.Object)(object)((Il2CppObjectBase)component).TryCast<TMP_InputField>() != (UnityEngine.Object)null)
				{
					role = "text field";
					return true;
				}
			}
			return TryEekValue(go, out role, out value);
		}
		catch
		{
		}
		return false;
	}

	/// <summary>
	/// Second attempt for the game's own widgets. Verified against
	/// BepInEx\interop\Assembly-CSharp.dll: EekUIButton, EekUIToggle, EekUISlider and
	/// EekUIDropdown all extend MonoBehaviour rather than Selectable, and each one holds a
	/// reference to the Unity control it drives - EekUISlider.OGDONAFLGNI is a
	/// UnityEngine.UI.Slider, EekUIToggle.FHIDCFFFIJM a Toggle, EekUIDropdown.LIKJKKDDHLD a
	/// Dropdown. Reading the value through those references costs nothing when the control
	/// happens to sit on the same GameObject, and is the only thing that works when it does
	/// not, so no assumption about the menu hierarchy is needed either way. The obfuscated
	/// property names are build-specific: if a game update renames them this stops compiling,
	/// which is the failure mode to want - loud, not silent.
	/// </summary>
	private static bool TryEekValue(GameObject go, out string role, out string value)
	{
		role = null;
		value = null;
		EekUISlider eekSlider = go.GetComponent<EekUISlider>();
		if (Cpp.Alive((UnityEngine.Object)(object)eekSlider))
		{
			Slider slider = Cpp.Read(() => eekSlider.OGDONAFLGNI);
			if (Cpp.Alive((UnityEngine.Object)(object)slider))
			{
				role = "slider";
				value = $"{Cpp.Read(() => slider.value, 0f):0.##}";
				return true;
			}
		}
		EekUIToggle eekToggle = go.GetComponent<EekUIToggle>();
		if (Cpp.Alive((UnityEngine.Object)(object)eekToggle))
		{
			Toggle toggle = Cpp.Read(() => eekToggle.FHIDCFFFIJM);
			if (Cpp.Alive((UnityEngine.Object)(object)toggle))
			{
				role = "checkbox";
				value = (Cpp.Read(() => toggle.isOn, fallback: false) ? "checked" : "unchecked");
				return true;
			}
		}
		EekUIDropdown eekDropdown = go.GetComponent<EekUIDropdown>();
		if (Cpp.Alive((UnityEngine.Object)(object)eekDropdown))
		{
			Dropdown dropdown = Cpp.Read(() => eekDropdown.LIKJKKDDHLD);
			if (Cpp.Alive((UnityEngine.Object)(object)dropdown))
			{
				role = "dropdown";
				value = SelectedOption(dropdown);
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// The spoken form of a control's current value, or null for controls that have none.
	/// Text fields are excluded on purpose: typing is announced by TextFields, and echoing
	/// every keystroke here would talk over it.
	/// </summary>
	private static string ValueOf(GameObject go)
	{
		if (!TryValue(go, out string role, out string value) || string.Equals(role, "text field", StringComparison.Ordinal))
		{
			return null;
		}
		return value;
	}

	private static string SelectedOption(Dropdown dd)
	{
		try
		{
			int num = Cpp.Read(() => dd.value, -1);
			Il2CppSystem.Collections.Generic.List<Dropdown.OptionData> list = Cpp.Read(() => dd.options);
			if (num >= 0 && num < Cpp.CountOf<Dropdown.OptionData>(list))
			{
				Dropdown.OptionData o = Cpp.AtOf<Dropdown.OptionData>(list, num);
				string text = ((o == null) ? null : Cpp.Read(() => o.text));
				if (!string.IsNullOrWhiteSpace(text))
				{
					return TextUtil.Clean(text);
				}
			}
		}
		catch
		{
		}
		return "no selection";
	}

	private static string SelectedOptionTmp(TMP_Dropdown dd)
	{
		try
		{
			int num = Cpp.Read(() => dd.value, -1);
			Il2CppSystem.Collections.Generic.List<TMP_Dropdown.OptionData> list = Cpp.Read(() => dd.options);
			if (num >= 0 && num < Cpp.CountOf<TMP_Dropdown.OptionData>(list))
			{
				TMP_Dropdown.OptionData o = Cpp.AtOf<TMP_Dropdown.OptionData>(list, num);
				string text = ((o == null) ? null : Cpp.Read(() => o.text));
				if (!string.IsNullOrWhiteSpace(text))
				{
					return TextUtil.Clean(text);
				}
			}
		}
		catch
		{
		}
		return "no selection";
	}

	/// <summary>
	/// The control the user is on: Unity's selection when there is one, otherwise the last
	/// control announced - the game's EekUI widgets raise OnSelect without always leaving
	/// an EventSystem selection behind. The fallback is trusted only while a menu is up, so
	/// a stale object is never re-read during play.
	/// </summary>
	private static GameObject FocusedObject()
	{
		try
		{
			EventSystem es = EventSystem.current;
			if (Cpp.Alive((UnityEngine.Object)(object)es))
			{
				GameObject val = Cpp.Read(() => es.currentSelectedGameObject);
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					return val;
				}
			}
		}
		catch
		{
		}
		if (Active && Cpp.Alive((UnityEngine.Object)(object)_lastGo))
		{
			return _lastGo;
		}
		return null;
	}

	/// <summary>
	/// Watches the focused control's value and speaks it when it changes while the focus
	/// stays put - a slider dragged, a checkbox ticked, a dropdown switched. The game drives
	/// those through its own input handling and this build exposes no settings-change method
	/// to patch (see Patches.Apply), so the value is read live each frame and compared.
	/// </summary>
	private static void TrackValue()
	{
		if (!Prefs.SpeakUi.Value)
		{
			return;
		}
		GameObject val = FocusedObject();
		if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
		{
			_valueId = 0;
			_lastValue = null;
			_pendingValue = null;
			return;
		}
		int instanceID = ((UnityEngine.Object)val).GetInstanceID();
		string text = ValueOf(val);
		if (instanceID != _valueId)
		{
			// A different control: its own label announcement carries the value.
			_valueId = instanceID;
			_lastValue = text;
			_pendingValue = null;
		}
		else if (text != null && !string.Equals(text, _lastValue, StringComparison.Ordinal))
		{
			_lastValue = text;
			_pendingValue = text;
			// A held arrow key or a mouse drag moves a slider every frame; pushing the
			// deadline forward on each change means only the value it settles on is spoken.
			_pendingValueAt = Time.unscaledTime + ValueQuiet;
		}
	}

	private static void SpeakPendingValue()
	{
		if (_pendingValue != null && Time.unscaledTime >= _pendingValueAt)
		{
			string text = _pendingValue;
			_pendingValue = null;
			// Critical: stepping a value back to one spoken moments ago must still be heard,
			// so this must not fall into Speaker's duplicate window.
			Speaker.SayNow(text);
		}
	}

	/// <summary>
	/// Keeps a control that was highlighted while its scene was still building, so it can
	/// be spoken once <see cref="SettleSeconds" /> has passed. Dropping it instead is what
	/// made a freshly opened menu silent about the item it opens on.
	/// </summary>
	public static void HoldControl(GameObject go)
	{
		if (Cpp.Alive((UnityEngine.Object)(object)go))
		{
			_heldGo = go;
		}
	}

	private static void FlushHeldControl()
	{
		if ((UnityEngine.Object)(object)_heldGo == (UnityEngine.Object)null || GameRefs.SceneAge < SettleSeconds)
		{
			return;
		}
		GameObject val = _heldGo;
		_heldGo = null;
		if (!Cpp.Alive((UnityEngine.Object)(object)val) || !Prefs.SpeakUi.Value || BridgeOwnsTheList() || DialogueBridge.RepliesHeld || DialogueBridge.SpeakerTalking)
		{
			return;
		}
		if (((UnityEngine.Object)val).GetInstanceID() != _lastControlId)
		{
			AnnounceControl(val);
		}
	}

	private static string ActiveTextIn(GameObject go)
	{
		return Shallowest(go, includeInactive: false);
	}

	private static string TextIn(GameObject go)
	{
		string text = Shallowest(go, includeInactive: false);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return Shallowest(go, includeInactive: true);
	}

	private static string Shallowest(GameObject go, bool includeInactive)
	{
		string result = null;
		int num = int.MaxValue;
		try
		{
			Il2CppArrayBase<Text> componentsInChildren = go.GetComponentsInChildren<Text>(includeInactive);
			if (componentsInChildren != null)
			{
				foreach (Text t in componentsInChildren)
				{
					if ((UnityEngine.Object)(object)t == (UnityEngine.Object)null)
					{
						continue;
					}
					string text = Cpp.Read(() => t.text);
					if (!string.IsNullOrWhiteSpace(text))
					{
						int num2 = DepthBelow(go.transform, ((Component)t).transform);
						if (num2 >= 0 && num2 < num)
						{
							num = num2;
							result = text;
						}
					}
				}
			}
			Il2CppArrayBase<TMP_Text> componentsInChildren2 = go.GetComponentsInChildren<TMP_Text>(includeInactive);
			if (componentsInChildren2 != null)
			{
				foreach (TMP_Text t2 in componentsInChildren2)
				{
					if ((UnityEngine.Object)(object)t2 == (UnityEngine.Object)null)
					{
						continue;
					}
					string text2 = Cpp.Read(() => t2.text);
					if (!string.IsNullOrWhiteSpace(text2))
					{
						int num3 = DepthBelow(go.transform, t2.transform);
						if (num3 >= 0 && num3 < num)
						{
							num = num3;
							result = text2;
						}
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}

	/// <summary>
	/// Text from the row a control sits in, for widgets whose caption is a sibling rather
	/// than a child. Used only when that row holds this one control, because a row with
	/// several controls could hand back another control's caption, and a confidently wrong
	/// name is worse than a raw one.
	/// </summary>
	private static string RowText(GameObject go)
	{
		try
		{
			Transform val = Cpp.Read(() => go.transform.parent);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				return null;
			}
			GameObject gameObject = ((Component)val).gameObject;
			int num = 0;
			Il2CppArrayBase<Selectable> componentsInChildren = gameObject.GetComponentsInChildren<Selectable>(true);
			if (componentsInChildren != null)
			{
				foreach (Selectable item in componentsInChildren)
				{
					if ((UnityEngine.Object)(object)item != (UnityEngine.Object)null)
					{
						num++;
					}
				}
			}
			if (num != 1)
			{
				return null;
			}
			return TextIn(gameObject);
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Records a control that had no text anywhere, so its raw object name is what gets
	/// spoken. Each name is written once to unlabeled.txt in the same folder as labels.txt,
	/// already in "name = spoken name" form, so it can be pasted across and given a proper
	/// name instead of being guessed at from here.
	/// </summary>
	private static void NoteUnlabeled(string rawName)
	{
		if (string.IsNullOrWhiteSpace(rawName) || !Unlabeled.Add(rawName))
		{
			return;
		}
		try
		{
			string directory = Diagnostics.Directory;
			System.IO.Directory.CreateDirectory(directory);
			string text = System.IO.Path.Combine(directory, "unlabeled.txt");
			if (!System.IO.File.Exists(text))
			{
				System.IO.File.WriteAllText(text, "# Controls House Access found with no readable text of their own." + Environment.NewLine + "# The name on the left is the game's own; edit the part after the = and" + Environment.NewLine + "# copy the line into labels.txt to change what is spoken." + Environment.NewLine);
			}
			System.IO.File.AppendAllText(text, rawName + " = " + TextUtil.Humanize(rawName) + Environment.NewLine);
			Log.Debug("Unlabelled control recorded: " + rawName);
		}
		catch (Exception ex)
		{
			Log.Warn("Could not record the unlabelled control '" + rawName + "': " + ex.Message);
		}
	}

	private static int DepthBelow(Transform root, Transform child)
	{
		try
		{
			int num = 0;
			Transform val = child;
			while ((UnityEngine.Object)(object)val != (UnityEngine.Object)null && num < 32)
			{
				if ((UnityEngine.Object)(object)val == (UnityEngine.Object)(object)root)
				{
					return num;
				}
				val = val.parent;
				num++;
			}
		}
		catch
		{
		}
		return -1;
	}

	public static void ReadAll()
	{
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<KeyValuePair<float, string>> list = new List<KeyValuePair<float, string>>();
		foreach (Text t in Cpp.FindAll<Text>(activeOnly: true))
		{
			Consider(((UnityEngine.Object)(object)t == (UnityEngine.Object)null) ? null : Cpp.Read(() => t.text), ((UnityEngine.Object)(object)t == (UnityEngine.Object)null) ? 0f : Cpp.Read(() => ((Component)t).transform.position.y, 0f), seen, list);
		}
		foreach (TMP_Text t2 in Cpp.FindAll<TMP_Text>(activeOnly: true))
		{
			Consider(((UnityEngine.Object)(object)t2 == (UnityEngine.Object)null) ? null : Cpp.Read(() => t2.text), ((UnityEngine.Object)(object)t2 == (UnityEngine.Object)null) ? 0f : Cpp.Read(() => t2.transform.position.y, 0f), seen, list);
		}
		if (list.Count == 0)
		{
			// Critical, like the read below: this only runs because the user pressed the
			// read-screen key, and an unanswered key press reads as a broken mod.
			Speaker.SayNow("Nothing readable on screen.");
			return;
		}
		list.Sort((KeyValuePair<float, string> a, KeyValuePair<float, string> b) => b.Key.CompareTo(a.Key));
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		foreach (KeyValuePair<float, string> item in list)
		{
			stringBuilder.Append(item.Value);
			stringBuilder.Append(". ");
			if (++num >= 30)
			{
				break;
			}
		}
		Speaker.SayNow(TextUtil.Cap(stringBuilder.ToString(), 1400));
	}

	private static void Consider(string raw, float y, HashSet<string> seen, List<KeyValuePair<float, string>> into)
	{
		string text = TextUtil.Clean(raw);
		if (!string.IsNullOrWhiteSpace(text) && text.Length >= 2 && seen.Add(text))
		{
			into.Add(new KeyValuePair<float, string>(y, text));
		}
	}
}
