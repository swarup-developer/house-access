using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using HouseAccess.Speech;
using HouseAccess.UI;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Canvas;
using EekCharacterEngine.Interaction;
using EekCharacterEngine.Support;
using EekEvents;
using EekUI;
using HouseParty;
using HouseParty.Interface;
using UnityEngine;

using Speaker = HouseAccess.Speech.Speaker;

namespace HouseAccess.Game;

public static class Patches
{
	private static HarmonyLib.Harmony _harmony;

	private static string _lastSpeaker;

	private static int _lastWidgetId;

	private static readonly HashSet<string> FirstCalls = new HashSet<string>();

	// Built once: these two run on every patched host, every frame.
	private static readonly Action PumpAction = Driver.Pump;

	private static readonly Action PumpLateAction = Driver.PumpLate;

	public static int Applied { get; private set; }

	public static int Failed { get; private set; }

	/// <summary>Number of per-frame hosts driving Driver.Pump. Zero means the mod is inert.</summary>
	public static int DriverHosts { get; private set; }

	/// <summary>Number of hosts driving Driver.PumpLate. Zero is survivable, see Driver.PumpLate.</summary>
	public static int DriverLateHosts { get; private set; }

	public static void Apply(HarmonyLib.Harmony harmony)
	{
		_harmony = harmony;
		// First, and outside anything that can throw below: without a per-frame host
		// nothing else in the mod ever runs.
		PatchDriverHosts();
		// GOG v1.1.7 names its dialogue methods with interop placeholders; these four
		// are the real methods that show a line, begin a conversation, fill the reply
		// buttons and run when a reply button is clicked.
		Patch(typeof(DialogueUI), "JDBGBGNEMJH", "OnDialogueText");
		Patch(typeof(DialogueUI), "ECKCCJBNEAF", "OnDialogueStart");
		Patch(typeof(DialogueUI), "ILHLCBDDIBH", "OnResponsesQueued");
		Patch(typeof(DialogueUI), "OBDJDECDLHG", "OnResponseChosen");
		// NarratorManager.NarrateText does not exist in this build; narration lines are
		// announced through the DialogueUI patches above.
		Patch(typeof(ThoughtBubbleManager), "Show", "OnThought");
		Patch(typeof(InteractivePhone), "PlayTextMessageNotification", "OnTextMessage");
		Patch(typeof(Thermostat), "Tamper", "OnTamper");
		Patch(typeof(ThoughtBubble), "Display", "OnThoughtDisplay");
		Patch(typeof(MessageHandler), "OnDisplayMessage", "OnDisplayMessage", new Type[3]
		{
			typeof(string),
			typeof(string),
			typeof(bool)
		});
		Patch(typeof(PopupManager), "Display", "OnPopup", new Type[2]
		{
			typeof(string),
			typeof(string)
		});
		Patch(typeof(UIRadialMenu), "SetInteractions", "OnRadialOpened");
		Patch(typeof(UIRadialMenu), "OnChoose", "OnRadialChosen");
		Patch(typeof(RadialMenu), "SetInteractions", "OnWheelOpened");
		Patch(typeof(RadialMenu), "OnChoose", "OnWheelChosen");
		// The game itself calls RadialMenu.OnChoose from native code while a wheel's
		// options are being rebuilt at scene and conversation transitions, and an
		// index that no longer exists makes the game throw ArgumentOutOfRangeException
		// ("During invoking native->managed trampoline") and freeze. Prefix-guard the
		// index so a choice that cannot name a current option is skipped instead.
		PatchPrefix(typeof(RadialMenu), "OnChoose", "GuardWheelChoose");
		Patch(typeof(InventoryUI), "OnItemClick", "OnInventoryClick");
		if (Prefs.PatchWidgetFocus.Value)
		{
			// ToolTipProvider.ToolTipChanged is not named in this build; its single
			// ToolTipAssociate method is the tooltip-change sink.
			Patch(typeof(ToolTipProvider), "OKBDGDAJKNC", "OnToolTipChanged");
			// Inventory/use-select hover methods do not exist in this GOG v1.1.7 build;
			// widget focus is announced through the EekUI OnSelect patches below and the
			// bridges' own navigation announcements.
		}
		if (Prefs.PatchWidgetFocus.Value)
		{
			Patch(typeof(EekUIButton), "OnSelect", "OnButtonCheckSelect");
			Patch(typeof(EekUIToggle), "OnSelect", "OnToggleCheckSelect");
			Patch(typeof(EekUISlider), "OnSelect", "OnSliderCheckSelect");
			Patch(typeof(EekUIDropdown), "OnSelect", "OnDropdownCheckSelect");
		}
		else
		{
			Log.Info("Widget focus patches disabled (PatchWidgetFocus = false).");
		}
		// MiniGame.Scored / ReactionHandler.PerformEventTriggersForAllCharactersInVicinity
		// patches are omitted: those classes do not exist in this GOG v1.1.7 build.
		Patch(typeof(MiniGameManager), "OnStart1TGame", "OnGameStart");
		Patch(typeof(MiniGameManager), "OnStart2TGame", "OnGameStart");
		Patch(typeof(MiniGameManager), "OnEndGame", "OnGameEnd");
		if (Prefs.PatchPreferences.Value)
		{
			// SettingsManager methods are obfuscated in this build and cannot be
			// patched reliably. The settings canvas is still announced by the
			// MenuReader when it opens, and sliders/toggles are read by the
			// widget-focus patches below.
			Log.Info("SettingsManager patches skipped (methods not found in this build).");
		}
		else
		{
			Log.Info("Preference patches disabled (PatchPreferences = false).");
		}
		Log.Info($"Harmony: {Applied} patches applied, {Failed} skipped.");
	}



	/// <summary>
	/// Hooks Driver.Pump onto game methods that Unity already calls every frame.
	/// The mod used to ride an injected MonoBehaviour instead. Injection runs
	/// ClassInjector.RegisterTypeInIl2Cpp -> InjectorHelpers.Setup(), which installs
	/// Il2CppInterop's GenericMethod_GetMethod_Hook - the frame House Party dies in
	/// with an AccessViolation on this build, per BepInEx\ErrorLog.log. Harmony
	/// patching an existing il2cpp method injects nothing: Il2CppInterop.HarmonySupport
	/// only calls ClassInjector.IsManagedTypeInjected, a lookup, never Setup(). So
	/// this path does not install that hook. Whether the game then boots can only be
	/// confirmed by launching it.
	///
	/// Several hosts are patched on purpose - each one only lives in some scenes -
	/// and Driver.Pump ignores repeat calls inside the same frame, so overlap is
	/// free. Every method below was verified present in BepInEx\interop metadata
	/// for House Party GOG v1.1.7. Which of them is actually alive in a given scene
	/// is not knowable from metadata; the "Driver hosts:" log line below and the
	/// spoken readiness greeting are the runtime check.
	/// </summary>
	private static void PatchDriverHosts()
	{
		// Unity's own UI driver: alive in the main menu, the pause menu and in play.
		if (PatchDriverHost(typeof(UnityEngine.EventSystems.EventSystem), "Update"))
		{
			DriverHosts++;
		}
		// Game-side managers, for frames where no EventSystem is active.
		if (PatchDriverHost(typeof(TransitionalSceneManager), "Update"))
		{
			DriverHosts++;
		}
		// EekGamesIntroManager overrides TransitionalSceneManager.Update, so the base
		// patch above does not cover the intro scene; DisclaimerManager declares no
		// Update of its own and is covered by the base.
		if (PatchDriverHost(typeof(EekGamesIntroManager), "Update"))
		{
			DriverHosts++;
		}
		if (PatchDriverHost(typeof(GOGGalaxyManager), "Update"))
		{
			DriverHosts++;
		}
		if (PatchDriverHost(typeof(EekCharacterEngine.GameManager), "Update"))
		{
			DriverHosts++;
		}
		if (PatchDriverHost(typeof(EekCharacterEngine.AudioManager), "Update"))
		{
			DriverHosts++;
		}
		if (PatchDriverHost(typeof(CutSceneManager), "Update"))
		{
			DriverHosts++;
		}
		if (PatchDriverHost(typeof(MessageHandler), "Update"))
		{
			DriverHosts++;
		}
		// Late phase: the player's own LateUpdate, so Navigator's facing write happens
		// after the game has finished moving the player for this frame. Verified in
		// BepInEx\interop metadata: PlayerCharacter.LateUpdate is the most derived
		// LateUpdate in the player chain - HousePartyPlayerCharacter, BobbyCharacter
		// and FemalePlayerCharacter declare none - so this covers both playable
		// characters.
		if (PatchDriverHostLate(typeof(EekCharacterEngine.PlayerCharacter), "LateUpdate"))
		{
			DriverLateHosts++;
		}
		Log.Info($"Driver hosts: {DriverHosts} update, {DriverLateHosts} late.");
	}

	private static bool PatchDriverHost(Type target, string methodName)
	{
		return Patch(target, methodName, "DriverTick", null, "that per-frame host does not exist in this build");
	}

	private static bool PatchDriverHostLate(Type target, string methodName)
	{
		return Patch(target, methodName, "DriverLateTick", null, "the late phase falls back to the update phase");
	}

	private static void DriverTick()
	{
		Guard(PumpAction);
	}

	private static void DriverLateTick()
	{
		Guard(PumpLateAction);
	}

	private static bool Patch(Type target, string methodName, string postfixName)
	{
		return Patch(target, methodName, postfixName, null, "that announcement is disabled");
	}

	private static bool Patch(Type target, string methodName, string postfixName, Type[] paramTypes)
	{
		return Patch(target, methodName, postfixName, paramTypes, "that announcement is disabled");
	}

	private static bool Patch(Type target, string methodName, string postfixName, Type[] paramTypes, string skipNote)
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		try
		{
			MethodInfo methodInfo = ((paramTypes == null) ? AccessTools.Method(target, methodName, (Type[])null, (Type[])null) : AccessTools.Method(target, methodName, paramTypes, (Type[])null));
			if (methodInfo == null)
			{
				Failed++;
				Log.Warn($"Harmony: {target.Name}.{methodName} not found; {skipNote}.");
				return false;
			}
			MethodInfo method = typeof(Patches).GetMethod(postfixName, BindingFlags.Static | BindingFlags.NonPublic);
			_harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			Applied++;
			Log.Debug("Harmony: patched " + target.Name + "." + methodName);
			return true;
		}
		catch (Exception ex)
		{
			Failed++;
			Log.Warn($"Harmony: could not patch {target.Name}.{methodName}: {ex.Message}");
			return false;
		}
	}

	private static bool PatchPrefix(Type target, string methodName, string prefixName)
	{
		try
		{
			MethodInfo methodInfo = AccessTools.Method(target, methodName, (Type[])null, (Type[])null);
			if (methodInfo == null)
			{
				Failed++;
				Log.Warn($"Harmony: {target.Name}.{methodName} not found; the wheel index guard is disabled.");
				return false;
			}
			MethodInfo method = typeof(Patches).GetMethod(prefixName, BindingFlags.Static | BindingFlags.NonPublic);
			_harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			Applied++;
			Log.Debug("Harmony: prefixed " + target.Name + "." + methodName);
			return true;
		}
		catch (Exception ex)
		{
			Failed++;
			Log.Warn($"Harmony: could not prefix {target.Name}.{methodName}: {ex.Message}");
			return false;
		}
	}

	private static void OnDialogueStart(CharacterBase __0)
	{
		Guard(delegate
		{
			string text = (_lastSpeaker = GameRefs.NameOf(__0));
			if (!string.IsNullOrEmpty(text))
			{
				Speaker.Say(text + " says:", Pri.High);
			}
			DialogueBridge.NotifyStarted(text);
		});
	}

	private static void OnDialogueText(string __0)
	{
		Guard(delegate
		{
			if (Prefs.SpeakDialogue.Value)
			{
				string text2 = TextUtil.Clean(__0);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					DialogueBridge.NotifyLine(text2);
				}
			}
		});
	}

	private static void OnResponsesQueued()
	{
		Guard(DialogueBridge.NotifyResponsesPending);
	}

	private static void OnResponseChosen()
	{
		Guard(DialogueBridge.NotifyResponseChosen);
	}

	private static void OnRadialOpened(UIRadialMenu __instance, string __0)
	{
		Guard(delegate
		{
			RadialBridge.NotifyOpened(__instance, __0);
		});
	}

	private static void OnWheelOpened(RadialMenu __instance, string __0, List<string> __1)
	{
		FirstCall("RadialMenu.SetInteractions");
		Guard(delegate
		{
			InteractiveItem item = Cpp.Read(() => __instance.PGLCMOFAEMF);
			WheelBridge.NotifyOpened(__instance, __0, item);
		});
	}

	private static void OnWheelChosen()
	{
		FirstCall("RadialMenu.OnChoose");
		Guard(WheelBridge.NotifyChosen);
	}

	private static float _lastWheelGuardWarn;

	private static float _lastWheelGuardPass;

	private static string _lastWheelGuardSig;

	/// <summary>
	/// Runs before RadialMenu.OnChoose. The game calls the wheel's OnChoose from
	/// native code while a wheel is being rebuilt at scene and conversation
	/// transitions, and an index that no longer names a current option makes the
	/// game throw ArgumentOutOfRangeException from inside native code and freeze
	/// ("[Error :Il2CppInterop] During invoking native->managed trampoline ...
	/// EekUI.RadialMenu.OnChoose").
	///
	/// The index is checked against every option collection the wheel keeps that is
	/// readable from here - the sorted option tuples WheelBridge announces, the
	/// per-slot buttons, and the per-slot objects - and the call is skipped unless
	/// it is inside the smallest of them. A wheel that is being closed or rebuilt
	/// can keep one collection (the tuples) while clearing the others, so a bound
	/// against any single list lets a stale index through; the smallest list is the
	/// only one an index is guaranteed to be valid against. A wheel with nothing on
	/// it has nothing to choose, so those calls are skipped too. When the guard
	/// lets a call through it logs the index and the three counts once per change,
	/// so if a crash still occurs the next LogOutput.log shows exactly what passed.
	/// </summary>
	private static void GuardWheelChoose(RadialMenu __instance, int __0, ref bool __runOriginal)
	{
		int tupleCount = Cpp.CountOf<Il2CppSystem.ValueTuple<string, bool>>(Cpp.Read(() => __instance.BHNCIKDJNOO));
		int buttonCount = Cpp.CountOf<UnityEngine.UI.Button>(Cpp.Read(() => __instance.FAIFDGOFNIA));
		int slotCount = Cpp.CountOf<GameObject>(Cpp.Read(() => __instance.CBHGENOCLOF));
		int bound = Mathf.Min(tupleCount, Mathf.Min(buttonCount, slotCount));
		string sig = __0 + " of " + tupleCount + " options, " + buttonCount + " buttons, " + slotCount + " slots";
		float unscaledTime = Time.unscaledTime;
		// The tuples list (BHNCIKDJNOO) and the native button/slot arrays can
		// briefly disagree during a wheel rebuild. If they disagree at all, skip
		// to avoid an ArgumentOutOfRangeException from native OnChoose.
		// But also allow the call when tuples match buttons OR tuples match slots
		// (a wheel can have empty slots with buttons+labels, or vice versa).
		// The tuples list is the source of truth for what options the wheel offers.
		// Buttons and slots are visual elements that can outnumber the tuples (empty
		// slots, decorative buttons). Trust tupleCount as the valid range, but also
		// require it to match at least one visual collection when they disagree,
		// because a wheel that is being rebuilt can have a stale tuples list.
		// When tuples <= buttons and tuples <= slots, the tuples are likely accurate.
		bool tuplesAreSmallest = tupleCount <= buttonCount && tupleCount <= slotCount;
		bool safe = bound > 0 && __0 < bound
			&& (tupleCount == buttonCount || tupleCount == slotCount || tuplesAreSmallest);
		if (__0 >= 0 && safe)
		{
			if (sig != _lastWheelGuardSig && unscaledTime - _lastWheelGuardPass > 2f)
			{
				_lastWheelGuardSig = sig;
				_lastWheelGuardPass = unscaledTime;
				Log.Info("Wheel choose passed the guard: " + sig + ".");
			}
			return;
		}
		__runOriginal = false;
		if (unscaledTime - _lastWheelGuardWarn > 1f)
		{
			_lastWheelGuardWarn = unscaledTime;
			Log.Warn("Wheel choose of index " + __0 + " ignored: the wheel offers " + sig + " right now.");
			if (WheelBridge.Active)
			{
				Speaker.SayNow("That option is not available right now.");
			}
		}
	}

	private static void OnRadialChosen(int __0)
	{
		Guard(delegate
		{
			RadialBridge.NotifyChosen(__0);
		});
	}

	private static void OnTamper(Thermostat __instance)
	{
		FirstCall("Thermostat.Tamper");
		Guard(delegate
		{
			Thermostats.NotifyTampered(__instance);
		});
	}

	private static void OnTextMessage()
	{
		FirstCall("InteractivePhone.PlayTextMessageNotification");
		Guard(delegate
		{
			if (Prefs.SpeakDialogue.Value && !DialogueBridge.Active && !DialogueBridge.SpeakerTalking)
			{
				Speaker.Say("A phone buzzes. New message.", Pri.High);
			}
		});
	}

	private static void OnThought(string __0)
	{
		FirstCall("ThoughtBubbleManager.Show");
		Guard(delegate
		{
			if (Prefs.SpeakDialogue.Value)
			{
				string text2 = TextUtil.Clean(__0);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					Speaker.Say("You think. " + TextUtil.Cap(text2, 600), Pri.High);
				}
			}
		});
	}

	private static void OnThoughtDisplay(string __0)
	{
		FirstCall("ThoughtBubble.Display");
		Guard(delegate
		{
			SayThought(__0, isThought: true);
		});
	}

	private static void OnDisplayMessage(string __0, string __1, bool __2)
	{
		FirstCall("MessageHandler.OnDisplayMessage");
		Guard(delegate
		{
			string text = TextUtil.Clean(__0);
			string text2 = TextUtil.Clean(__1);
			string combined;
			if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
			{
				combined = text + ". " + text2;
			}
			else
			{
				combined = !string.IsNullOrWhiteSpace(text) ? text : text2;
			}
			SayThought(combined, __2);
		});
	}

	private static void OnPopup(string __0, string __1)
	{
		FirstCall("PopupManager.Display");
		Guard(delegate
		{
			if (Prefs.SpeakDialogue.Value)
			{
				string text = TextUtil.Clean(__0);
				string text2 = TextUtil.Clean(__1);
				if (!string.IsNullOrWhiteSpace(text) || !string.IsNullOrWhiteSpace(text2))
				{
					Speaker.Say(TextUtil.Cap(string.IsNullOrWhiteSpace(text) ? text2 : (text + ". " + text2), 600), Pri.High);
				}
			}
		});
	}

	private static void SayThought(string text, bool isThought)
	{
		if (Prefs.SpeakDialogue.Value)
		{
			string text2 = TextUtil.Clean(text);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				Speaker.Say((isThought ? "You think. " : string.Empty) + TextUtil.Cap(text2, 600), Pri.High);
			}
		}
	}

	private static void OnButtonCheckSelect(EekUIButton __instance)
	{
		FirstCall("EekUIButton.CheckSelect");
		Guard(delegate
		{
			AnnounceWidget(((UnityEngine.Object)(object)__instance == (UnityEngine.Object)null) ? null : ((Component)__instance).gameObject, (UnityEngine.Object)(object)__instance != (UnityEngine.Object)null && __instance.DFNIODNIFOF);
		});
	}

	private static void OnToggleCheckSelect(EekUIToggle __instance)
	{
		FirstCall("EekUIToggle.CheckSelect");
		Guard(delegate
		{
			AnnounceWidget(((UnityEngine.Object)(object)__instance == (UnityEngine.Object)null) ? null : ((Component)__instance).gameObject, (UnityEngine.Object)(object)__instance != (UnityEngine.Object)null && __instance.DFNIODNIFOF);
		});
	}

	private static void OnSliderCheckSelect(EekUISlider __instance)
	{
		FirstCall("EekUISlider.CheckSelect");
		Guard(delegate
		{
			AnnounceWidget(((UnityEngine.Object)(object)__instance == (UnityEngine.Object)null) ? null : ((Component)__instance).gameObject, (UnityEngine.Object)(object)__instance != (UnityEngine.Object)null && __instance.DFNIODNIFOF);
		});
	}

	private static void OnDropdownCheckSelect(EekUIDropdown __instance)
	{
		FirstCall("EekUIDropdown.CheckSelect");
		Guard(delegate
		{
			AnnounceWidget(((UnityEngine.Object)(object)__instance == (UnityEngine.Object)null) ? null : ((Component)__instance).gameObject, (UnityEngine.Object)(object)__instance != (UnityEngine.Object)null && __instance.DFNIODNIFOF);
		});
	}

	private static void AnnounceWidget(GameObject go, bool highlighted)
	{
		if (!Prefs.SpeakUi.Value || !highlighted || (UnityEngine.Object)(object)go == (UnityEngine.Object)null)
		{
			return;
		}
		if (GameRefs.SceneAge < MenuReader.SettleSeconds)
		{
			// Unity raises OnSelect while a scene is still assembling its UI, and reading a
			// half-built widget gives the wrong name. This used to drop the announcement,
			// which is why a menu could open in silence on the item it starts on; MenuReader
			// now speaks it as soon as the scene has settled.
			MenuReader.HoldControl(go);
			return;
		}
		int instanceID = ((UnityEngine.Object)go).GetInstanceID();
		if (instanceID != _lastWidgetId)
		{
			_lastWidgetId = instanceID;
			MenuReader.AnnounceControl(go);
		}
	}

	private static void OnGameStart()
	{
		FirstCall("MiniGameManager.OnStartGame");
		Guard(MiniGameBridge.OnStarted);
	}

	private static void OnGameEnd()
	{
		FirstCall("MiniGameManager.OnEndGame");
		Guard(MiniGameBridge.OnEnded);
	}

	private static void OnInventoryClick(string __0)
	{
		Guard(delegate
		{
			if (!string.IsNullOrWhiteSpace(__0))
			{
				Speaker.Say("Selected " + TextUtil.Clean(__0) + ".", Pri.High);
			}
		});
	}

	private static void OnHintChosen(HintLoader __instance)
	{
		Guard(delegate
		{
			LoadingBridge.OnHintChanged(__instance);
		});
	}

	private static void OnToolTipChanged(ToolTipAssociate __0)
	{
		Guard(delegate
		{
			LoadingBridge.OnToolTipChanged(__0);
		});
	}

	private static void FirstCall(string name)
	{
		if (FirstCalls.Add(name))
		{
			Log.Info("[patch] first call: " + name);
		}
	}

	private static void Guard(Action body)
	{
		try
		{
			body();
		}
		catch (Exception ex)
		{
			try
			{
				Log.Warn("Patch body threw: " + ex.Message);
			}
			catch
			{
			}
		}
	}
}
