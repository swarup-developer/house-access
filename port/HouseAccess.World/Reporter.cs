using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using HouseAccess.Game;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Motion;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HouseAccess.World;

public static class Reporter
{
	private static readonly string[] Interesting = new string[23]
	{
		"drunk", "intox", "sober", "alcohol", "bac", "mood", "happy", "angry", "sad", "stress",
		"arous", "horny", "love", "friend", "trust", "opinion", "relationship", "energy", "stamina", "health",
		"bladder", "hunger", "thirst"
	};

	private static string _lastRoom;

	private static float _nextRoomCheck;

	private static bool? _wasCrouching;

	private static string _lastGoal;

	private static bool? _wasSeated;

	private static GameRefs.Shot _shot = GameRefs.Shot.Chest;

	private static Character _framingFor;

	private static float _framingUntil;


	public static void AnnounceLocation()
	{
		List<string> list = new List<string>();
		string text = GameRefs.CurrentZoneName();
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(TextUtil.Humanize(text));
			list.Add(GameRefs.InsideHouse ? "inside" : "outside");
		}
		else
		{
			if (!GameRefs.InGame)
			{
				Speaker.Say("Not in the game world. " + SceneName() + ".", Pri.High);
				return;
			}
			list.Add("Area not named");
			list.Add(GameRefs.InsideHouse ? "inside" : "outside");
		}
		string text2 = Activity.ClockTime();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add(text2);
		}
		int count = Radar.PeopleNearby().Count;
		list.Add((count == 0) ? "nobody nearby" : TextUtil.Pluralise(count, "person nearby", "people nearby"));
		Speaker.SayParts(Pri.High, list.ToArray());
	}

	private static string SceneName()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Scene activeScene = SceneManager.GetActiveScene();
			string name = activeScene.name;
			return string.IsNullOrWhiteSpace(name) ? "Unknown screen" : TextUtil.Humanize(name);
		}
		catch
		{
			return "Unknown screen";
		}
	}

	public static void AnnounceOccupants()
	{
		List<Entry> list = Radar.PeopleNearby();
		if (list.Count == 0)
		{
			Speaker.Say("Nobody nearby.", Pri.High);
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Character item in Watchers.Observers())
		{
			string text = GameRefs.NameOf(item);
			if (!string.IsNullOrWhiteSpace(text))
			{
				hashSet.Add(text);
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(TextUtil.Pluralise(list.Count, "person", "people"));
		if (hashSet.Count > 0)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral(", ");
			handler.AppendFormatted(hashSet.Count);
			handler.AppendLiteral(" watching you");
			stringBuilder2.Append(ref handler);
		}
		stringBuilder.Append(". ");
		for (int i = 0; i < list.Count && i < 8; i++)
		{
			Entry entry = list[i];
			stringBuilder.Append(entry.Label);
			stringBuilder.Append(", ");
			stringBuilder.Append(TextUtil.Distance(entry.Distance));
			stringBuilder.Append(", ");
			stringBuilder.Append(TextUtil.ClockPhrase(entry.Clock));
			if (!string.IsNullOrWhiteSpace(entry.Room))
			{
				stringBuilder.Append(", in the ");
				stringBuilder.Append(entry.Room);
			}
			string value = Activity.Describe(entry.Person);
			if (!string.IsNullOrWhiteSpace(value))
			{
				stringBuilder.Append(", ");
				stringBuilder.Append(value);
			}
			if (hashSet.Contains(entry.Label))
			{
				stringBuilder.Append(", watching you");
			}
			stringBuilder.Append(". ");
		}
		Speaker.Say(TextUtil.Cap(stringBuilder.ToString(), 700), Pri.High);
	}

	public static void AnnounceExits()
	{
		List<Entry> list = new List<Entry>();
		foreach (Entry item in Radar.All)
		{
			if (item != null && item.Alive && item.Kind == EntryKind.Door)
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			Radar.ForceSweep();
			foreach (Entry item2 in Radar.All)
			{
				if (item2 != null && item2.Alive && item2.Kind == EntryKind.Door)
				{
					list.Add(item2);
				}
			}
		}
		if (list.Count == 0)
		{
			Speaker.Say("No doors nearby.", Pri.High);
			return;
		}
		list.Sort((Entry a, Entry b) => a.Distance.CompareTo(b.Distance));
		Zone val = GameRefs.CurrentZone();
		string text = GameRefs.NameOf(val);
		StringBuilder stringBuilder = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(text))
		{
			stringBuilder.Append("From the ");
			stringBuilder.Append(TextUtil.Humanize(text));
			stringBuilder.Append(". ");
		}
		stringBuilder.Append(TextUtil.Pluralise(list.Count, "way out", "ways out"));
		stringBuilder.Append(". ");
		for (int num = 0; num < list.Count && num < 8; num++)
		{
			Entry entry = list[num];
			string text2 = GameRefs.DestinationOf(entry.Door, val);
			string value = GameRefs.StateOf(entry.Door) ?? "door";
			if (!string.IsNullOrWhiteSpace(text2))
			{
				stringBuilder.Append("The ");
				stringBuilder.Append(TextUtil.Humanize(text2));
				stringBuilder.Append(", through a ");
				stringBuilder.Append(value);
				stringBuilder.Append(" door");
			}
			else
			{
				stringBuilder.Append(entry.Label);
				stringBuilder.Append(", ");
				stringBuilder.Append(value);
			}
			stringBuilder.Append(", ");
			stringBuilder.Append(TextUtil.Distance(entry.Distance));
			stringBuilder.Append(", ");
			stringBuilder.Append(TextUtil.ClockPhrase(entry.Clock));
			stringBuilder.Append(". ");
		}
		Speaker.Say(TextUtil.Cap(stringBuilder.ToString(), 1000), Pri.High);
	}

	public static void Tick()
	{
		if (Time.unscaledTime < _nextRoomCheck)
		{
			return;
		}
		_nextRoomCheck = Time.unscaledTime + 0.4f;
		if (DialogueBridge.Active || DialogueBridge.SpeakerTalking || CutsceneBridge.Playing)
		{
			return;
		}
		WatchFraming();
		WatchSeated();
		WatchCrouch();
		WatchGoal();
		if (!Prefs.AnnounceRoomChanges.Value)
		{
			return;
		}
		string text = GameRefs.CurrentZoneName();
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, _lastRoom, StringComparison.OrdinalIgnoreCase))
		{
			bool flag = _lastRoom == null;
			_lastRoom = text;
			if (!flag)
			{
				Speaker.Say(TextUtil.Humanize(text));
			}
		}
	}

	public static void ResetRoom()
	{
		_lastRoom = null;
		_wasCrouching = null;
		_wasSeated = null;
		_lastGoal = null;
	}

	private static void WatchGoal()
	{
		string text = GameRefs.GoalText();
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, _lastGoal, StringComparison.OrdinalIgnoreCase))
		{
			bool flag = _lastGoal == null;
			_lastGoal = text;
			if (!flag || !(GameRefs.SceneAge < 5f))
			{
				Speaker.Say(text);
			}
		}
	}

	private static void WatchSeated()
	{
		bool flag = GameRefs.Seated();
		if (_wasSeated.HasValue && _wasSeated.Value == flag)
		{
			return;
		}
		bool flag2 = !_wasSeated.HasValue;
		_wasSeated = flag;
		if (!flag2)
		{
			if (!flag)
			{
				Speaker.Say("Standing.");
				return;
			}
			string text = Prefs.KeyCancel?.Value;
			Speaker.Say(string.IsNullOrWhiteSpace(text) ? "Seated." : ("Seated. Press " + text + " to get up."));
		}
	}

	private static void WatchCrouch()
	{
		PlayerCharacter p = GameRefs.Player;
		if ((UnityEngine.Object)(object)p == (UnityEngine.Object)null)
		{
			return;
		}
		bool flag = Cpp.Read(() => ((Character)p).IsCrouching, fallback: false);
		if (!_wasCrouching.HasValue || _wasCrouching.Value != flag)
		{
			bool flag2 = !_wasCrouching.HasValue;
			_wasCrouching = flag;
			if (!flag2)
			{
				Speaker.Say(flag ? "Crouching." : "Standing.");
			}
		}
	}

	public static void AnnounceStatus()
	{
		PlayerCharacter p = GameRefs.Player;
		if ((UnityEngine.Object)(object)p == (UnityEngine.Object)null)
		{
			Speaker.Say("No player yet. Status is only available once you are in the house.", Pri.High);
			return;
		}
		List<string> list = new List<string>();
		string text = Activity.ClockTime();
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(text);
		}
		string text2 = Activity.Holding();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add("holding " + text2);
		}
		string text3 = Activity.Describe(((Il2CppObjectBase)p).TryCast<Character>());
		if (!string.IsNullOrWhiteSpace(text3))
		{
			list.Add(text3);
		}
		// Energy and bladder are not exposed on this build's player character; the HUD
		// status-bar readings further below still cover the player's state.
		foreach (KeyValuePair<string, string> item in InterestingValues((Character)(object)p))
		{
			list.Add(TextUtil.Humanize(item.Key).ToLowerInvariant() + " " + item.Value);
		}
		foreach (string item2 in StatusBars.Read())
		{
			list.Add(item2);
		}
		if (Cpp.Read(() => ((Character)p).IsCrouching, fallback: false))
		{
			list.Add("crouching");
		}
		if (Cpp.Read(() => ((Character)p).IsNaked, fallback: false))
		{
			list.Add("naked");
		}
		else if (Cpp.Read(() => ((Character)p).IsTopless, fallback: false))
		{
			list.Add("topless");
		}
		else if (Cpp.Read(() => ((Character)p).IsBottomless, fallback: false))
		{
			list.Add("bottomless");
		}
		if (list.Count == 0)
		{
			Speaker.Say("No status values available.", Pri.High);
		}
		else
		{
			Speaker.SayParts(Pri.High, list.ToArray());
		}
	}

	private static bool TryAnnounceInspector()
	{
		if (OpportunityBridge.Active)
		{
			OpportunityBridge.ReadAll();
			return true;
		}
		try
		{
			HouseParty.Interface.MessageHandler mh = Cpp.FindOne<HouseParty.Interface.MessageHandler>(activeOnly: true);
			if (Cpp.Alive((UnityEngine.Object)(object)mh))
			{
				GameObject go = Cpp.Read(() => ((Component)mh).gameObject);
				if (Cpp.Alive((UnityEngine.Object)(object)go) && go.activeInHierarchy)
				{
					List<string> parts = new List<string>();
					foreach (UnityEngine.UI.Text t in go.GetComponentsInChildren<UnityEngine.UI.Text>(false))
					{
						if (!Cpp.Alive((UnityEngine.Object)(object)t))
						{
							continue;
						}
						string s = TextUtil.Clean(Cpp.Read(() => t.text));
						if (!string.IsNullOrWhiteSpace(s) && !parts.Contains(s))
						{
							parts.Add(s);
						}
					}
					foreach (TMPro.TMP_Text t in go.GetComponentsInChildren<TMPro.TMP_Text>(false))
					{
						if (!Cpp.Alive((UnityEngine.Object)(object)t))
						{
							continue;
						}
						string s = TextUtil.Clean(Cpp.Read(() => t.text));
						if (!string.IsNullOrWhiteSpace(s) && !parts.Contains(s))
						{
							parts.Add(s);
						}
					}
					if (parts.Count > 0)
					{
						Speaker.Say(TextUtil.Cap(string.Join(". ", parts), 1000), Pri.High);
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

	public static void AnnounceTargetStatus()
	{
		if (TryAnnounceInspector())
		{
			return;
		}
		Entry entry = Radar.Current;
		if (entry == null || entry.Kind != EntryKind.Person)
		{
			List<Entry> list = Radar.PeopleNearby();
			if (list.Count == 0)
			{
				Radar.ForceSweep();
				list = Radar.PeopleNearby();
			}
			if (list.Count == 0)
			{
				Speaker.Say("Nobody nearby.", Pri.High);
				return;
			}
			list.Sort((Entry a, Entry b) => a.Distance.CompareTo(b.Distance));
			entry = list[0];
		}
		Appearance.Describe(entry.Person);
	}

	public static void FrameSubject()
	{
		FrameSubject(walkThere: false);
	}

	public static void FrameSubject(bool walkThere)
	{
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		Entry current = Radar.Current;
		Character val = ((current != null && current.Kind == EntryKind.Person) ? current.Person : null);
		if (!Cpp.Alive((UnityEngine.Object)(object)val))
		{
			List<Entry> list = Radar.PeopleNearby();
			if (list.Count == 0)
			{
				Speaker.Say("Nobody to photograph.", Pri.High);
				return;
			}
			list.Sort((Entry a, Entry b) => a.Distance.CompareTo(b.Distance));
			val = list[0].Person;
		}
		Vector3 eyePos = GameRefs.EyePos;
		GameRefs.ShotFor(val, _shot, eyePos, out var aim, out var wantDistance);
		Navigator.FacePoint(aim);
		string text = GameRefs.NameOf(val) ?? "them";
		float num = Vector3.Distance(eyePos, aim);
		string text2 = ((_shot == GameRefs.Shot.Face) ? "face" : ((_shot == GameRefs.Shot.Chest) ? "head and chest" : "full body"));
		float num2 = num - wantDistance;
		if (Mathf.Abs(num2) < 0.4f)
		{
			Speaker.Say($"{text2} shot of {text}. In position. {Facing(val, eyePos)}.", Pri.High);
			return;
		}
		if (!walkThere)
		{
			string value = ((num2 > 0f) ? ("step " + TextUtil.Distance(num2) + " closer") : ("step back " + TextUtil.Distance(0f - num2)));
			Speaker.Say($"{text2} shot of {text}. {value}. {Facing(val, eyePos)}.", Pri.High);
			return;
		}
		Vector3 val2;
		try
		{
			val2 = ((Component)val).transform.position;
		}
		catch
		{
			val2 = aim;
		}
		Vector3 val3 = eyePos - val2;
		val3.y = 0f;
		if (val3.sqrMagnitude < 0.01f)
		{
			Speaker.Say(text2 + " shot of " + text + ". Cannot work out where to stand.", Pri.High);
			return;
		}
		Vector3 world = val2 + val3.normalized * wantDistance;
		_framingFor = val;
		_framingUntil = Time.unscaledTime + 30f;
		Speaker.Say(text2 + " shot of " + text + ". Walking into position.", Pri.High);
		Navigator.WalkToPoint(world, "the spot for a " + text2 + " shot of " + text);
	}

	private static void WatchFraming()
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		if (!Cpp.Alive((UnityEngine.Object)(object)_framingFor))
		{
			_framingFor = null;
		}
		else if (Time.unscaledTime > _framingUntil)
		{
			_framingFor = null;
		}
		else if (!Navigator.IsWalking)
		{
			Character framingFor = _framingFor;
			_framingFor = null;
			Vector3 eyePos = GameRefs.EyePos;
			GameRefs.ShotFor(framingFor, _shot, eyePos, out var aim, out var _);
			Navigator.FacePoint(aim);
			string value = ((_shot == GameRefs.Shot.Face) ? "face" : ((_shot == GameRefs.Shot.Chest) ? "head and chest" : "full body"));
			Speaker.Say($"In position for a {value} shot. {Facing(framingFor, eyePos)}.", Pri.High);
		}
	}

	public static void CycleShot()
	{
		_shot = ((_shot == GameRefs.Shot.Face) ? GameRefs.Shot.Chest : ((_shot == GameRefs.Shot.Chest) ? GameRefs.Shot.Full : GameRefs.Shot.Face));
		FrameSubject(walkThere: true);
	}

	private static string Facing(Character who, Vector3 from)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Transform val = Cpp.Read(() => ((Component)who).transform);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				return "facing unknown";
			}
			Vector3 forward = val.forward;
			forward.y = 0f;
			Vector3 val2 = from - val.position;
			val2.y = 0f;
			if (forward.sqrMagnitude < 0.0001f || val2.sqrMagnitude < 0.0001f)
			{
				return "facing unknown";
			}
			float num = Vector3.Angle(forward.normalized, val2.normalized);
			if (num < 45f)
			{
				return "facing you";
			}
			if (num < 115f)
			{
				return "turned side on";
			}
			return "facing away from you";
		}
		catch
		{
			return "facing unknown";
		}
	}

	public static void AnnounceFeelings()
	{
		Entry current = Radar.Current;
		Character val = ((current != null && current.Kind == EntryKind.Person) ? current.Person : null);
		if (!Cpp.Alive((UnityEngine.Object)(object)val))
		{
			List<Entry> list = Radar.PeopleNearby();
			if (list.Count == 0)
			{
				Radar.ForceSweep();
				list = Radar.PeopleNearby();
			}
			if (list.Count == 0)
			{
				Speaker.Say("Nobody nearby.", Pri.High);
				return;
			}
			list.Sort((Entry a, Entry b) => a.Distance.CompareTo(b.Distance));
			val = list[0].Person;
		}
		string text = GameRefs.NameOf(val) ?? "They";
		List<KeyValuePair<string, int>> list2 = FeelingsToward(val);
		if (list2.Count == 0)
		{
			Speaker.Say(text + ". Nothing recorded between you yet.", Pri.High);
			return;
		}
		List<string> list3 = new List<string> { text };
		foreach (KeyValuePair<string, int> item in list2)
		{
			list3.Add($"{item.Key.ToLowerInvariant()} {item.Value}");
		}
		Speaker.SayParts(Pri.High, list3.ToArray());
	}

	public static List<KeyValuePair<string, int>> FeelingsToward(Character c)
	{
		// The named relationship-value store behind feelings does not exist on this
		// game build, so there are no numeric feelings to report.
		return new List<KeyValuePair<string, int>>();
	}

	private static string PlayerName()
	{
		try
		{
			PlayerCharacter player = GameRefs.Player;
			if ((UnityEngine.Object)(object)player == (UnityEngine.Object)null)
			{
				return null;
			}
			Character val = ((Il2CppObjectBase)player).TryCast<Character>();
			return ((UnityEngine.Object)(object)val == (UnityEngine.Object)null) ? null : GameRefs.NameOf(val);
		}
		catch
		{
			return null;
		}
	}

	public static List<string> AllValueNames(Character c)
	{
		// Character value lists are not exposed on this game build.
		return new List<string>();
	}

	private static List<KeyValuePair<string, string>> InterestingValues(Character c)
	{
		// Character value lists are not exposed on this game build.
		return new List<KeyValuePair<string, string>>();
	}

	private static string FindKey(Dictionary<string, string> dict, string term)
	{
		string[] array = new string[3]
		{
			term,
			term.ToUpperInvariant(),
			char.ToUpper(term[0]) + term.Substring(1)
		};
		string[] array2 = array;
		foreach (string c in array2)
		{
			if (Cpp.Read(() => dict.ContainsKey(c), fallback: false))
			{
				return c;
			}
		}
		return null;
	}

	public static void AnnounceKeys()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("House Access keys. ");
		Group(stringBuilder, "Targets", ("next", Prefs.KeyNextTarget), ("previous", Prefs.KeyPrevTarget), ("next category", Prefs.KeyCycleFilter), ("previous category", Prefs.KeyCycleFilterBack), ("repeat", Prefs.KeyRepeatTarget), ("interact", Prefs.KeyInteract), ("what you could do", Prefs.KeyListInteractions));
		Group(stringBuilder, "Moving", ("face it", Prefs.KeyFaceTarget), ("walk to it", Prefs.KeyAutoWalk), ("turn left", Prefs.KeyTurnLeft), ("turn right", Prefs.KeyTurnRight), ("stop everything", Prefs.KeyCancel));
		Group(stringBuilder, "Looking around", ("where am I", Prefs.KeyAnnounceLocation), ("ways out", Prefs.KeyListExits), ("scan the room", Prefs.KeyScanRoom), ("who is nearby", Prefs.KeyAnnounceOccupants), ("look ahead", Prefs.KeyLookAhead), ("who can see me", Prefs.KeyWhoSeesMe));
		Group(stringBuilder, "About you and them", ("your status", Prefs.KeyAnnounceStatus), ("read the screen", Prefs.KeyReadScreen), ("describe a person", Prefs.KeyTargetStatus), ("how they feel", Prefs.KeyFeelings), ("your inventory", Prefs.KeyReadInventory), ("your own actions", Prefs.KeySelfActions), ("pick up or throw", Prefs.KeyHands), ("hold someone", Prefs.KeyHoldTarget));
		// Punch and block are deliberately absent: they are declared in Prefs but no code reads
		// them on this build, so announcing them would promise a key that does nothing. The two
		// listed here are read, and both answer that fighting is unsupported here.
		Group(stringBuilder, "Fighting", ("combat status", Prefs.KeyCombatStatus), ("end the fight", Prefs.KeyEndFight));
		Group(stringBuilder, "Diagnostics", ("write a diagnostics dump", Prefs.KeyDump), ("write the hook report for bug reports", Prefs.KeyHookReport));
		stringBuilder.Append("In any list, arrows move and enter chooses. ");
		// Spelled out because the rule is only useful if its edges are known: the keypad works
		// as well as the number row, zero is the tenth item rather than nothing, ten is as far
		// as numbers reach, and the search box is the one list where a bare number is text
		// being typed instead of a row being chosen.
		stringBuilder.Append("A number reads that item, control and a number picks it, in the game's own menus too. ");
		stringBuilder.Append("Number row or keypad, zero is the tenth item, and numbers reach the first ten only. ");
		stringBuilder.Append("In search a number is part of the name, so there control and a number picks a match. ");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendFormatted(Spoken(Prefs.KeyCancel));
		handler.AppendLiteral(" cancels.");
		stringBuilder2.Append(ref handler);
		Speaker.SayNow(TextUtil.Cap(stringBuilder.ToString(), 2200));
	}

	private static void Group(StringBuilder sb, string title, params (string What, MelonPreferences_Entry<string> Key)[] items)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < items.Length; i++)
		{
			(string, MelonPreferences_Entry<string>) tuple = items[i];
			string text = Spoken(tuple.Item2);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(tuple.Item1 + " " + text);
			}
		}
		if (list.Count != 0)
		{
			sb.Append(title);
			sb.Append(": ");
			sb.Append(string.Join(", ", list));
			sb.Append(". ");
		}
	}

	private static string Spoken(MelonPreferences_Entry<string> entry)
	{
		string text = entry?.Value;
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		return text.Replace("+", " ").Replace("PageUp", "page up").Replace("PageDown", "page down")
			.Replace("Ctrl", "control")
			.Replace("Alt", "alt")
			.Replace("Shift", "shift")
			.Replace("Comma", "comma")
			.Replace("Minus", "minus")
			.Replace("Equals", "equals")
			.Replace("BackQuote", "backtick")
			.Replace("Backspace", "backspace")
			.ToLowerInvariant();
	}

}
