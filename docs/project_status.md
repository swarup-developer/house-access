# Project Status: HouseAccess (House Party Accessibility Mod)

This document tracks the current accessibility status, tested features, open tasks, and architecture of HouseAccess. It is structured according to the Accessibility Mod Template specification.

## Core Metadata
* **Game:** House Party
* **Game Engine:** Unity (IL2CPP) with modern UnityEngine.InputSystem package
* **Mod Loader:** BepInEx 6 (IL2CPP)
* **Architecture:** 64-bit Windows
* **Screen Reader Layer:** Tolk (NVDA, JAWS, SAPI, System Speech)
* **Active Repository:** e:\git-project\_houseaccess_work
* **Target Deploy Path:** D:\game\House Party\BepInEx\plugins\HouseAccess.dll

---

## What Was Worked On Recently
* **Steam-Build Compatibility (v1.1.13):**
  * Fixed the total-mod failure reported by Steam users (TypeLoadException on `EekUI.DialogueUI` → "No per-frame host could be patched").
  * All Harmony patch targets now resolve game types BY NAME at runtime (GameType.Of) with interop namespace fallbacks; a missing type skips only its own hook.
  * Patch bodies take untyped instances (Il2CppObjectBase) and cast inside try/catch, removing the JIT trap where a missing game type in a patch body signature kills the method.
  * Method names are candidate lists (obfuscated + real names) so future dumps can add Steam names without restructuring.
  * WheelBridge label fallback: wheel options come from the SetInteractions labels when the wheel's stripped option field is renamed; the OnChoose guard respects fallback mode.
* **Input System Decoupling (Guide Chapter: Keyboard Navigation Design):**
  * Eliminated all direct calls to legacy UnityEngine.Input.GetKeyDown and Input.GetKey across OpportunityBridge, PhoneBridge, and InventoryBridge.
  * Routed all keyboard input through HouseAccess.InputLayer.Keys (Keys.Down, Keys.Held, Keys.Ctrl, Keys.NumberDown), preventing System.InvalidOperationException crashes caused by Unity's Input System player settings.
* **Phone System Accessibility:**
  * Restored full in-hand phone controller supporting 8 apps (Photos, Messages, Music, Phone, Mail, Facebook, Reddit, Hearthstone).
  * Added digital clock readout, gallery photo stepping, and camera controller feedback.
* **Opportunity & Memories Screen Accessibility:**
  * Fixed canvas visibility tracking on OpportunityWindowManager.
  * Added Tab switching between Opportunities and Memories.
  * Added arrow key and number-key selection, Home repeat, and F2 read-all.
* **Inventory Canvas Detection & Navigation:**
  * Resolved active state bug by checking CanvasBase.Canvas.activeInHierarchy.
  * Connected quick-use display slots and bag inventory items with action picker support.
* **Inspector & Dialogue Speech Hook:**
  * Fixed parameter signature on MessageHandler.OnDisplayMessage (string __1) to speak full inspection/thought text.
  * Integrated TryAnnounceInspector() into F4 status announcement.

---

## Screen & Feature Status Checklist

### 1. Game Menus & UI
* [x] **Main Menu / Title Screen**: Accessible with arrow keys, Enter, and SpeakCurrent.
* [x] **Pause Menu / Settings**: Navigable with sound feedback.
* [x] **Inventory (I / Tab)**: Accessible. Slots and bag items speak names, quantities, and descriptions. Escape closes.
* [x] **Opportunities & Memories (O)**: Accessible. Tab switches between Opportunities and Memories tabs. Arrow keys navigate items.
* [x] **In-Game Phone**: Accessible. Navigates apps, photo viewer, messages, and calls.
* [x] **Inspection / Message Popups**: Accessible. Full popup/thought text announced on trigger and via F4.
* [x] **Dialogue Wheel / Conversations**: Accessible via WheelBridge with number key selection.
* [x] **Action Picker**: Accessible with Up/Down and Enter to execute item actions.

### 2. 3D World Navigation & Sonification
* [x] **Object Sonification / Radar**: Beeps and pitch cues for interactables in reach/view.
* [x] **Character Sonification / Proximity**: Tracks NPCs with directional audio and status info.
* [x] **Waypoint / Pathfinding (Navigator)**: Routes player through door thresholds and waypoints.
* [x] **Target Inspection (F4)**: Announces character mood, state, clothing, and open inspector windows.

---

## Keybindings Reference for Screen Reader Users
* **Tab**: Standard UI navigation / switch tabs in Opportunities and Memories.
* **Arrows (Up/Down/Left/Right)**: Navigate items, menus, and phone apps.
* **Enter / Return**: Confirm / activate selected item or app.
* **Escape**: Cancel / close current menu or screen.
* **1 to 9**: Direct select item / dialogue choice; Ctrl + 1..9 opens phone app.
* **Home**: Repeat current item / target description.
* **F1**: Help and keybind announcement.
* **F2**: Read entire screen contents / all entries.
* **F4**: Target status inspection and active inspector readout.
* **F5**: Quick Save.
* **F9**: Quick Load.

---

## Known Gotchas & Architecture Rules
* **Unity Input System:** House Party uses UnityEngine.InputSystem. NEVER call UnityEngine.Input.GetKeyDown() or UnityEngine.Input.GetKey(). ALWAYS use HouseAccess.InputLayer.Keys.Down() or Keys.Held().
* **Canvas Active Checking:** In House Party UI, CanvasBase.gameObject.activeInHierarchy remains true while menus are closed. Always check CanvasBase.Canvas.activeInHierarchy or IsShowing.
* **String Message Hooks:** In Harmony patches for MessageHandler.OnDisplayMessage, the second parameter is string __1 (the message content). Omitting it silently loses message bodies.
* **Output Formatting:** For user communications, strictly avoid tables. Present information using structured lists.
