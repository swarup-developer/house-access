# House Access — changelog

Newest first. Version 1.1.9 is the current release; 1.1.0 was the last one
published before it.

House Access is developed and played on the GOG build v1.1.7 of House Party,
Windows 64-bit, Unity 2020.3.47f1, with BepInEx 6.0.0-be.785 and
`UnityLogListening = false`.

## 1.1.9 — the wheel can no longer freeze the game

Starting a new game, or moving between conversations, could make House Party
"not responding": the game itself calls the interaction wheel's `OnChoose`
from native code while the wheel is being rebuilt at those transitions, and an
index that no longer exists makes the game throw
`ArgumentOutOfRangeException` from inside its own code — the repeated
`[Error :Il2CppInterop] During invoking native->managed trampoline ...
EekUI.RadialMenu.OnChoose` block in `BepInEx\LogOutput.log` — and freeze.

- A guard now runs before the wheel's `OnChoose`: the index is checked against
  the options the wheel currently offers, and a choice that cannot name an
  option is skipped instead of thrown (one log line per second at most). The
  game's legitimate choices are untouched — the check is read-only and, if the
  options cannot be read, the original call always runs.
- The mod's own wheel choices are double-checked at the moment they are made:
  if the wheel was repopulated since its options were announced, the mod says
  "that option is no longer available" instead of passing on a stale index.

## 1.1.8 — replies follow the game's focus

Dialogue replies are plain Unity buttons, so the game's own EventSystem
selection can sit on them. The mod now keeps its reply cursor and the
game's real focus in step in both directions:

- When the mod moves with the arrows (or a number reads a reply), it also
  points the game's selection at that reply, so the game's highlight - and
  any arrow handling of its own - starts from the same reply the mod
  announced.
- When the game selects a reply on its own (its arrow handling, a click, or
  the mouse), the mod follows: the cursor moves there and the reply is read
  out, and the move is logged as "game focus moved to reply N" so the two
  directions can be told apart in `BepInEx\LogOutput.log`.
- The reply list now stays "held" until a reply is chosen or the
  conversation ends, which keeps the generic menu reader quiet about the
  dialogue screen and prevents the same reply from being announced twice.

## 1.1.7 — description template and walk diagnostics

- The character description file (`UserData\HouseAccess\descriptions.txt`) is
  now completed with one blank `Name = ` line per known character when it has
  never been filled in, and the scan is retried once the party has spawned.
  "Loaded 0 character descriptions" was the untouched template, not a failure —
  but you could not tell which names to write; now the file shows them.
- Auto-walk fallback messages now include how far you still are from the target
  when a movement mode gives up ("still 4.2 m away"), so `BepInEx\LogOutput.log`
  can show whether the walk is progressing or truly stuck.

## 1.1.6 — arrow-browse the replies

Dialogue replies can now be walked with the same arrows every other list in
the mod uses: Down and Up move through the replies one at a time, each read
as you land on it ("Get out of my house, 2 of 5"), wrapping at the ends, and
Enter picks the reply you are on. The bindings are the shared KeyUiNext /
KeyUiPrev / KeyUiActivate keys, so rebinding them in the key editor moves
the replies too. Numbers and Control+number still work exactly as before.

Previously the reply list was the one list in the mod that ignored the
arrows its own key documentation promised.

## 1.1.5 — talking, walking and the audio beacon

### Conversation replies read out

Talking to someone left the reply list unspoken: the mod waited twenty
seconds before reading your options — a leftover from a build where it could
detect when the voiced line finished, which this one cannot — so after the
speaker's line you heard silence and the conversation looked dead.

- The reply list is now read out shortly after it appears, right after the
  speaker's line: "5 replies. 1. … 2. …". Numbers still read a single reply and
  Control+number still picks one, and the list stays navigable for the whole
  conversation.
- The conversation is treated as active while replies are pending, held or
  collected, even when the game's own "dialogue open" signal is unreliable, so
  a missed signal can no longer silence the list.
- Dialogue start, reply collection and dialogue end are logged, and a reply
  batch that never appears is now a warning instead of a hidden debug line, so
  a report like this one can be diagnosed from `BepInEx\LogOutput.log`.

### Auto-walk no longer crashes

Pressing End to walk to a target threw a `NullReferenceException` inside the
warp step whenever the walk state had been disturbed (right after a cutscene,
for example), killing the walk and filling the log with errors. The whole
warp step is now guarded and falls back to frame movement instead, and the
log shows a one-line warning rather than a stack trace.

### The audio beacon works again

The Ctrl+B beacon was completely silent: its tones were built by casting a
managed `float[]` to an interop array, which throws `InvalidCastException` on
this runtime. The tones are now built as a real interop array, so the beacon
pings and arrival chime sound again.

## 1.1.4 — builds anywhere

A release whose changes are in the build and the installer, not the gameplay
code: the mod itself is unchanged from 1.1.3.

- `dotnet build port\HouseAccess.csproj` no longer needs the repository to live
  inside the game folder, and no longer needs a `-p:BepInExRoot` switch on this
  machine. The project now finds the game's BepInEx install by itself: it scans
  the fixed drives for a folder containing `HouseParty.exe` with
  `BepInEx\core` and `BepInEx\interop` next to it and compiles against that.
- If no game install is found, it falls back to the copy stashed in the
  repository's `tmp\BepInEx788-aside` folder, so a plain clone still builds.
- A specific install can still be forced with
  `dotnet build -p:BepInExRoot="C:\path\to\BepInEx"`.
- The installer layout is complete again: the game folder ships BepInEx 6 for
  IL2CPP (build 788) with its `dotnet` runtime folder, the interop assemblies,
  and `UnityLogListening = false` in `BepInEx.cfg`. Without that setting the
  game dies with an access violation at engine start-up, before the mod can
  speak — which is what the silent start-ups on this machine were.

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
MelonLoader; 1.1.3 and later live in `BepInEx\plugins`. Follow the install steps in the
README from the beginning, including the `UnityLogListening` setting, and remove
the old MelonLoader install if you still have one. Your own `labels.txt`,
`descriptions.txt` and `cutscenes.txt` in `UserData\HouseAccess` carry over
unchanged.

