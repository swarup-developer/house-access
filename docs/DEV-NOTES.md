# House Access — rebuild for House Party v1.1.7+

These are the development notes: what was found in the game, what was changed to
match it, and how each claim was verified. The player-facing documents are
`../README.md` and `../CHANGELOG.md`.

## What this is

House Access is a community screen-reader mod that makes House Party playable
without sight. This folder contains a **recompiled** version of the mod that is
compatible with current House Party builds (the ones where the game is shipped
as a single Il2Cpp assembly with MelonLoader 0.6.x on .NET 6).

The previous release of the mod could not start on these builds: it was
compiled against an older game layout (five separate Il2Cpp game assemblies and
MelonLoader 0.7.x), so the loader reported missing dependencies and the mod
never initialised. This rebuild keeps the same feature set and voice
announcements, but talks to the game's *current* internals.

## Changes in this build

- **Retargeted the whole mod** to the modern single `Assembly-CSharp` interop
  assembly. References now resolve, and the mod loads and registers.
- **Dialogue, thoughts and pop-ups** are read from the real hooks this game
  build exposes (its dialogue UI methods, message/thought handlers, pop-up
  display). Speaker names, reply lists, chosen responses and "conversation
  over" prompts behave as before.
- **Radial menus and interaction wheels** (verbs on people and objects) are
  announced from the live menus.
- **Room, door, zone and character scanning** was rewritten around the current
  door/clothing/character model: door locked/unlocked/open/closed, who is in a
  room, posture, talking state, nudity states, and so on.
- **Inventory, held items and use-with** lists now read the current UI and
  support keyboard navigation/selection.
- **Cutscenes** announce start/end with the scene name, cast and length, and
  still read your user-written scene descriptions.
- **Loading screens, hints and tooltips** announce progress, hint text, tooltip
  text, and the "press a key to continue" prompt.
- **Key rebinding screen** announces each action and its binding; the widget
  focus announcements were moved onto the events that this build actually
  raises (selection changes on EekUI buttons, toggles, sliders and dropdowns).
- **Mini-game, thermostat and phone hooks** were retargeted to their current
  entry points.
- **Diagnostics dump** was trimmed of values this build no longer stores
  (bladder/energy internals were removed by the game) and now shows what the
  build does expose.

### Honest limitations (things this build removed from the game)

Some internals no longer exist in current House Party builds, so those
assistive extras are cleanly disabled and say so instead of failing silently:

- Relationship-meter and detailed "opportunity" readouts (game internals gone).
- In-depth appearance/clothing-item lists (only nudity states remain exposed).
- Hair/clothing customization helper for the create-a-character screen.
- The in-game developer console reader.
- Combat assist and phone-screen reading (their backing APIs were stripped).
- Separate "Narrator:" channel (narration now flows through the dialogue
  channel).
- Photo notification announcements.

These are logged at start-up so the current behaviour is always transparent.

## Compatibility

- Works with House Party Il2Cpp builds (GOG, Steam and others ship the same
  modern engine layout) running MelonLoader 0.6.x with .NET 6.
- The mod detects its game references automatically; no per-store files are
  needed.
- Other game files are **never touched**: only
  `Mods/HouseAccess.dll` is replaced.

## Files

- `Mods/HouseAccess.dll` — the mod itself (drop-in replacement).
- `backup/HouseAccess-v1.1.0-original.dll` — the untouched previous release,
  kept for reference.

## Testing

1. Start the game once with the new `HouseAccess.dll` in `Mods`.
2. Check `MelonLoader/Latest.log`:
   - "House Access" should initialise (no missing-dependency errors).
   - The line "Harmony: N patches applied, M skipped" reports how many
     announcements are live (M should be 0 on v1.1.7+).
3. If anything misbehaves, send that log back — every disabled feature is
   labelled in it.

---

## v1.1.2 (BepInEx port) — no more il2cpp class injection (2026-09-05)

Start-up crash fix. The process died with an access violation inside
Il2CppInterop's `GenericMethod_GetMethod_Hook.Hook`; that hook is installed by
`InjectorHelpers.Setup()`, which only runs when something injects a managed
type or enum into il2cpp. Both injectors in the process were removed:

- BepInEx's Unity log listener, via `UnityLogListening = false` in
  `BepInEx\config\BepInEx.cfg` (original kept as
  `BepInEx.cfg.bak-before-unitylog-fix`).
- The mod's own `AddComponent<ModDriver>()` in `HouseAccessMod.Load()`, which
  scanning the compiled plugin showed to be its only injection call.

`ModDriver` (a MonoBehaviour) is now `internal static class Driver` with
frame-guarded `Pump()` / `PumpLate()`, called from Harmony postfixes on eight
per-frame game methods plus `PlayerCharacter.LateUpdate`. Zero hosts patched is
reported in the log as `Driver hosts: 0 update, 0 late` and spoken aloud.

Full detail, verification chain and rebuild instructions: this file and
`../CRASH-FIX-GUIDE.md`. The player-facing notes for the same work are in
`../CHANGELOG.md`, with install and key documentation in `../README.md`.

The MelonLoader build in `src/` is unchanged and still affected — MelonLoader
injects its own support component before any mod runs.

---

## v1.1.3 (BepInEx port) — menus talk properly (2026-09-05)

v1.1.2 boots and plays; the four things that were still wrong in menus were all
reported by ear and each one is fixed at its cause.

- **A changed value was silent.** Values were only read at the moment a control
  took focus, and this game build has no `PreferenceManager.SetInt` to hook
  (the log line "Preference value announcements are not supported on this game
  build" is exactly that), so moving a slider or ticking a box announced
  nothing. `MenuReader` now reads the focused control's value live and speaks it
  once it settles (0.15 s of quiet), so a dragged slider gives one number rather
  than fifty. The announcement bypasses the duplicate filter, so stepping a
  value back to one you just heard is still spoken.
- **Opening a menu could be silent.** Unity raises the highlight event while the
  scene is still assembling its UI, and the mod dropped every such event for the
  first three seconds — which is precisely when a menu opens. That window is now
  one second, and the control is *held* and spoken as soon as the scene settles
  instead of being thrown away.
- **Items read as odd raw names.** The name now comes from the widget's visible
  text, then `labels.txt`, then text the widget only shows in another state,
  then the row it sits in (only when that row holds one control, so no other
  control's caption can be borrowed), and only then from the raw object name.
  `Humanize` also drops layout words — btn, img, txt, panel, holder — unless
  that would leave nothing, so a control really called "Button" still says
  "Button". Anything still nameless is written once to
  `UserData\HouseAccess\unlabeled.txt` as `name = spoken name`, ready to be
  edited and pasted into `labels.txt`.
- **The same item would not repeat.** The "already said that" latch had no time
  limit, so returning to an item was silent. It now only covers half a second,
  which is all it was ever for, and the repeat key (default `Home`) re-reads the
  focused control in menus — the same key every other list in the mod already
  uses, and the target readout still owns it outside menus.

Values are read through the game's own widget references. Verified in
`BepInEx\interop\Assembly-CSharp.dll`: `EekUISlider`, `EekUIToggle`,
`EekUIDropdown` and `EekUIButton` extend `MonoBehaviour`, not `Selectable`, and
each holds the Unity control it drives, so looking only for a `Selectable` on
the widget's own object can find nothing at all.

### Number keys now work the way the help says they do

The spoken key list (default `F1`) has always ended with "a number reads that
item, control and a number picks it". Seven lists did handle digits, but each
one slightly differently, and the sentence was not true anywhere else.

- **One rule, one place.** `ListNumbers` now owns it for all eleven lists:
  dialogue replies, inventory, use-with, the interaction wheel, the radial menu,
  your own actions, hands, the mod's key setup, the game's key rebinding screen,
  the search results and the game's own menus.
- **The keypad counts too**, and `0` is the tenth item rather than nothing.
  Numbers reach the first ten items only; anything past the end now answers
  "only six items here" instead of saying nothing, because a key press met with
  silence is indistinguishable from a mod that has stopped working.
- **Pressing the same number twice re-reads that item** instead of being
  swallowed by the duplicate filter.
- **The game's own menus answer numbers.** A number reads the control at that
  position, top to bottom then left to right, and moves the game's own focus
  there so its arrow keys carry on from where you left off. Control and a number
  presses it by calling the same `OnSubmit` the game's submit key delivers —
  `Button.OnSubmit` presses, `Toggle.OnSubmit` flips, `Dropdown.OnSubmit` opens
  the option list, all resolved in `BepInEx\interop\UnityEngine.UI.dll`. A
  slider or text field declares no submit, so those are focused and left alone,
  and the mod says "focused" rather than claiming it pressed something.
  `ExecuteEvents.Execute` is the obvious way to click anything and is
  deliberately not used: it is a generic il2cpp method taking a managed
  delegate, which is what re-arms the v1.1.2 boot crash.
- **The search box is the one exception.** A bare digit there belongs in the
  name you are typing, so only control and a number picks a match. The help now
  says so, along with the keypad, the tenth item and the ten-item limit.

Only the BepInEx build in `port/` is changed. The MelonLoader sources in `src/`
are untouched, as with every fix since v1.1.2.
