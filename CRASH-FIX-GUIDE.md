# Fix guide — House Party dies at start-up with an access violation

## Where this stands right now

Two changes are already applied in this game folder:

1. `BepInEx\config\BepInEx.cfg` now has `UnityLogListening = false`
   (the original file is kept as `BepInEx.cfg.bak-before-unitylog-fix`).
2. The mod source in `_houseaccess_work\port\` no longer injects a managed
   class into il2cpp — the change is in `HouseAccess\HouseAccessMod.cs` and
   `HouseAccess.Game\Patches.cs`.

What is left for you: rebuild the plugin from `port\`, drop it into
`BepInEx\plugins\`, and start the game. Nothing about a launch can be
confirmed from source alone, so the launch is the test.

## What crashes, and why

The process dies with an access violation (`0xc0000005`) at start-up. The
frame in `BepInEx\ErrorLog.log` is
`Il2CppInterop.Runtime.Injection.GenericMethod_GetMethod_Hook.Hook`, and on
MelonLoader the last log line before death is:

```
[Il2CppInterop] Registered mono type Il2CppInterop.Runtime.DelegateSupport+Il2CppToMonoDelegateReference in il2cpp domain
```

That frame is only reachable if something asked Il2CppInterop to inject a
managed type into il2cpp. Read from the IL of the DLLs shipped in this game
folder:

- `InjectorHelpers.Setup()` is what installs `GenericMethod_GetMethod_Hook`.
  It has exactly four call sites, all inside
  `BepInEx\core\Il2CppInterop.Runtime.dll`: `ClassInjector.RegisterTypeInIl2Cpp`,
  `ClassInjector.Dump`, `EnumInjector.RegisterEnumInIl2Cpp` and
  `EnumInjector.InjectEnumValues`. No injection, no hook, no crash frame.
- BepInEx's own boot reached it through
  `IL2CPPChainloader.SetupUnityLogging()` → `IL2CPPUnityLogSource..ctor` →
  `DelegateSupport.ConvertDelegate` → `RegisterTypeInIl2Cpp`. With
  `UnityLogListening = false`, `SetupUnityLogging()` returns before that
  constructor runs.
- After that config change the only remaining injector in the process was the
  mod itself: `HouseAccessMod.Load()` called
  `BasePlugin.AddComponent<ModDriver>()` → `Il2CppUtils.AddComponent` →
  `RegisterTypeInIl2Cpp`. Scanning the compiled `HouseAccess.dll` found that
  call to be the plugin's *only* injection call.
- Harmony itself never injects. A sweep of all 33 assemblies under
  `BepInEx\core`, `plugins` and `patchers` finds only three callers of
  `RegisterTypeInIl2Cpp`: `Il2CppUtils.AddComponent` and the static
  constructors of `Il2CppManagedEnumerable` / `Il2CppManagedEnumerator`.
  `Il2CppInterop.HarmonySupport` only calls
  `ClassInjector.IsManagedTypeInjected`, which is a lock plus a set lookup.

What is *not* verified: the exact native fault inside that hook. Two
explanations fit the evidence (an unguarded read of the generic-method context,
or a detour written over a wrong target address), and telling them apart needs
a native debugger. It does not matter for the fix — if nothing injects,
`Setup()` never runs and the hook is never installed at all.

## The two changes, in plain terms

**1. `UnityLogListening = false`.** BepInEx no longer forwards Unity's own log
messages into `LogOutput.log`. That is the whole cost. The mod's own lines are
unaffected.

**2. The mod no longer injects a MonoBehaviour.** It used to add its own
`ModDriver` component to a game object, which is what dragged
`RegisterTypeInIl2Cpp` into the process. Instead, `Driver.Pump()` is now called
from Harmony postfixes on game methods that Unity already runs every frame:

- `UnityEngine.EventSystems.EventSystem.Update` (menus and play)
- `TransitionalSceneManager.Update` and `EekGamesIntroManager.Update`
  (intro, disclaimer and scene transitions)
- `GOGGalaxyManager.Update`
- `EekCharacterEngine.GameManager.Update`, `AudioManager.Update`,
  `CutSceneManager.Update`, `HouseParty.Interface.MessageHandler.Update`
- late phase: `EekCharacterEngine.PlayerCharacter.LateUpdate`, so the
  "face this direction" write still lands after the game has moved the player

Every one of those methods was confirmed to exist in `BepInEx\interop`
metadata for this game build, and their subclasses were checked so nothing
shadows them. Several are patched on purpose: each is only alive in some
scenes, and the pump ignores repeat calls within the same frame, so overlap
costs nothing. If none of them can be patched the mod says so out loud
("House Access could not start. No update host was found.") instead of going
quiet.

## What to do now

1. Close the game.
2. Build the plugin:
   `dotnet build "D:\game\House Party\_houseaccess_work\port\HouseAccess.csproj" -c Release`
3. Copy `port\bin\Release\HouseAccess.dll` into
   `D:\game\House Party\BepInEx\plugins\`.
4. Start NVDA, then start the game.

## How to tell whether it worked

Spoken, a few seconds after the world loads:

> "House Access ready, using NVDA. Press F1 for keys."

In `BepInEx\LogOutput.log`, these lines are the ones that matter:

- `House Access 1.1.2 (BepInEx build) starting.` — the plugin ran.
- `Driver hosts: N update, M late` — how many per-frame hosts were patched.
  **N must be at least 1.** If it is 0 the mod is inert and says so aloud.
- `Driver: N update host(s), M late host(s).`
- `Harmony: N patches applied, M skipped.` — the spoken announcements. Every
  skipped one is named in a `not found` warning line just above.
- `Speech backend: NVDA` and `[TTS] Loaded native library: <path>`.
- There should be **no** `Registered mono type` line anywhere in the log. If
  one appears, something in the process is still injecting into il2cpp — send
  the log, that line names the culprit.

If the game still dies, send both `BepInEx\LogOutput.log` and
`BepInEx\ErrorLog.log`.

## Why MelonLoader cannot be fixed the same way

On MelonLoader the crash also happened with **no mods at all** ("0 Mods
loaded"), and on 24 and 28 August 2026, before the rebuilt mod existed.
MelonLoader injects its own support-module component during its own start-up,
before any mod is given a chance to run, so there is nothing a mod can change
to avoid it. Use the BepInEx build on this PC.

If MelonLoader was ever installed here, rename `version.dll` in the game folder
to `version.dll.off` — BepInEx and MelonLoader cannot both hook the same game.

## If it still crashes: environment fallbacks

Everything below is an **untested hypothesis**, listed cheapest and safest
first. None of it is needed if the log shows the mod starting normally.

1. **Repair the .NET 6 runtime.** Settings → Apps → Installed apps → "Microsoft
   .NET Runtime 6.0.x (x64)" and "Microsoft .NET 6.0 - Windows Desktop Runtime
   6.0.x (x64)" → ⋮ → Modify → Repair. Installers are at
   `https://dotnet.microsoft.com/en-us/download/dotnet/6.0`.
2. **Exclude the game folder from Windows Defender real-time scanning.**
   Windows Security → Virus & threat protection → Manage settings → Exclusions
   → add `D:\game\House Party`. Freshly generated interop assemblies are
   written and loaded within the same second, which is the kind of thing
   real-time scanning can disturb.
3. **Verify the game files.** GOG Galaxy → House Party → Manage installation →
   Verify / Repair. On Steam: Properties → Installed Files → Verify integrity.
4. **Update the graphics driver** (this PC: NVIDIA GTX 1650, driver 559.99).
   Low likelihood, but cheap.
5. **Windows Memory Integrity (Core isolation / HVCI), last resort.** Turning
   it off has been suggested for crashes like this one, but nothing measured
   here supports it, and it switches off a real Windows security protection for
   the whole machine, not just the game. Only try it if steps 1 to 4 changed
   nothing: Settings → Privacy & Security → Windows Security → Device security
   → Core isolation details → Memory integrity → Off, then reboot. **Turn it
   back on** once you know whether it made a difference.

A plain diagnostic, if you want to prove the loader is the trigger: close the
game, rename `winhttp.dll` in the game folder to `winhttp.dll.off`, start the
game (it runs unmodded), then rename it back. This changes nothing by itself.

## Things not to do

- Do not delete the `Mods` or `backup` folders (the original mod is preserved
  in `_houseaccess_work\backup\`).
- Do not edit or replace any game files — only `BepInEx\plugins\HouseAccess.dll`
  (or `Mods\HouseAccess.dll` on MelonLoader) is ever touched.
