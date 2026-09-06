# Contributing to House Access

These notes are stricter than most projects' and deliberately so. Read them
before writing code; a patch that ignores them will be turned down even if it
works.

## What this project actually is

House Access is a screen reader mod. Its users are blind, and they cannot look at
the screen to check whether the mod did what it claimed. That single fact drives
every rule below.

The consequence worth internalising: **silence is indistinguishable from a broken
mod.** If a key is bound, it must speak — even if all it has to say is that the
feature does not exist on this build. A key press met with nothing tells the user
their mod has died, and they have no way to find out otherwise. Several of this
project's past bugs were exactly that, and one of them was a key that had been
promised in the spoken help for two releases while no code read it at all.

The second consequence: this mod sits on top of a large, obfuscated,
undocumented, commercial game. Nobody here can see the game's source. So claims
about the game are verified against its metadata before they are written into
code, not inferred from what would be reasonable.

## What must never be committed

The repository root sits inside a House Party installation, which makes a
careless `git add` genuinely dangerous. Never commit:

- Decompiled source of any kind, whether from the game or from an earlier build
  of this mod. The `decompiled/` folder exists locally and stays local.
- Anything dumped or extracted from the game: type listings, member listings,
  metadata dumps, process dumps, the `game_dump/` folder, `all_types.txt`,
  `members_*.txt`, `imports.txt`.
- Compiled output of any kind. No `.dll`, no `.pdb`, no `.exe`, no `bin/`, no
  `obj/`. The mod's own DLL is a release download, never a commit.
- Game files, game assets, or any part of a BepInEx, MelonLoader or `UserData`
  folder.
- Logs, local config, or anything containing a `C:\Users\` path.

Two mechanisms enforce this. `.gitignore` denies everything at the top level and
re-admits named entries one line at a time, so a new file is untracked until
somebody deliberately admits it. `.githooks/pre-commit` then inspects what is
staged and refuses the commit outright, which catches the case `.gitignore`
cannot: a file staged with `git add -f`.

Enable the hook once per clone, because git does not ship hooks with a
repository:

    git config core.hooksPath .githooks

Verify it took with `git config --get core.hooksPath`, which must print
`.githooks`. Do not commit with `--no-verify` to get around a refusal; fix what
it is refusing.

## Setting up to build

You need your own legal copy of House Party. Nothing here ships game files, and
nothing here can be built without them.

- The project targets `net6.0`, C# 10, and builds against BepInEx 6 for IL2CPP,
  x64. Build `6.0.0-be.785` is the one the mod is developed and played on.
- `port/HouseAccess.csproj` carries 126 assembly references: four from
  `$(BepInExRoot)\core` and 122 from `$(BepInExRoot)\interop`. Those interop
  assemblies are generated on your machine, from your copy of the game, the first
  time you run it under BepInEx. They are not in this repository and must not be
  added to it.
- The project resolves `BepInExRoot` at build time. It scans the fixed drives
  for a folder containing `HouseParty.exe` with `BepInEx\core` and
  `BepInEx\interop` next to it and compiles against that; if none is found it
  falls back to the copy stashed in the repository's own `tmp\BepInEx788-aside`
  folder. A specific install can still be forced:
  `dotnet build port\HouseAccess.csproj -p:BepInExRoot="<House Party>\BepInEx"`.
- Set `UnityLogListening = false` under `[Logging]` in
  `BepInEx\config\BepInEx.cfg` before you run the game. This is not a preference.
  With it left on, the game dies with an access violation during start-up, before
  the mod loads.

Build and deploy:

    dotnet build port\HouseAccess.csproj -c Release
    copy port\bin\Release\HouseAccess.dll "<House Party>\BepInEx\plugins\"

Then check which build actually loaded, because an old DLL left in `plugins` is
indistinguishable from a mod ignoring your keys. `BepInEx\LogOutput.log` names it
near the top as `Loading [House Access <version>]`.

## Rules that decide whether a patch is accepted

**Every bound key speaks.** If a feature cannot work on the current game build,
say so out loud through `Speaker`. Look at how `CombatBridge`, `CustomizeBridge`
and `ConsoleReader` answer their own disabled keys and follow that pattern. Never
write a key handler whose only output is a log line.

**Never inject a managed type into the IL2CPP runtime.** This is the hardest rule
in the project, and violating it does not produce a nice exception — it produces
an access violation during start-up that took a long time to diagnose. In
practice that means: no `AddComponent<T>` of a managed type, no
`StartCoroutine`, no `ClassInjector` registration, and no passing a managed
delegate or `IEnumerable` into an IL2CPP generic. `ExecuteEvents.Execute<T>()` is
forbidden for that last reason; call `OnSubmit(new BaseEventData(...))` on the
concrete control instead. The per-frame driver deliberately rides along on
methods the game already calls rather than owning a component.

**Verify against the game, do not reason about it.** Before writing code that
touches a game class, method or field, confirm it exists in that build's
metadata. If the search comes back empty or ambiguous, stop and say so in the
issue or pull request. Do not fill the gap with a plausible assumption; a
plausible assumption that is wrong reaches the user as silence. Where the game's
metadata cannot answer the question — timing, dynamic state, what the engine does
between two hooks — add a debug-gated probe and capture a real log instead of
guessing.

**Read what the game already computed; never recompute it.** If the game holds a
value, read the field. Reimplementing its formula duplicates game logic in the
mod and drifts on the next patch. Cache references, never values: a cached value
becomes a stale announcement, which is the screen reader confidently reading
yesterday's state.

**Workarounds need justifying before they are written.** Before adding a
try-catch that swallows, a null-fallback that masks, a retry or wait hack, a
parallel reimplementation of game logic, or a hardcoded magic value: state the
clean solution that uses the game directly, and if you cannot find one, say which
clean paths you considered and exactly why each is blocked. Ask in the issue
first. If it ships, mark it in code as `// WORKAROUND: <why the clean path
failed>`. A workaround without that trail is treated as a bug.

Try-catch is for reflection and for external calls that can genuinely change
underneath us — the speech backends, and game APIs. Ordinary code uses
null-checks, and a null that matters is logged and announced, never swallowed
quietly.

**Respect the game's own controls.** A key the mod does not use is passed through
untouched. Do not take over a key the game might want, and do not talk over the
game. Two keys were removed from the spoken help rather than wired up for exactly
this reason.

**Work with the game's systems, not around them.** Feature parity with a sighted
player is the target: the same information, the same reach, the same uncertainty.
Fog of war, undiscovered rooms, missing tooltips and the game's own lies are game
logic and stay hidden. Building a parallel UI is a last resort for when the game
offers no usable equivalent, and cheating on the player's behalf is not a fix.

**One rule, in one place.** Behaviour that repeats across lists lives in a shared
helper. Number-key handling is the worked example: eleven lists route through
`ListNumbers`, which also owns the wording of "Only N items here." Adding a
twelfth list means calling that helper, not writing digits locally. Anything
spoken to the user is phrased once, where it can be changed once.

## Style

The existing code is the reference; match it rather than the conventions you
prefer.

- Handler and bridge classes are named `<Feature>Bridge` or `<Feature>Handler`,
  private fields are `_camelCase`, and tabs indent.
- Comments and log messages are in English. Comments explain why, especially
  where they record something learned about the game the hard way — those
  comments are the project's memory and are worth more than the code around them.
- Public members carry an XML `<summary>`. Private ones only when they are not
  obvious.
- Spoken strings are plain and short. Priority goes through `Pri`; remember that
  anything below `Pri.Critical` is dropped as a duplicate if it repeats inside the
  dedupe window, which is what `SayNow` exists to bypass.

## Testing, and what a pull request needs to say

There is no automated test suite, because everything worth testing here is
whether a human hears the right thing. So the pull request carries the evidence
instead:

- The game build you tested against, and the `Loading [House Access <version>]`
  line from `BepInEx\LogOutput.log` proving the DLL you tested was the one you
  built.
- Which screen reader you used. "It logged the right text" is not a test; the
  text has to have been spoken.
- For anything touching the game's own menus, dialogue, or a list: the keys you
  pressed and what you heard, in order.
- Where you verified any new claim about the game.

Keep pull requests to one subject. A patch that fixes a menu bug and also
reformats three files will be asked to split.

## Not accepted

- Features that need game files, assets or decompiled code to be committed.
- Cheats, unfair advantages, or anything that hands the player information a
  sighted player would not have.
- A silent `catch` block, or a null-check whose only branch is `return`.
- New spoken strings duplicated across files instead of shared.
- Reformatting passes, renaming passes, or dependency bumps bundled with
  behaviour changes.
- Anything that reintroduces managed type injection, however convenient.

## Provenance

This tree is a rebuild of a mod whose original author is unknown and whose
licence does not exist. `NOTICE.md` sets out exactly what is inherited, what was
rewritten, and why no licence file is offered. Read it before forking or reusing
anything, and read it before assuming you may relicense your own contribution
here.



