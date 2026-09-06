using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using HouseAccess.Game;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.UI;
using HouseAccess.World;
using EekCharacterEngine;
using EekCharacterEngine.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HouseAccess;

[BepInPlugin("HouseAccess.HouseAccess", "House Access", "1.1.8")]
public class HouseAccessMod : BasePlugin
{
	private Harmony _harmony;

	public override void Load()
	{
		global::HouseAccess.Log.Bind(base.Log);
		global::HouseAccess.Log.Info("House Access 1.1.8 (BepInEx build) starting. Injection-free driver, menu value tracking, held first focus, label fallbacks, repeat key in menus.");
		MelonLoader.MelonPreferences.Bind(Config);
		Prefs.Init();
		Overrides.Load();
		CutsceneBridge.Load();
		Speaker.Init();
		global::HouseAccess.Log.Info("Speech backend: " + Speaker.BackendName);
		try
		{
			_harmony = new Harmony("HouseAccess.HouseAccess");
			HouseAccess.Game.Patches.Apply(_harmony);
		}
		catch (Exception e)
		{
			global::HouseAccess.Log.Error("Harmony setup failed entirely", e);
		}
		// The driver used to be an injected MonoBehaviour: AddComponent<ModDriver>().
		// That call goes Il2CppUtils.AddComponent -> ClassInjector.RegisterTypeInIl2Cpp
		// -> InjectorHelpers.Setup(), which installs GenericMethod_GetMethod_Hook - the
		// frame this build of House Party dies in with an AccessViolation. Driver.Pump
		// is now called from Harmony postfixes on game methods that already run every
		// frame, so the mod never injects a managed type into il2cpp.
		if (HouseAccess.Game.Patches.DriverHosts == 0)
		{
			global::HouseAccess.Log.Error("No per-frame host could be patched - House Access will not tick. See the Harmony lines above in LogOutput.log.");
			Speaker.SayNow("House Access could not start. No update host was found.");
		}
		else
		{
			global::HouseAccess.Log.Info($"Driver: {HouseAccess.Game.Patches.DriverHosts} update host(s), {HouseAccess.Game.Patches.DriverLateHosts} late host(s).");
		}
	}

	public override bool Unload()
	{
		try
		{
			Beacon.Shutdown();
			Speaker.Shutdown();
		}
		catch (Exception e)
		{
			global::HouseAccess.Log.Error("Unload failed", e);
		}
		_harmony?.UnpatchSelf();
		return true;
	}
}

/// <summary>
/// Per-frame driver for the whole mod. This used to be an injected MonoBehaviour
/// (ModDriver); it is now a plain static class pumped from Harmony postfixes on game
/// methods that already run every frame - see Patches.PatchDriverHosts. Il2Cpp class
/// injection is what kills this build of House Party, so the mod injects nothing.
/// Several hosts can be alive in the same frame, so both pumps ignore repeat calls
/// within one frame: Keys.Hit() must be evaluated exactly once per frame.
/// </summary>
internal static class Driver
{
	private static bool _enabled = true;

	private static bool _greeted;

	private static bool _descriptionsRetried;

	private static float _greetAt;

	private static int _lastScene = int.MinValue;

	private static string _lastSceneName;

	private static bool _primed;

	private static int _tickFrame = -1;

	private static int _lateFrame = -1;

	/// <summary>
	/// Update phase. Safe to call from any number of per-frame hosts; the first call
	/// in a frame does the work and the rest return immediately.
	/// </summary>
	public static void Pump()
	{
		int frameCount = Time.frameCount;
		if (frameCount == _tickFrame)
		{
			return;
		}
		_tickFrame = frameCount;
		if (!_primed)
		{
			_primed = true;
			CheckScene(force: true);
		}
		else
		{
			CheckScene(force: false);
		}
		Log.Guard("Speech", delegate
		{
			Speaker.Tick(Time.unscaledTime);
		});
		Log.Guard("SpeechTest", delegate
		{
			SpeechTest.Tick(Time.unscaledTime);
		});
		if (Keys.Hit(Prefs.KeyCycleSpeech))
		{
			Speaker.CycleBackend();
		}
		else if (Keys.Hit(Prefs.KeySpeechTest))
		{
			SpeechTest.Start();
		}
		else if (Keys.Hit(Prefs.KeyToggleMod))
		{
			_enabled = !_enabled;
			if (!_enabled)
			{
				Navigator.Stop(null);
				Hold.Release(null);
				Speaker.SayNow("House Access off.");
			}
			else
			{
				Speaker.SayNow("House Access on.");
			}
		}
		else if (_enabled)
		{
			Log.Guard("Refs", GameRefs.Tick);
			if (!_greeted && Time.realtimeSinceStartup > _greetAt)
			{
				_greeted = true;
				Log.Guard("Descriptions", LoadDescriptions);
				Speaker.Say($"House Access ready, using {Speaker.BackendName}. Press {Prefs.KeyHelp.Value} for keys.", Pri.High);
			}
			else if (_greeted && !_descriptionsRetried && Time.realtimeSinceStartup > _greetAt + 20f)
			{
				// The greet-time scan can run before any characters have spawned (intro
				// cutscene), which leaves the description template without names; try
				// once more once the party is around.
				_descriptionsRetried = true;
				Log.Guard("Descriptions", LoadDescriptions);
			}
			Log.Guard("Find", Finder.Tick);
			Log.Guard("Dialogue", DialogueBridge.Tick);
			Log.Guard("Combat", CombatBridge.Tick);
			Log.Guard("Customize", CustomizeBridge.Tick);
			Log.Guard("Thermostat", Thermostats.Tick);
			Log.Guard("Cutscene", CutsceneBridge.Tick);
			Log.Guard("Actions", ActionPicker.Tick);
			Log.Guard("Hands", Hands.Tick);
			Log.Guard("UseWith", UseWithBridge.Tick);
			Log.Guard("Inventory", InventoryBridge.Tick);
			Log.Guard("Opportunities", OpportunityBridge.Tick);
			Log.Guard("Camera", CameraBridge.Tick);
			Log.Guard("ConsoleRead", ConsoleReader.Tick);
			Log.Guard("ConsoleBuild", ConsoleBuilder.Tick);
			Log.Guard("InputConfig", InputConfigBridge.Tick);
			Log.Guard("KeySetup", KeyEditor.Tick);
			Log.Guard("Phone", PhoneBridge.Tick);
			Log.Guard("Wheel", WheelBridge.Tick);
			Log.Guard("Radial", RadialBridge.Tick);
			Log.Guard("TextFields", TextFields.Tick);
			Log.Guard("Menu", MenuReader.Tick);
			Log.Guard("Loading", LoadingBridge.Tick);
			Log.Guard("Hold", Hold.Tick);
			Log.Guard("Radar", Radar.Tick);
			Log.Guard("Rooms", Reporter.Tick);
			Log.Guard("Navigator", Navigator.Tick);
			Log.Guard("Beacon", Beacon.Tick);
			Log.Guard("Input", HandleKeys);
		}
	}

	/// <summary>
	/// Late phase, driven from PlayerCharacter.LateUpdate so the facing write lands
	/// after the player's own late logic. If that host is missing this phase is simply
	/// skipped: Navigator.Tick already calls ApplyFacing in the update phase, so the
	/// only cost is a slightly coarser turn, never a lost feature.
	/// </summary>
	public static void PumpLate()
	{
		int frameCount = Time.frameCount;
		if (frameCount == _lateFrame)
		{
			return;
		}
		_lateFrame = frameCount;
		if (_enabled)
		{
			Log.Guard("NavigatorLate", Navigator.LateTick);
		}
	}

	private static void CheckScene(bool force)
	{
		Scene active;
		try
		{
			active = SceneManager.GetActiveScene();
		}
		catch
		{
			return;
		}
		int buildIndex = active.buildIndex;
		string name = active.name;
		if (!force && buildIndex == _lastScene && name == _lastSceneName)
		{
			return;
		}
		_lastScene = buildIndex;
		_lastSceneName = name;
		if (force && buildIndex < 0 && string.IsNullOrEmpty(name))
		{
			return;
		}
		ResetForScene(buildIndex, name);
	}

	private static void ResetForScene(int buildIndex, string sceneName)
	{
		GameRefs.NoteSceneLoaded();
		GameRefs.ForgetLabels();
		GameRefs.Invalidate();
		Radar.Reset();
		Navigator.Reset();
		DialogueBridge.Reset();
		RadialBridge.Reset();
		WheelBridge.Reset();
		PhoneBridge.Reset();
		Finder.Reset();
		ConsoleReader.Reset();
		ConsoleBuilder.Reset();
		InputConfigBridge.Reset();
		KeyEditor.Reset();
		CameraBridge.Reset();
		MenuReader.Reset();
		LoadingBridge.Reset();
		Reporter.ResetRoom();
		ActionPicker.Reset();
		Hands.Reset();
		OpportunityBridge.Reset();
		InventoryBridge.Reset();
		UseWithBridge.Reset();
		CutsceneBridge.Reset();
		Thermostats.Reset();
		Photos.Reset();
		CombatBridge.Reset();
		CustomizeBridge.Reset();
		MiniGameBridge.Reset();
		ReactionBridge.Reset();
		Hold.Reset();
		Log.Debug("Scene initialised: " + sceneName);
	}

	private static void OpenSelfActions()
	{
		InteractiveItem val = GameRefs.SelfInteraction();
		if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
		{
			Speaker.SayNow("No personal actions available here.");
		}
		else
		{
			ActionPicker.Open(val, "Yourself");
		}
	}

	private static void LoadDescriptions()
	{
		List<string> list = new List<string>();
		foreach (Character item in GameRefs.Npcs())
		{
			string text = GameRefs.NameOf(item);
			if (!string.IsNullOrWhiteSpace(text) && !list.Contains(text))
			{
				list.Add(text);
			}
		}
		Appearance.Load(list);
	}

	private static void HandleKeys()
	{
		if (Keys.Hit(Prefs.KeyCancel))
		{
			Speaker.Stop();
			Navigator.Stop(null);
			ActionPicker.Cancel(announce: false);
			Hold.Release(null);
			if (GameRefs.Seated() && GameRefs.StandUp())
			{
				Speaker.SayNow("Standing up.");
			}
			else
			{
				Speaker.SayNow("Stopped.");
			}
			return;
		}
		if (Keys.Hit(Prefs.KeyHelp))
		{
			Reporter.AnnounceKeys();
			return;
		}
		if (Keys.Hit(Prefs.KeyCombatStatus))
		{
			if (!MiniGameBridge.ReadStatus())
			{
				CombatBridge.ReadStatus();
			}
			return;
		}
		if (Keys.Hit(Prefs.KeyEndFight))
		{
			CombatBridge.ResolveFight();
			return;
		}
		if (!Keys.TypingInField)
		{
			float num = (Keys.Shift ? 90f : Mathf.Max(5f, Prefs.TurnStepDegrees.Value));
			if (Keys.Down(Prefs.Key(Prefs.KeyTurnLeft)))
			{
				Navigator.TurnBy(0f - num);
				return;
			}
			if (Keys.Down(Prefs.Key(Prefs.KeyTurnRight)))
			{
				Navigator.TurnBy(num);
				return;
			}
		}
		if (Keys.Hit(Prefs.KeyReadConsole))
		{
			ConsoleReader.ReadFeedback();
			return;
		}
		if (Keys.Hit(Prefs.KeySetup))
		{
			KeyEditor.Toggle();
			return;
		}
		if (Keys.Hit(Prefs.KeyFind))
		{
			Finder.Toggle();
			return;
		}
		if (Keys.Hit(Prefs.KeyTypeText))
		{
			TextFields.Focus();
			return;
		}
		if (Keys.Hit(Prefs.KeyFeelings))
		{
			Reporter.AnnounceFeelings();
			return;
		}
		if (Keys.Hit(Prefs.KeyWhoSeesMe))
		{
			Watchers.AnnounceObservers();
			return;
		}
		if (Keys.Hit(Prefs.KeyReachOut))
		{
			Hands.Reach(0.35f);
			return;
		}
		if (Keys.Hit(Prefs.KeyReachIn))
		{
			Hands.Reach(-0.35f);
			return;
		}
		if (Keys.Hit(Prefs.KeyPhotos))
		{
			Photos.ReadAll();
			return;
		}
		if (Keys.Hit(Prefs.KeyCameraView))
		{
			CameraBridge.ToggleView();
			return;
		}
		if (CameraBridge.Viewing && Keys.Hit(Prefs.KeyUiActivate))
		{
			CameraBridge.NextPhoto();
			return;
		}
		if (Keys.Hit(Prefs.KeyShotType))
		{
			Reporter.CycleShot();
			return;
		}
		if (Keys.Hit(Prefs.KeyFrameSubject))
		{
			Reporter.FrameSubject();
			return;
		}
		if (Keys.Hit(Prefs.KeyRaiseLower))
		{
			Hands.RaiseOrLower();
			return;
		}
		if (Keys.Hit(Prefs.KeyHands))
		{
			Hands.Use();
			return;
		}
		if (Keys.Hit(Prefs.KeySelfActions))
		{
			OpenSelfActions();
			return;
		}
		if (Keys.Hit(Prefs.KeyReadInventory))
		{
			InventoryBridge.ReadContents();
			return;
		}
		if (Keys.Hit(Prefs.KeyListExits))
		{
			Reporter.AnnounceExits();
			return;
		}
		if (Keys.Hit(Prefs.KeyHoldTarget))
		{
			Hold.Toggle();
			return;
		}
		if (Keys.Hit(Prefs.KeyExplore))
		{
			if (GameRefs.InPlayScene)
			{
				Speaker.SayNow("Only on the character screen.");
			}
			else
			{
				CustomizeBridge.Toggle();
			}
			return;
		}
		if (Keys.Hit(Prefs.KeyScanRoom))
		{
			RoomScan.ScanRoom();
			return;
		}
		bool flag = DialogueBridge.HasChoices || RadialBridge.Active || WheelBridge.Active || PhoneBridge.Active || Finder.Active || InputConfigBridge.Active || KeyEditor.Active || ActionPicker.Active || Hands.Active || OpportunityBridge.Active || InventoryBridge.Active || UseWithBridge.Active || CustomizeBridge.Active || MenuReader.Active;
		if (Keys.Hit(Prefs.KeyNextFacing))
		{
			Radar.Next();
			Navigator.FaceCurrentQuiet();
		}
		else if (Keys.Hit(Prefs.KeyPrevFacing))
		{
			Radar.Prev();
			Navigator.FaceCurrentQuiet();
		}
		else if (Keys.Hit(Prefs.KeyNextTarget))
		{
			Radar.Next();
		}
		else if (Keys.Hit(Prefs.KeyPrevTarget))
		{
			Radar.Prev();
		}
		else if (Keys.Hit(Prefs.KeyCycleFilterBack))
		{
			Radar.CycleFilter(-1);
		}
		else if (Keys.Hit(Prefs.KeyCycleFilter))
		{
			Radar.CycleFilter(1);
		}
		else if (Keys.Hit(Prefs.KeyRepeatTarget) && !flag)
		{
			Radar.AnnounceCurrent();
		}
		else if (Keys.Hit(Prefs.KeyListInteractions))
		{
			Radar.AnnounceInteractions();
		}
		else if (Keys.Hit(Prefs.KeyInteract))
		{
			Radar.InteractWithCurrent();
		}
		else if (Keys.Hit(Prefs.KeyFaceTarget))
		{
			Navigator.FaceCurrent();
		}
		else if (Keys.Hit(Prefs.KeyAutoWalk))
		{
			Navigator.ToggleWalk();
		}
		else if (Keys.Hit(Prefs.KeyToggleBeacon))
		{
			Beacon.Toggle();
		}
		else if (Keys.Hit(Prefs.KeyAnnounceLocation))
		{
			Reporter.AnnounceLocation();
		}
		else if (Keys.Hit(Prefs.KeyAnnounceStatus))
		{
			Reporter.AnnounceStatus();
		}
		else if (Keys.Hit(Prefs.KeyAnnounceOccupants))
		{
			Reporter.AnnounceOccupants();
		}
		else if (Keys.Hit(Prefs.KeyTargetStatus))
		{
			Reporter.AnnounceTargetStatus();
		}
		else if (Keys.Hit(Prefs.KeyReadScreen))
		{
			if (DialogueBridge.Active)
			{
				DialogueBridge.RepeatLast();
			}
			else
			{
				MenuReader.ReadAll();
			}
		}
		else if (Keys.Hit(Prefs.KeyLookAhead))
		{
			Navigator.LookAhead();
		}
		else if (Keys.Hit(Prefs.KeyDump))
		{
			Diagnostics.Dump();
		}
	}
}
