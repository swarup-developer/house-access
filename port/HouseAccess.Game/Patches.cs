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
using Il2CppInterop.Runtime.InteropTypes;
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

	/// <summary>
	/// One line per patch attempt: what the hook is for, which candidate type and
	/// method names were tried, and how it ended. This is what the hook report
	/// (Ctrl+F7) writes out, so a log from an unsupported game build shows exactly
	/// which names must be added to support it.
	/// </summary>
	public sealed class HookRecord
	{
		public string Feature;

		public string TypeCandidates;

		public string MethodCandidates;

		public bool Ok;

		public string Status;
	}

	private static readonly List<HookRecord> HookLog = new List<HookRecord>();

	/// <summary>Every patch attempt from startup, in order.</summary>
	public static IReadOnlyList<HookRecord> Hooks => HookLog;

	private static void Record(string feature, string[] typeNames, string[] methodNames, bool ok, string status)
	{
		HookLog.Add(new HookRecord
		{
			Feature = feature,
			TypeCandidates = string.Join(", ", typeNames),
			MethodCandidates = string.Join(", ", methodNames),
			Ok = ok,
			Status = status
		});
	}

	/// <summary>Number of per-frame hosts driving Driver.Pump. Zero means the mod is inert.</summary>
	public static int DriverHosts { get; private set; }

	/// <summary>Number of hosts driving Driver.PumpLate. Zero is survivable, see Driver.PumpLate.</summary>
	public static int DriverLateHosts { get; private set; }

	/// <summary>
	/// Every game type this file touches is resolved by NAME at runtime (GameType.Of),
	/// never by typeof(...) in an argument list, and every patch body takes only
	/// mod-safe parameter types (strings, ints, Il2CppObjectBase, UnityEngine types).
	///
	/// The reason is the Steam build: on Unity 2022.3.62f2 the interop assembly no
	/// longer carries every type the GOG v1.1.7 build has (EekUI.DialogueUI was the
	/// first one reported missing). A JITted typeof() for a missing type throws
	/// TypeLoadException while the CALL is being evaluated - so when Apply passed
	/// typeof(DialogueUI), the exception killed Apply itself before a single patch
	/// (including every per-frame driver host) could be installed, and the whole mod
	/// sat silent. With name lookup, a missing type now costs one log line and its
	/// own feature, nothing else.
	/// </summary>
	public static void Apply(HarmonyLib.Harmony harmony)
	{
		_harmony = harmony;
		// First, and outside anything that can throw below: without a per-frame host
		// nothing else in the mod ever runs.
		PatchDriverHosts();
		// GOG v1.1.7 names its dialogue methods with interop placeholders; these four
		// are the real methods that show a line, begin a conversation, fill the reply
		// buttons and run when a reply button is clicked. On the Steam build the
		// DialogueUI type itself is reported missing; each miss is skipped with one
		// log line instead of taking the mod down. Add the Steam build's real method
		// names to the candidate lists as they are discovered.
		PatchNamed(DialogueUIType, "JDBGBGNEMJH", "OnDialogueText");
		PatchNamed(DialogueUIType, "ECKCCJBNEAF", "OnDialogueStart");
		PatchNamed(DialogueUIType, "ILHLCBDDIBH", "OnResponsesQueued");
		PatchNamed(DialogueUIType, "OBDJDECDLHG", "OnResponseChosen");
		// NarratorManager.NarrateText does not exist in this build; narration lines are
		// announced through the DialogueUI patches above.
		PatchNamed(ThoughtBubbleManagerType, "Show", "OnThought");
		PatchNamed(InteractivePhoneType, "PlayTextMessageNotification", "OnTextMessage");
		PatchNamed(ThermostatType, "Tamper", "OnTamper");
		PatchNamed(ThoughtBubbleType, "Display", "OnThoughtDisplay");
		PatchNamed(MessageHandlerType, "OnDisplayMessage", "OnDisplayMessage", new Type[3]
		{
			typeof(string),
			typeof(string),
			typeof(bool)
		});
		PatchNamed(PopupManagerType, "Display", "OnPopup", new Type[2]
		{
			typeof(string),
			typeof(string)
		});
		PatchNamed(UIRadialMenuType, "SetInteractions", "OnRadialOpened");
		PatchNamed(UIRadialMenuType, "OnChoose", "OnRadialChosen");
		PatchNamed(RadialMenuType, "SetInteractions", "OnWheelOpened", new Type[2]
		{
			typeof(string),
			typeof(Il2CppSystem.Collections.Generic.List<string>)
		});
		PatchNamed(RadialMenuType, "OnChoose", "OnWheelChosen", new Type[1] { typeof(int) });
		// The game itself calls RadialMenu.OnChoose from native code while a wheel's
		// options are being rebuilt at scene and conversation transitions, and an
		// index that no longer exists makes the game throw ArgumentOutOfRangeException
		// ("During invoking native->managed trampoline") and freeze. Prefix-guard the
		// index so a choice that cannot name a current option is skipped instead.
		PatchNamed(RadialMenuType, "OnChoose", "GuardWheelChoose", null, prefix: true);
		PatchNamed(InventoryUIType, "OnItemClick", "OnInventoryClick");
		if (Prefs.PatchWidgetFocus.Value)
		{
			// ToolTipProvider.ToolTipChanged is not named in this build; its single
			// ToolTipAssociate method is the tooltip-change sink.
			PatchNamed(ToolTipProviderType, "OKBDGDAJKNC", "OnToolTipChanged");
			// Inventory/use-select hover methods do not exist in this GOG v1.1.7 build;
			// widget focus is announced through the EekUI OnSelect patches below and the
			// bridges' own navigation announcements.
			PatchNamed(EekUIButtonType, "OnSelect", "OnButtonCheckSelect");
			PatchNamed(EekUIToggleType, "OnSelect", "OnToggleCheckSelect");
			PatchNamed(EekUISliderType, "OnSelect", "OnSliderCheckSelect");
			PatchNamed(EekUIDropdownType, "OnSelect", "OnDropdownCheckSelect");
		}
		else
		{
			Log.Info("Widget focus patches disabled (PatchWidgetFocus = false).");
		}
		// MiniGame.Scored / ReactionHandler.PerformEventTriggersForAllCharactersInVicinity
		// patches are omitted: those classes do not exist in this GOG v1.1.7 build.
		PatchNamed(MiniGameManagerType, "OnStart1TGame", "OnGameStart");
		PatchNamed(MiniGameManagerType, "OnStart2TGame", "OnGameStart");
		PatchNamed(MiniGameManagerType, "OnEndGame", "OnGameEnd");
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
		if (Failed > 0)
		{
			Log.Info("Skipped hooks are per-feature: everything else keeps working. Send LogOutput.log to get the missing names added.");
		}
	}

	// Candidate qualified names per game type, in the namespace spellings the two
	// interop generations use (current builds expose unprefixed namespaces, older
	// ones the "Il2Cpp" prefixes), then the bare name as a last resort. The first
	// hit wins; a miss on every candidate means the type does not exist in this
	// game build and its hooks are skipped with a single log line.
	private static readonly string[] DialogueUIType = new string[3] { "EekUI.DialogueUI", "Il2CppEekUI.DialogueUI", "DialogueUI" };

	private static readonly string[] ThoughtBubbleManagerType = new string[3] { "ThoughtBubbleManager", "Il2Cpp.ThoughtBubbleManager", "Il2CppEekCharacterEngine.ThoughtBubbleManager" };

	private static readonly string[] InteractivePhoneType = new string[3] { "InteractivePhone", "Il2Cpp.InteractivePhone", "Il2CppHouseParty.InteractivePhone" };

	private static readonly string[] ThermostatType = new string[3] { "HouseParty.Thermostat", "Il2CppHouseParty.Thermostat", "Thermostat" };

	private static readonly string[] ThoughtBubbleType = new string[3] { "EekCharacterEngine.Support.ThoughtBubble", "Il2CppEekCharacterEngine.Support.ThoughtBubble", "ThoughtBubble" };

	private static readonly string[] MessageHandlerType = new string[3] { "HouseParty.Interface.MessageHandler", "Il2CppHouseParty.Interface.MessageHandler", "MessageHandler" };

	private static readonly string[] PopupManagerType = new string[3] { "EekCharacterEngine.PopupManager", "Il2CppEekCharacterEngine.PopupManager", "PopupManager" };

	private static readonly string[] UIRadialMenuType = new string[3] { "EekUI.UIRadialMenu", "Il2CppEekUI.UIRadialMenu", "UIRadialMenu" };

	private static readonly string[] RadialMenuType = new string[3] { "EekUI.RadialMenu", "Il2CppEekUI.RadialMenu", "RadialMenu" };

	private static readonly string[] InventoryUIType = new string[3] { "InventoryUI", "Il2Cpp.InventoryUI", "Il2CppEekUI.InventoryUI" };

	private static readonly string[] ToolTipProviderType = new string[3] { "EekCharacterEngine.Canvas.ToolTipProvider", "Il2CppEekCharacterEngine.Canvas.ToolTipProvider", "ToolTipProvider" };

	private static readonly string[] EekUIButtonType = new string[3] { "EekUI.EekUIButton", "Il2CppEekUI.EekUIButton", "EekUIButton" };

	private static readonly string[] EekUIToggleType = new string[3] { "EekUI.EekUIToggle", "Il2CppEekUI.EekUIToggle", "EekUIToggle" };

	private static readonly string[] EekUISliderType = new string[3] { "EekUI.EekUISlider", "Il2CppEekUI.EekUISlider", "EekUISlider" };

	private static readonly string[] EekUIDropdownType = new string[3] { "EekUI.EekUIDropdown", "Il2CppEekUI.EekUIDropdown", "EekUIDropdown" };

	private static readonly string[] MiniGameManagerType = new string[3] { "EekCharacterEngine.Support.MiniGameManager", "Il2CppEekCharacterEngine.Support.MiniGameManager", "MiniGameManager" };

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
		// A UnityEngine type, safe to name directly (no game-build renames touch it).
		if (PatchCore(new string[1] { "UnityEngine.EventSystems.EventSystem" }, new string[1] { "Update" }, "DriverTick", null, "that per-frame host does not exist in this build", prefix: false))
		{
			DriverHosts++;
		}
		// Game-side managers, for frames where no EventSystem is active.
		if (PatchDriverHost(TransitionalSceneManagerType, "Update"))
		{
			DriverHosts++;
		}
		// EekGamesIntroManager overrides TransitionalSceneManager.Update, so the base
		// patch above does not cover the intro scene; DisclaimerManager declares no
		// Update of its own and is covered by the base.
		if (PatchDriverHost(EekGamesIntroManagerType, "Update"))
		{
			DriverHosts++;
		}
		if (PatchDriverHost(GOGGalaxyManagerType, "Update"))
		{
			DriverHosts++;
		}
		if (PatchDriverHost(GameManagerType, "Update"))
		{
			DriverHosts++;
		}
		if (PatchDriverHost(AudioManagerType, "Update"))
		{
			DriverHosts++;
		}
		if (PatchDriverHost(CutSceneManagerType, "Update"))
		{
			DriverHosts++;
		}
		if (PatchDriverHost(MessageHandlerType, "Update"))
		{
			DriverHosts++;
		}
		// Late phase: the player's own LateUpdate, so Navigator's facing write happens
		// after the game has finished moving the player for this frame. Verified in
		// BepInEx\interop metadata: PlayerCharacter.LateUpdate is the most derived
		// LateUpdate in the player chain - HousePartyPlayerCharacter, BobbyCharacter
		// and FemalePlayerCharacter declare none - so this covers both playable
		// characters.
		if (PatchDriverHostLate(PlayerCharacterType, "LateUpdate"))
		{
			DriverLateHosts++;
		}
		Log.Info($"Driver hosts: {DriverHosts} update, {DriverLateHosts} late.");
	}

	private static readonly string[] TransitionalSceneManagerType = new string[3] { "TransitionalSceneManager", "Il2Cpp.TransitionalSceneManager", "Il2CppEekCharacterEngine.TransitionalSceneManager" };

	private static readonly string[] EekGamesIntroManagerType = new string[3] { "EekGamesIntroManager", "Il2Cpp.EekGamesIntroManager", "Il2CppEekCharacterEngine.EekGamesIntroManager" };

	private static readonly string[] GOGGalaxyManagerType = new string[3] { "GOGGalaxyManager", "Il2Cpp.GOGGalaxyManager", "Il2CppHouseParty.GOGGalaxyManager" };

	private static readonly string[] GameManagerType = new string[3] { "EekCharacterEngine.GameManager", "Il2CppEekCharacterEngine.GameManager", "GameManager" };

	private static readonly string[] AudioManagerType = new string[3] { "EekCharacterEngine.AudioManager", "Il2CppEekCharacterEngine.AudioManager", "AudioManager" };

	private static readonly string[] CutSceneManagerType = new string[3] { "CutSceneManager", "Il2Cpp.CutSceneManager", "Il2CppEekCharacterEngine.CutSceneManager" };

	private static readonly string[] PlayerCharacterType = new string[3] { "EekCharacterEngine.PlayerCharacter", "Il2CppEekCharacterEngine.PlayerCharacter", "PlayerCharacter" };

	private static bool PatchDriverHost(string[] typeNames, string methodName)
	{
		return PatchCore(typeNames, new string[1] { methodName }, "DriverTick", null, "that per-frame host does not exist in this build", prefix: false);
	}

	private static bool PatchDriverHostLate(string[] typeNames, string methodName)
	{
		return PatchCore(typeNames, new string[1] { methodName }, "DriverLateTick", null, "the late phase falls back to the update phase", prefix: false);
	}

	private static void DriverTick()
	{
		Guard(PumpAction);
	}

	private static void DriverLateTick()
	{
		Guard(PumpLateAction);
	}

	private static void PatchNamed(string[] typeNames, string methodName, string postfixName)
	{
		PatchCore(typeNames, new string[1] { methodName }, postfixName, null, "that announcement is disabled", prefix: false);
	}

	private static void PatchNamed(string[] typeNames, string methodName, string postfixName, Type[] paramTypes, bool prefix = false)
	{
		PatchCore(typeNames, new string[1] { methodName }, postfixName, paramTypes, "that announcement is disabled", prefix);
	}

	/// <summary>
	/// Resolves the target type by name, tries each candidate method name in order,
	/// and installs the hook. Every failure mode - type missing, method missing,
	/// Harmony refusing the patch - is caught here and costs one warning, never the
	/// rest of Apply.
	/// </summary>
	private static bool PatchCore(string[] typeNames, string[] methodNames, string postfixName, Type[] paramTypes, string skipNote, bool prefix)
	{
		Type target = null;
		string[] array = typeNames;
		foreach (string text in array)
		{
			target = GameType.Of(text);
			if (target != null)
			{
				break;
			}
		}
		if (target == null)
		{
			Failed++;
			Record(postfixName, typeNames, methodNames, ok: false, "type not present in this game build");
			Log.Warn("Harmony: " + typeNames[0] + "." + methodNames[0] + " skipped: the type is not present in this game build; " + skipNote + ".");
			return false;
		}
		string[] array2 = methodNames;
		foreach (string methodName in array2)
		{
			MethodInfo methodInfo;
			try
			{
				methodInfo = ((paramTypes == null) ? AccessTools.Method(target, methodName, (Type[])null, (Type[])null) : AccessTools.Method(target, methodName, paramTypes, (Type[])null));
			}
			catch (Exception ex)
			{
				methodInfo = null;
				Log.Warn("Harmony: looking up " + target.Name + "." + methodName + " threw: " + ex.Message);
			}
			if (methodInfo == null)
			{
				continue;
			}
			try
			{
				MethodInfo method = typeof(Patches).GetMethod(postfixName, BindingFlags.Static | BindingFlags.NonPublic);
				HarmonyMethod value = new HarmonyMethod(method);
				if (prefix)
				{
					_harmony.Patch(methodInfo, prefix: value);
				}
				else
				{
					_harmony.Patch(methodInfo, postfix: value);
				}
				Applied++;
				Record(postfixName, typeNames, methodNames, ok: true, "patched " + target.FullName + "." + methodName);
				Log.Debug("Harmony: patched " + target.Name + "." + methodName);
				return true;
			}
			catch (Exception ex2)
			{
				Failed++;
				Record(postfixName, typeNames, methodNames, ok: false, "patching " + target.FullName + "." + methodName + " threw: " + ex2.Message);
				Log.Warn("Harmony: could not patch " + target.Name + "." + methodName + ": " + ex2.Message);
				return false;
			}
		}
		Failed++;
		Record(postfixName, typeNames, methodNames, ok: false, "none of the candidate method names exist on the type; " + skipNote);
		Log.Warn("Harmony: " + target.Name + "." + methodNames[0] + " not found; " + skipNote + ".");
		return false;
	}

	/// <summary>
	/// Casts a patched method's instance to a concrete interop type INSIDE a patch
	/// body, so a missing game type costs only this feature. Never use game types in
	/// a patch body's parameter list: the JIT needs them to compile the method itself.
	/// </summary>
	private static T Cast<T>(Il2CppObjectBase o) where T : Il2CppObjectBase
	{
		try
		{
			return o?.TryCast<T>();
		}
		catch
		{
			return null;
		}
	}

	private static void OnDialogueStart(Il2CppObjectBase __0)
	{
		Guard(delegate
		{
			string text = (_lastSpeaker = GameRefs.NameOf(Cast<CharacterBase>(__0)));
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

	private static void OnRadialOpened(Il2CppObjectBase __instance, string __0)
	{
		Guard(delegate
		{
			RadialBridge.NotifyOpened(Cast<UIRadialMenu>(__instance), __0);
		});
	}

	private static void OnWheelOpened(Il2CppObjectBase __instance, string __0, object __1)
	{
		FirstCall("RadialMenu.SetInteractions");
		Guard(delegate
		{
			// The wheel's own option labels arrive as the SetInteractions argument on
			// builds where the stripped tuple field is renamed; hand them to the
			// bridge BEFORE the instance cast so they survive even if the cast fails.
			WheelBridge.NotifyLabelsFromSetInteractions(__1);
			RadialMenu menu = Cast<RadialMenu>(__instance);
			if (menu != null)
			{
				InteractiveItem item = Cpp.Read(() => menu.PGLCMOFAEMF);
				WheelBridge.NotifyOpened(menu, __0, item);
			}
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
	private static void GuardWheelChoose(Il2CppObjectBase __instance, int __0, ref bool __runOriginal)
	{
		// Inline try/catch rather than Guard(...): the decision to block lives in the
		// ref parameter, which a closure cannot capture. If anything here throws, the
		// default __runOriginal stays true, so a guard bug can never freeze the wheel
		// - the game's own call goes through.
		try
		{
			RadialMenu menu = Cast<RadialMenu>(__instance);
			if (menu == null)
			{
				// The wheel type (or one of its members) is not readable on this game
				// build; without a guard we must not block the game's own choice.
				return;
			}
			int tupleCount = Cpp.CountOf<Il2CppSystem.ValueTuple<string, bool>>(Cpp.Read(() => menu.BHNCIKDJNOO));
			int buttonCount = Cpp.CountOf<UnityEngine.UI.Button>(Cpp.Read(() => menu.FAIFDGOFNIA));
			int slotCount = Cpp.CountOf<GameObject>(Cpp.Read(() => menu.CBHGENOCLOF));
			int bound = Mathf.Min(tupleCount, Mathf.Min(buttonCount, slotCount));
			string sig = __0 + " of " + tupleCount + " options, " + buttonCount + " buttons, " + slotCount + " slots";
			float unscaledTime = Time.unscaledTime;
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
			// The mod announced this wheel from the SetInteractions labels (fallback
			// mode, the game's collections are not readable); the game's own collections
			// being empty is expected there, so do not block the player's choice.
			if (tupleCount == 0 && buttonCount == 0 && slotCount == 0 && WheelBridge.FallbackMode)
			{
				Log.Info("Wheel choose passed the guard (label fallback): " + sig + ".");
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
		catch (Exception ex)
		{
			try
			{
				Log.Warn("Wheel guard threw: " + ex.Message);
			}
			catch
			{
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

	private static void OnTamper(Il2CppObjectBase __instance)
	{
		FirstCall("Thermostat.Tamper");
		Guard(delegate
		{
			Thermostats.NotifyTampered(Cast<Thermostat>(__instance));
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

	private static void OnButtonCheckSelect(Il2CppObjectBase __instance)
	{
		FirstCall("EekUIButton.CheckSelect");
		AnnounceWidgetInstance(__instance);
	}

	private static void OnToggleCheckSelect(Il2CppObjectBase __instance)
	{
		FirstCall("EekUIToggle.CheckSelect");
		AnnounceWidgetInstance(__instance);
	}

	private static void OnSliderCheckSelect(Il2CppObjectBase __instance)
	{
		FirstCall("EekUISlider.CheckSelect");
		AnnounceWidgetInstance(__instance);
	}

	private static void OnDropdownCheckSelect(Il2CppObjectBase __instance)
	{
		FirstCall("EekUIDropdown.CheckSelect");
		AnnounceWidgetInstance(__instance);
	}

	private static void AnnounceWidgetInstance(Il2CppObjectBase __instance)
	{
		Guard(delegate
		{
			Component component = Cast<Component>(__instance);
			if (component == null)
			{
				return;
			}
			GameObject gameObject = Cpp.Read(() => component.gameObject);
			bool flag = true;
			// The interop property name of the widget's "currently selected" flag is
			// build-specific; a renamed field must not stop the announcement.
			try
			{
				PropertyInfo property = component.GetType().GetProperty("DFNIODNIFOF");
				if (property != null)
				{
					flag = Cpp.Read(() => (bool)property.GetValue(component), fallback: true);
				}
			}
			catch
			{
			}
			AnnounceWidget(gameObject, flag);
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

	private static void OnToolTipChanged(Il2CppObjectBase __0)
	{
		Guard(delegate
		{
			LoadingBridge.OnToolTipChanged(Cast<ToolTipAssociate>(__0));
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
