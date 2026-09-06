# House Access — changelog

Newest first. Version 1.1.3 is the current release; 1.1.0 was the last one
published before it.

House Access is developed and played on the GOG build v1.1.7 of House Party,
Windows 64-bit, Unity 2020.3.47f1, with BepInEx 6.0.0-be.785 and
`UnityLogListening = false`.

## 1.1.3 — menus and number keys

**Menus now talk properly.** Four things people reported by ear, each fixed at
its cause rather than papered over.

- A changed value was silent. Moving a slider or ticking a box announced
  nothing, because values were only read at the moment a control took focus. The
  mod now watches the focused control's value and speaks it once it settles, so a
  dragged slider gives you one number instead of fifty, and stepping back to a
  value you just heard is still spoken.
- Opening a menu could be silent. The mod was throwing away the game's focus
  events for the first three seconds after a scene loaded, which is exactly when
  a menu opens. It now waits one second, and holds the first control instead of
  discarding it, so the menu introduces itself as soon as it is ready.
- Items read as odd raw names. Names are now taken from the control's visible
  text first, then your own `labels.txt`, then text the control only shows in
  another state, then the row it sits in, and only then from the internal object
  name — with layout words like btn, img and panel stripped out. Anything still
  nameless is written to `UserData\HouseAccess\unlabeled.txt` so you can name it
  yourself.
- The same item would not repeat. Returning to an item you had just heard was
  silent, because the "already said that" rule had no time limit. It now lasts
  half a second, and Home re-reads whatever control has focus.

**Number keys work everywhere, and work the same way everywhere.** The spoken key
list had always ended with "a number reads that item, control and a number picks
it". That was only true in some lists, and each of those did it slightly
differently.

- One rule now covers eleven lists: dialogue replies, inventory, use-with, the
  interaction wheel, the radial menu, your own actions, your hands, the mod's key
  setup, the game's key rebinding screen, the search results, and the game's own
  menus.
- The keypad counts as well as the number row, and zero is the tenth item rather
  than nothing.
- Numbers reach the first ten items. Press one past the end and the mod tells you
  how many items there are, instead of answering with silence.
- Pressing the same number twice reads that item again.
- In the game's own menus, a number reads the control at that position and moves
  the game's real focus there, so its arrow keys carry on from where you left
  off. Control and a number presses it — buttons press, toggles flip, dropdowns
  open. Sliders and text boxes have nothing to press, so those are focused and
  the mod says "focused" rather than claiming otherwise.
- Your hands changed to match the rule: a bare number used to switch hands
  straight away, and now it reads while Control and a number switches.
- The search box is the deliberate exception, because there a bare number is part
  of the name you are typing. Control and a number picks a match.

**Two keys were promising things they could not do**, and the mod no longer
claims them: punch and block were listed in the F1 help but nothing read those
keys on this build, so they have been dropped from the list. Ctrl+Backspace, for
reading the developer console, wrote a line to the log and said nothing aloud;
it now tells you the console is not readable on this version of the game.

## 1.1.2 — the version that starts

House Access moved from MelonLoader to BepInEx, and the start-up crash that came
with the move was fixed.

The game was dying with an access violation before reaching the menu. The cause
was type injection: both BepInEx's Unity log listener and the mod's own driver
component were registering managed types with the game's IL2CPP runtime, and that
path crashes in this combination of game and loader. Neither does it any more.
The log listener is turned off in `BepInEx.cfg` — which is why the install
instructions insist on `UnityLogListening = false` — and the mod's per-frame
driver no longer needs a component of its own; it rides along on methods the game
already calls every frame.

If you install this mod and the game crashes on start-up, that setting is almost
always the reason.

## Rebuild for current House Party

Never published on its own, but this is where most of the work went, and it is
what makes 1.1.2 and 1.1.3 possible at all.

House Access 1.1.0 could not start on current House Party. It was built when the
game shipped as five separate assemblies under MelonLoader 0.7; the game now
ships as one, under a different runtime, and the loader simply reported missing
dependencies. Every hook in the mod was re-aimed at the game as it is now:

- Dialogue, thoughts and pop-ups, including speaker names, reply lists and the
  "conversation over" prompt.
- Radial menus and interaction wheels.
- Room, door, zone and character scanning — locked and unlocked doors, who is in
  a room, posture, whether someone is talking, and nudity state.
- Inventory, held items and the use-with list, with keyboard navigation.
- Cutscenes, which announce their name, cast and length, and read your own
  descriptions from `cutscenes.txt`.
- Loading screens, hints and tooltips.
- The key rebinding screen.
- The thermostat, the phone and the mini-game entry points.
- The diagnostics dump, trimmed of readings the game no longer keeps.

Some features could not be rebuilt, because the parts of the game they read were
removed. They are switched off deliberately and say so when you use their keys,
rather than failing quietly: relationship meters and detailed opportunity
readouts, clothing and appearance lists, the character customisation screen, the
developer console, fighting, reading the phone screen, a separate narrator voice,
and photo notifications. The README lists these too, and every one of them names
itself in the log at start-up.

## 1.1.0

The last release published before this one. MelonLoader, and older House Party
builds only. It does not start on current versions of the game.

## Upgrading from 1.1.0

This is not a drop-in replacement. 1.1.0 lived in a `Mods` folder under
MelonLoader; 1.1.3 lives in `BepInEx\plugins`. Follow the install steps in the
README from the beginning, including the `UnityLogListening` setting, and remove
the old MelonLoader install if you still have one. Your own `labels.txt`,
`descriptions.txt` and `cutscenes.txt` in `UserData\HouseAccess` carry over
unchanged.

