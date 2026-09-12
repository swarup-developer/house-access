using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using MelonLoader.Preferences;
using UnityEngine;

namespace HouseAccess;

public static class Prefs
{
	/// <summary>Use the controller path confirmed to move the player on current Steam builds.</summary>
	public const string DefaultMoveMode = "direct";

	public struct Chord
	{
		public KeyCode Key;

		public bool Ctrl;

		public bool Alt;

		public bool Shift;

		public bool IsNone => (int)Key == 0;
	}

	private static MelonPreferences_Category _cat;

	private static readonly Dictionary<string, KeyCode> KeyCache = new Dictionary<string, KeyCode>();

	public static MelonPreferences_Entry<string> Backend;

	public static MelonPreferences_Entry<int> SapiRate;

	public static MelonPreferences_Entry<int> SapiVolume;

	public static MelonPreferences_Entry<int> CoalesceMs;

	public static MelonPreferences_Entry<int> DedupeMs;

	public static MelonPreferences_Entry<bool> BrailleOutput;

	public static MelonPreferences_Entry<bool> SpeakDialogue;

	public static MelonPreferences_Entry<string> DialogueSpeech;

	public static MelonPreferences_Entry<bool> SpeakUi;

	public static MelonPreferences_Entry<bool> SpeakLoading;

	public static MelonPreferences_Entry<bool> UseGameWheel;

	public static MelonPreferences_Entry<bool> WarnWhenWatched;

	public static MelonPreferences_Entry<bool> SpeakReactions;

	public static MelonPreferences_Entry<string> Reactions;

	public static MelonPreferences_Entry<bool> PatchWidgetFocus;

	public static MelonPreferences_Entry<bool> PatchPreferences;

	public static MelonPreferences_Entry<bool> Verbose;

	public static MelonPreferences_Entry<bool> PreferSapiWhenUnverified;

	public static MelonPreferences_Entry<float> ScanRadius;

	public static MelonPreferences_Entry<float> ScanIntervalSec;

	public static MelonPreferences_Entry<int> MaxTargets;

	public static MelonPreferences_Entry<bool> RoomOnly;

	public static MelonPreferences_Entry<float> WalkSpeed;

	public static MelonPreferences_Entry<float> StopDistance;

	public static MelonPreferences_Entry<float> TurnSpeedDegPerSec;

	public static MelonPreferences_Entry<float> FaceHoldSeconds;

	public static MelonPreferences_Entry<float> TurnStepDegrees;

	public static MelonPreferences_Entry<bool> AnnounceProgress;

	public static MelonPreferences_Entry<bool> AnnounceRoomChanges;

	public static MelonPreferences_Entry<bool> HoldWhileWalking;

	public static MelonPreferences_Entry<bool> AutoOpenDoors;

	public static MelonPreferences_Entry<string> CombatAssist;

	public static MelonPreferences_Entry<bool> CombatAutoFace;

	public static MelonPreferences_Entry<bool> CombatCueAttack;

	public static MelonPreferences_Entry<bool> CombatRangeTone;

	public static MelonPreferences_Entry<string> MoveMode;

	public static MelonPreferences_Entry<float> BeaconVolume;

	public static MelonPreferences_Entry<float> BeaconMinInterval;

	public static MelonPreferences_Entry<float> BeaconMaxInterval;

	public static MelonPreferences_Entry<string> KeyNextTarget;

	public static MelonPreferences_Entry<string> KeyPrevTarget;

	public static MelonPreferences_Entry<string> KeyCycleFilter;

	public static MelonPreferences_Entry<string> KeyCycleFilterBack;

	public static MelonPreferences_Entry<string> KeyRepeatTarget;

	public static MelonPreferences_Entry<string> KeyFaceTarget;

	public static MelonPreferences_Entry<string> KeyAutoWalk;

	public static MelonPreferences_Entry<string> KeyToggleBeacon;

	public static MelonPreferences_Entry<string> KeyCancel;

	public static MelonPreferences_Entry<string> KeyAnnounceLocation;

	public static MelonPreferences_Entry<string> KeyAnnounceStatus;

	public static MelonPreferences_Entry<string> KeyAnnounceOccupants;

	public static MelonPreferences_Entry<string> KeyReadScreen;

	public static MelonPreferences_Entry<string> KeyUiNext;

	public static MelonPreferences_Entry<string> KeyUiPrev;

	public static MelonPreferences_Entry<string> KeyUiIncrease;

	public static MelonPreferences_Entry<string> KeyUiDecrease;

	public static MelonPreferences_Entry<string> KeyUiActivate;

	public static MelonPreferences_Entry<string> KeyInteract;

	public static MelonPreferences_Entry<string> KeyListInteractions;

	public static MelonPreferences_Entry<string> KeyTargetStatus;

	public static MelonPreferences_Entry<string> KeyPunch;

	public static MelonPreferences_Entry<string> KeyBlock;

	public static MelonPreferences_Entry<string> KeyCombatStatus;

	public static MelonPreferences_Entry<string> KeyEndFight;

	public static MelonPreferences_Entry<string> KeyNextFacing;

	public static MelonPreferences_Entry<string> KeyPrevFacing;

	public static MelonPreferences_Entry<string> KeyTurnLeft;

	public static MelonPreferences_Entry<string> KeyTurnRight;

	public static MelonPreferences_Entry<string> KeyRunCommand;

	public static MelonPreferences_Entry<string> KeyReadConsole;

	public static MelonPreferences_Entry<string> KeySetup;

	public static MelonPreferences_Entry<string> KeyFind;

	public static MelonPreferences_Entry<string> KeyTypeText;

	public static MelonPreferences_Entry<string> KeyFeelings;

	public static MelonPreferences_Entry<string> KeyWhoSeesMe;

	public static MelonPreferences_Entry<string> KeyReachOut;

	public static MelonPreferences_Entry<string> KeyReachIn;

	public static MelonPreferences_Entry<string> KeyPhotos;

	public static MelonPreferences_Entry<string> KeyCameraView;

	public static MelonPreferences_Entry<string> KeyShotType;

	public static MelonPreferences_Entry<string> KeyFrameSubject;

	public static MelonPreferences_Entry<string> KeyRaiseLower;

	public static MelonPreferences_Entry<string> KeyHands;

	public static MelonPreferences_Entry<string> KeySelfActions;

	public static MelonPreferences_Entry<string> KeyReadInventory;

	public static MelonPreferences_Entry<string> KeyListExits;

	public static MelonPreferences_Entry<string> KeyHoldTarget;

	public static MelonPreferences_Entry<string> KeyExplore;

	public static MelonPreferences_Entry<string> KeyScanRoom;

	public static MelonPreferences_Entry<string> KeyLookAhead;

	public static MelonPreferences_Entry<string> KeyHelp;

	public static MelonPreferences_Entry<string> KeyCycleSpeech;

	public static MelonPreferences_Entry<string> KeySpeechTest;

	public static MelonPreferences_Entry<string> KeyDump;

	public static MelonPreferences_Entry<string> KeyHookReport;

	public static MelonPreferences_Entry<string> KeyToggleMod;

	private static readonly Dictionary<string, Chord> ChordCache = new Dictionary<string, Chord>();

	public static void Init()
	{
		_cat = MelonPreferences.CreateCategory("HouseAccess", "House Access");
		Backend = E("SpeechBackend", "auto", "auto, universalspeech, nvda, zdsr, sapi, or log");
		SapiRate = E("SapiRate", 2, "SAPI-only speaking rate, -10..10. Ignored by screen readers.");
		SapiVolume = E("SapiVolume", 100, "SAPI-only volume, 0..100.");
		CoalesceMs = E("CoalesceMs", 110, "Buffer window in ms; messages inside it are merged into one utterance.");
		DedupeMs = E("DedupeMs", 1200, "Identical text is suppressed if repeated inside this window.");
		BrailleOutput = E("BrailleOutput", def: true, "Mirror announcements to a braille display when supported.");
		SpeakDialogue = E("SpeakDialogue", def: true, "Announce in-game dialogue and subtitle text.");
		DialogueSpeech = E("DialogueSpeech", "voiced", "When to read spoken lines. voiced reads only lines the actors do not say aloud. always reads everything, never reads none. Page Up repeats the last line either way.");
		SpeakUi = E("SpeakUi", def: true, "Announce menus, buttons and panels.");
		SpeakLoading = E("SpeakLoading", def: true, "Read loading screen hints and loading progress.");
		UseGameWheel = E("UseGameWheel", def: true, "Interact by opening the game own interaction wheel, which knows what is really available. Turn off to use the mod own list instead.");
		WarnWhenWatched = E("WarnWhenWatched", def: true, "Warn before taking or using something while somebody can see it. Being observed changes the outcome of those actions in this game.");
		SpeakReactions = E("SpeakReactions", def: true, "Announce how people react to what you do.");
		Reactions = E("Reactions", "notable", "Which reactions to speak. notable covers consequences: being seen doing something, fights, knockouts. all includes constant ones such as people entering your vicinity, which is very chatty. off silences them.");
		PatchWidgetFocus = E("PatchWidgetFocus", def: true, "Announce menu widget focus, tooltips and tile pickers. Turn off if a game update makes these methods unsafe to hook.");
		PatchPreferences = E("PatchPreferences", def: true, "Announce settings changes such as gender selection.");
		Verbose = E("VerboseLog", def: false, "Very chatty MelonLoader log. Turn on when reporting a bug.");
		PreferSapiWhenUnverified = E("PreferSapiWhenUnverified", def: true, "When no screen reader can be confirmed, use SAPI rather than UniversalSpeech. Set false if you use JAWS or SuperNova, which cannot be probed directly.");
		ScanRadius = E("ScanRadius", 25f, "Metres. How far the target radar reaches.");
		ScanIntervalSec = E("ScanInterval", 0.6f, "Seconds between full radar sweeps. Raise it if the game feels heavy, lower it if targets seem slow to appear.");
		RoomOnly = E("RoomOnly", def: true, "Show only the people and items in the room you are standing in, the way a sighted player sees one room at a time. Rooms and the wider who-is-nearby list are unaffected. Turn off to scan through walls again.");
		MaxTargets = E("MaxTargets", 80, "How many targets the radar keeps. The nearest are kept, so raising this reaches further down the list rather than changing what comes first.");
		WalkSpeed = E("WalkSpeed", 2.2f, "Metres per second for auto-walk.");
		StopDistance = E("StopDistance", 0.9f, "Metres from the target at which auto-walk stops. Stopping short of the game's own reach leaves you unable to interact, which is worse than standing a little too close.");
		TurnStepDegrees = E("TurnStepDegrees", 30f, "Degrees turned per press of the turn keys.");
		FaceHoldSeconds = E("FaceHoldSeconds", 2f, "How long a turn to face something is held against the game's own camera control. Any look input from you ends it at once.");
		TurnSpeedDegPerSec = E("TurnSpeed", 540f, "Degrees per second when turning to face a target.");
		AnnounceProgress = E("AnnounceProgress", def: true, "Call out remaining distance while auto-walking.");
		AnnounceRoomChanges = E("AnnounceRoomChanges", def: true, "Say the room name when you walk into a new one.");
		MoveMode = E("MoveMode", DefaultMoveMode, "How auto-walk moves you: direct, frame or warp. Direct adds a step to the game's native controller movement, respecting its speed and gravity. Frame and warp remain available and fall back if they make no progress.");
		CombatAssist = E("CombatAssist", "off", "How much of a fight to play for you. off is you. assist swings and blocks at the right moments. auto also closes the distance. None of them guarantee a win.");
		CombatAutoFace = E("CombatAutoFace", def: true, "Keep facing your opponent during a fight. A brawl is unwinnable if your swings miss because you drifted.");
		CombatCueAttack = E("CombatCueAttack", def: true, "A short tone the moment a punch would actually land.");
		CombatRangeTone = E("CombatRangeTone", def: true, "Use a tone rather than speech for being in attack reach. Speech is too slow mid-fight.");
		AutoOpenDoors = E("AutoOpenDoors", def: true, "Open a shut door standing in the way while auto-walking. Locked doors are reported, not forced.");
		HoldWhileWalking = E("HoldWhileWalking", def: true, "Automatically ask a person to wait while you walk to them. They resume as soon as you arrive, talk, or cancel.");
		BeaconVolume = E("BeaconVolume", 0.55f, "0..1 volume of the sonar ping.");
		BeaconMinInterval = E("BeaconMinInterval", 0.14f, "Fastest ping spacing, in seconds, when very close.");
		BeaconMaxInterval = E("BeaconMaxInterval", 1.1f, "Slowest ping spacing, in seconds, at maximum range.");
		KeyNextTarget = E("KeyNextTarget", "Ctrl+PageUp", "Next target.");
		KeyPrevTarget = E("KeyPrevTarget", "Ctrl+PageDown", "Previous target.");
		KeyCycleFilterBack = E("KeyCycleFilterBack", "PageDown", "Previous category.");
		KeyCycleFilter = E("KeyCycleFilter", "PageUp", "Cycle category: everything, people, items, doors, rooms, within reach.");
		KeyRepeatTarget = E("KeyRepeatTarget", "Home", "Repeat the current target: name, distance and bearing.");
		KeyFaceTarget = E("KeyFaceTarget", "Ctrl+Home", "Turn to face the target.");
		KeyAutoWalk = E("KeyAutoWalk", "End", "Walk to the target, or stop walking.");
		KeyToggleBeacon = E("KeyToggleBeacon", "Ctrl+b", "Toggle the sonar ping.");
		KeyCancel = E("KeyCancel", "Comma", "Stop everything: silence speech, cancel walking, leave any mode.");
		KeyAnnounceLocation = E("KeyAnnounceLocation", "Ctrl+r", "Say the current room.");
		KeyAnnounceStatus = E("KeyAnnounceStatus", "F3", "Read your own status values.");
		KeyAnnounceOccupants = E("KeyAnnounceOccupants", "Ctrl+p", "List the people nearby.");
		KeyReadScreen = E("KeyReadScreen", "F2", "Read the screen. During a conversation, repeat the last line instead.");
		KeyUiNext = E("KeyUiNext", "DownArrow", "Next item in any list: dialogue replies, options, inventory, the opportunity log.");
		KeyUiPrev = E("KeyUiPrev", "UpArrow", "Previous item in any list.");
		KeyUiIncrease = E("KeyUiIncrease", "RightArrow", "Increase a slider, or rotate the explore cursor right.");
		KeyUiDecrease = E("KeyUiDecrease", "LeftArrow", "Decrease a slider, or rotate the explore cursor left.");
		KeyUiActivate = E("KeyUiActivate", "Return", "Choose the highlighted item, or walk to the explore cursor.");
		KeyInteract = E("KeyInteract", "Equals", "Interact with the target, opening its option list.");
		KeyListInteractions = E("KeyListInteractions", "Ctrl+Equals", "Read what you could do with the target, without doing it.");
		KeyTargetStatus = E("KeyTargetStatus", "F4", "Describe the selected person: room, clothing and how they feel about you.");
		KeyPunch = E("KeyPunch", "Z", "Throw a punch. The mouse still works too.");
		KeyBlock = E("KeyBlock", "X", "Hold to block. The mouse still works too.");
		KeyCombatStatus = E("KeyCombatStatus", "F6", "Read combat state: health, stamina, opponent and reach.");
		KeyEndFight = E("KeyEndFight", "Ctrl+F4", "End the current fight outright by knocking the opposition down.");
		KeyRunCommand = E("KeyRunCommand", "Ctrl+Return", "Run the console command as it stands, without filling every argument.");
		KeyReadConsole = E("KeyReadConsole", "Ctrl+Backspace", "Read the console output again while it is open.");
		KeySetup = E("KeySetup", "Ctrl+k", "Change the mod's own keys from inside the game.");
		KeyFind = E("KeyFind", "Ctrl+z", "Find a target by typing part of its name.");
		KeyTypeText = E("KeyTypeText", "Ctrl+t", "Put the caret in the text box on screen, so typing reaches it.");
		KeyTurnLeft = E("KeyTurnLeft", "J", "Turn left by a fixed step. Hold shift for a quarter turn.");
		KeyTurnRight = E("KeyTurnRight", "L", "Turn right by a fixed step. Hold shift for a quarter turn.");
		KeyNextFacing = E("KeyNextFacing", "Shift+PageUp", "Next target, and turn to face it.");
		KeyPrevFacing = E("KeyPrevFacing", "Shift+PageDown", "Previous target, and turn to face it.");
		KeyWhoSeesMe = E("KeyWhoSeesMe", "Ctrl+v", "Say who can currently see you.");
		KeyFeelings = E("KeyFeelings", "Ctrl+f", "How the selected person feels about you.");
		KeyReachOut = E("KeyReachOut", "Alt+Equals", "Push a held object further away.");
		KeyReachIn = E("KeyReachIn", "Alt+Minus", "Pull a held object closer.");
		KeyPhotos = E("KeyPhotos", "Alt+p", "Read the photographs you have taken and what was in them.");
		KeyCameraView = E("KeyCameraView", "Alt+k", "Switch the camera between the viewfinder and the photos taken.");
		KeyShotType = E("KeyShotType", "Alt+g", "Choose face, head and chest, or full body, and line up for it.");
		KeyFrameSubject = E("KeyFrameSubject", "Alt+f", "Line the view up to photograph the selected person.");
		KeyRaiseLower = E("KeyRaiseLower", "Alt+h", "Raise or lower whatever is in your hand, the phone included.");
		KeyHands = E("KeyHands", "Dash", "Pick up the selected item, or choose what to do with whatever you are holding.");
		KeySelfActions = E("KeySelfActions", "", "The mod's own list of your actions. Left unbound because the game's own E wheel does the same thing and is read aloud already. Bind it if that ever stops working.");
		KeyReadInventory = E("KeyReadInventory", "Ctrl+i", "Read what you are carrying, without opening the bag.");
		KeyListExits = E("KeyListExits", "Ctrl+d", "Ways out: which rooms you can reach and whether the doors are open.");
		KeyHoldTarget = E("KeyHoldTarget", "Ctrl+End", "Ask the selected person to wait where they are.");
		KeyExplore = E("KeyExplore", "Ctrl+x", "Explore cursor in the house, tile browsing in menus.");
		KeyScanRoom = E("KeyScanRoom", "Alt+r", "Scan the room: wall distances, then all twelve clock directions.");
		KeyLookAhead = E("KeyLookAhead", "Alt+Home", "Describe whatever is directly ahead.");
		KeyHelp = E("KeyHelp", "F1", "Read the keyboard layout aloud.");
		KeyCycleSpeech = E("KeyCycleSpeech", "Ctrl+F12", "Switch to the next speech engine and remember it.");
		KeySpeechTest = E("KeySpeechTest", "Ctrl+F1", "Speak a test phrase through every available speech engine in turn.");
		KeyDump = E("KeyDump", "Ctrl+F2", "Write a diagnostics dump to UserData/HouseAccess.");
		KeyHookReport = E("KeyHookReport", "Ctrl+F7", "Write the hook report: every patch the mod tried, with the type and method names it looked for. This is the file to send in when a game update breaks hooks.");
		KeyToggleMod = E("KeyToggleMod", "Ctrl+F3", "Enable or disable House Access.");
		try
		{
			MelonPreferences.Save();
			Log.Info("Preferences saved to BepInEx/config/HouseAccess.HouseAccess.cfg.");
		}
		catch (Exception ex)
		{
			Log.Warn("Could not save preferences: " + ex.Message);
		}
		Log.Verbose = Verbose.Value;
	}

	private static MelonPreferences_Entry<T> E<T>(string id, T def, string desc)
	{
		return _cat.CreateEntry<T>(id, def, (string)null, desc, false, false, (ValueValidator)null, (string)null);
	}

	public static Chord Combo(MelonPreferences_Entry<string> entry)
	{
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		if (entry == null)
		{
			return default(Chord);
		}
		string value = entry.Value;
		if (string.IsNullOrWhiteSpace(value))
		{
			return default(Chord);
		}
		if (ChordCache.TryGetValue(value, out var value2))
		{
			return value2;
		}
		Chord chord = default(Chord);
		string text = value;
		string[] array = value.Split('+');
		foreach (string text2 in array)
		{
			string text3 = text2.Trim().ToLowerInvariant();
			if (text3 == "ctrl" || text3 == "control")
			{
				chord.Ctrl = true;
				text = Strip(text, text2);
			}
			else if (text3 == "alt")
			{
				chord.Alt = true;
				text = Strip(text, text2);
			}
			else if (text3 == "shift")
			{
				chord.Shift = true;
				text = Strip(text, text2);
			}
		}
		chord.Key = ParseKey(text.Trim(' ', '+'));
		ChordCache[value] = chord;
		return chord;
	}

	private static string Strip(string whole, string token)
	{
		int num = whole.IndexOf(token, StringComparison.OrdinalIgnoreCase);
		return (num < 0) ? whole : whole.Remove(num, token.Length);
	}

	public static void ClearChordCache()
	{
		ChordCache.Clear();
	}

	public static List<string> Bindings()
	{
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		List<string> list = new List<string>();
		FieldInfo[] fields = typeof(Prefs).GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.Name.StartsWith("Key") && fieldInfo.GetValue(null) is MelonPreferences_Entry<string> val)
			{
				Chord chord = Combo(val);
				string value = (chord.Ctrl ? "Ctrl+" : "") + (chord.Alt ? "Alt+" : "") + (chord.Shift ? "Shift+" : "");
				list.Add($"{fieldInfo.Name} = \"{val.Value}\" -> {value}{chord.Key}");
			}
		}
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	public static KeyCode Key(MelonPreferences_Entry<string> entry)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		if (entry == null)
		{
			return (KeyCode)0;
		}
		return ParseKey(entry.Value);
	}

	private static KeyCode ParseKey(string raw)
	{
		//IL_0618: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0622: Unknown result type (might be due to invalid IL or missing references)
		//IL_0629: Unknown result type (might be due to invalid IL or missing references)
		//IL_062a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_062e: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0610: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrWhiteSpace(raw))
		{
			return (KeyCode)0;
		}
		if (KeyCache.TryGetValue(raw, out var value))
		{
			return value;
		}
		string text = raw.Trim().Replace(" ", string.Empty);
		switch (text.ToLowerInvariant())
		{
		case "[":
			text = "LeftBracket";
			break;
		case "]":
			text = "RightBracket";
			break;
		case "\\":
			text = "Backslash";
			break;
		case ";":
			text = "Semicolon";
			break;
		case "'":
			text = "Quote";
			break;
		case ",":
			text = "Comma";
			break;
		case ".":
			text = "Period";
			break;
		case "/":
			text = "Slash";
			break;
		case "up":
			text = "UpArrow";
			break;
		case "down":
			text = "DownArrow";
			break;
		case "left":
			text = "LeftArrow";
			break;
		case "right":
			text = "RightArrow";
			break;
		case "enter":
			text = "Return";
			break;
		case "esc":
			text = "Escape";
			break;
		case "pageup":
			text = "PageUp";
			break;
		case "pagedown":
			text = "PageDown";
			break;
		case "pgup":
			text = "PageUp";
			break;
		case "pgdn":
			text = "PageDown";
			break;
		case "del":
			text = "Delete";
			break;
		case "dash":
		case "hyphen":
		case "-":
			text = "Minus";
			break;
		case "=":
		case "plus":
		case "equal":
			text = "Equals";
			break;
		case "space":
			text = "Space";
			break;
		case "ins":
			text = "Insert";
			break;
		case "backspace":
			text = "Backspace";
			break;
		case "`":
			text = "BackQuote";
			break;
		}
		KeyCode result = (KeyCode)0;
		try
		{
			if (!Enum.TryParse<KeyCode>(text, ignoreCase: true, out result))
			{
				Log.Warn("Unrecognised key name '" + raw + "' in preferences; that bind is disabled.");
				result = (KeyCode)0;
			}
		}
		catch
		{
			result = (KeyCode)0;
		}
		KeyCache[raw] = result;
		return result;
	}

	public static void ClearKeyCache()
	{
		KeyCache.Clear();
	}
}
