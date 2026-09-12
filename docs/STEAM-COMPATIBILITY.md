# Steam compatibility diagnosis — 1.1.14 through 1.1.16

The supplied BepInEx log shows that House Access 1.1.13 loaded successfully,
selected NVDA, and installed eight update hosts plus one late-update host. The
failure happened inside the mod's per-frame code, including `MenuReader` and
`GameRefs`, rather than in speech initialization or driver installation.

The installed game is Steam build 25130746, running Unity 2022.3.62f2 with
BepInEx 6.0.0-be.788. The old mod expected types such as `EekUI.DialogueUI`,
`EekCharacterEngine.Character`, and `CanvasBase` in `Assembly-CSharp.dll`.
The installed game places them in `EekUI.dll` and `EekCharacterEngine.dll`.
Other affected types live in `HouseParty.dll` and `EekEvents.dll`.

The name-based patch lookup introduced in 1.1.13 could find those types, but it
could not repair assembly references compiled into bridge methods. Consequently,
the driver ran and repeatedly threw `TypeLoadException`, preventing both menu
speech and key handling. The log also recorded missing dialogue and tooltip hooks
whose names came from an older obfuscated interop build.

## Changes

- Resolve the local BepInEx installation before discovering interop references.
  Include the separate game assemblies and reject old single-assembly snapshots.
- Replace stale member names throughout the affected bridges with members checked
  in the installed interop metadata, including native controls, response buttons,
  inventory display items, wheel options, phone photos, and loading state.
- Hook `DialogueUI.OnNewDialogueText`, `HandleDialogueStart`, `OnNewResponses`,
  `OnResponseSelect`, and `ToolTipProvider.ToolTipChanged`. The EekUI widget types
  are in the global namespace despite belonging to the EekUI assembly.
- Read native canvas visibility instead of the manager object's active state.
  Clear dialogue ownership after its canvas closes, including when replies remain
  queued. Read the actual loading continue-prompt object.
- Read thought/message text from argument one; argument zero contains an internal
  action or event ID.
- Report recurring bridge exceptions at most once per ten seconds per unchanged
  failure, retaining full stack traces and a suppressed-repeat count. Calls still
  run each frame, and recovery clears the suppression state.

## Verification and limits

The source was compiled in Release using automatic installation detection: zero
errors and five existing unused-field/Windows-platform warnings. Static inspection
resolved all 47 game type references and 138 game member references in the new DLL.
All 33 hook targets and their consumed argument types matched the installed metadata.
Inspection used ILSpy's type listing and Mono.Cecil, without loading game code.

An isolated check of the error guard passed retry behavior, duplicate suppression,
repeat counts, recovery, changed failures, and independent bridge failures.

In a subsequent user-run test on September 11, the user confirmed that the menu
now reads. That run's BepInEx log shows House Access 1.1.14 using NVDA, all 33
patches applied with zero skipped, and zero errors or warnings. It also records
the first `EekUIButton.OnSelect` callback. This confirms main-menu speech, not
complete gameplay or interaction coverage.

A later gameplay log recorded the intro cutscene starting and ending, and the
first `RadialMenu.SetInteractions` callback, with no error entries. It did contain
two auto-walk warnings: the player remained 18.2 metres from the target while
using warp and frame movement, so the navigator switched to direct controller
movement. The code waits about 1.6 seconds without progress before each fallback.
The user then confirmed that auto-walk moved after the pause, establishing that
the direct-controller fallback worked. Version 1.1.15 makes that same movement
path the default. The user's installed configuration was also changed from
`MoveMode = warp` to `MoveMode = direct`, since a new default does not override
an existing saved preference. The revised startup selection has not yet been
tested in-game; the movement implementation itself is unchanged.

The agent did not launch the game, simulate input, or change focus. Existing
disabled helpers were not reimplemented as part of this fix. Older GOG 1.1.7
builds require a separate target.

## Audio and action wheels in 1.1.16

The user reported that Audio settings could not be opened or navigated by keyboard,
and that wheel actions were unavailable while the interaction report said ready.
The 1.1.15 log recorded slider, toggle, dropdown, and wheel callbacks without errors.

Offline inspection of the installed native methods, mapped from the loader's
method-address database to interop metadata, established these contracts:

- `AudioSettings.Toggle` selects its `_music` slider only when
  `PlayerControlManager.IsControllerInput()` is true. The bridge now focuses that
  same control for keyboard use, connects the native controls vertically, and
  restores the original navigation and opening control on close. Native slider
  movement and value-change handlers still perform all volume changes.
- Both wheel `OnChoose` methods subtract one from their argument. The mod and
  its guard previously treated the argument as a zero-based index. Dispatch now
  uses the source slot plus one, with bounds matching the native option/button
  checks. Decorative slot objects are not indexed by the native choice handler.
- `RadialMenu.SortOptions` adds unavailable default placeholders to the game's
  current interaction names. The bridge retains the native input-list reference
  for availability and the sorted source slot for dispatch. It no longer freezes
  a tuple flag at opening. A successful `TryChooseOptionIndex` clears the spoken
  wheel; a native refusal leaves it usable.
- Self-wheel button setup is delayed. The bridge waits for `EnableButtons` and
  checks availability live instead of storing the initial disabled state.
- The old interaction report scanned declared `ItemActions` and marked every
  one available. It now calls the game's virtual
  `CalculateCurrentInteractionsFromStory` method, which evaluates the game's own
  criteria and supplies its current interactions.

Twenty-seven isolated regression checks run the actual wheel/settings bridge
source with UI fixtures. They cover native numbering, skipped labels, disabled
and changing actions, rejected choices, delayed self-wheel setup, cancellation,
audio focus, page-local navigation, and restoration after closing/reopening.
These fixtures do not run Unity or validate audible in-game behavior.

Game-derived inspection files, the original DLL backup, build logs, and compiled
output remain local and are excluded from version control.
