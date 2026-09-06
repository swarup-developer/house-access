# House Access

Welcome to **House Access**, a screen reader accessibility mod crafted for **House Party**.

We believe gaming should be accessible to everyone. House Party is packed with hilarious dialogue, wild branching storylines, eccentric party guests, and chaotic social puzzles—and you shouldn't have to see the screen to dive right into the party, explore every room, chat up the guests, and see every questline through to the end.

House Access connects directly to your favorite screen reader (NVDA, JAWS via UniversalSpeech, ZDSR, or standard Windows SAPI) to speak menus, conversations, nearby objects, and your inventory out loud. It also gives you spatial navigation tools—like an audio beacon, room radar, and target tracking—so you can navigate the party with total confidence.

This is version **1.1.3**, retargeted and tested on current House Party (specifically tested on the GOG 64-bit v1.1.7 build, Unity 2020.3.47f1, powered by BepInEx 6.0.0-be.785).

---

## Quick Summary of What You Need

1. **House Party for Windows (64-bit)**: A current, modern build of the game.
2. **BepInEx 6 (IL2CPP x64)**: Build `6.0.0-be.785` is the version House Access is built and tested against. (Note: the older BepInEx 5 will not work with this game).
3. **A Screen Reader**:
   - **NVDA** (make sure `nvdaControllerClient.dll` is in your game directory).
   - **JAWS, SuperNova, System Access** (supported through `UniversalSpeech.dll`).
   - **ZDSR** (through `ZDSRAPI.dll`).
   - **Windows SAPI**: If no screen reader is detected, Windows SAPI will speak automatically as a fallback.

---

## Getting Started & Installation

Installing mods can sometimes feel tricky, especially with IL2CPP games. We've streamlined the steps below to be as straightforward as possible.

### Step 1: Install BepInEx 6
Download **BepInEx 6 for IL2CPP x64** and extract its contents into your main House Party game directory (where `HouseParty.exe` lives). When extracted correctly, you should see a `BepInEx` folder and a `winhttp.dll` sitting right next to `HouseParty.exe`.

### Step 2: Run the Game Once to Generate Files
Start `HouseParty.exe` once and wait a minute or two. On this very first run, BepInEx analyzes the game and generates its internal interop assemblies. It might look like nothing is happening for a little bit—that is completely normal! Once the main menu appears, close the game.

### Step 3: The One-Click BepInEx Config Fix (Important!)

> [!IMPORTANT]
> **A heartfelt heads-up about BepInEx:**  
> BepInEx 6 has a known start-up bug in its Unity logger. If `UnityLogListening` is left enabled, BepInEx crashes before any mod or screen reader can even initialize.  
> Because the crash happens at the engine level before House Access even wakes up, the mod cannot intercept it or speak an apology out loud to tell you what went wrong. The game would simply disappear without a sound.  
>
> We don't want anyone struggling with text editors or getting stuck, so we made this effortless:

#### Option A (Easiest — 1-Click Script):
In the `release` folder, we've provided a helper script called **`fix-bepinex-config.bat`**.
Copy `fix-bepinex-config.bat` into your House Party folder (next to `HouseParty.exe`) and double-click it (or press Enter on it). It will instantly set `UnityLogListening = false` for you and tell you it's done!

#### Option B (Manual):
If you prefer doing it by hand:
1. Open `BepInEx\config\BepInEx.cfg` in Notepad.
2. Find the `[Logging]` section.
3. Find the line `UnityLogListening = true` and change it to:
   ```ini
   UnityLogListening = false
   ```
4. Save and close Notepad.

### Step 4: Add House Access and Speech Libraries
1. Copy **`HouseAccess.dll`** into the `BepInEx\plugins` folder.
2. Place the speech helper DLLs (`UniversalSpeech.dll`, `nvdaControllerClient.dll`, and `ZDSRAPI.dll` if needed) right next to `HouseParty.exe` (or inside a folder named `universal speech` beside the game).
   *(Note for NVDA users: `nvdaControllerClient.dll` must be present next to `HouseParty.exe` for NVDA to receive speech!)*

### Step 5: Start the Party!
Make sure your screen reader is running, and launch House Party! Within seconds, you should hear House Access announce itself, and you can freely explore the menus using your arrow keys.

---

## How to Play & Default Controls

You can rebind every single key at any time by pressing **Ctrl + K** in-game, or by opening `BepInEx\config\HouseAccess.HouseAccess.cfg`.

Press **F1** at any time while playing for an in-game spoken list of all available keys.

### Interacting & Targeting
The mod keeps a smart cursor on nearby party guests and objects:
- **Ctrl + Page Down / Ctrl + Page Up**: Cycle to the next or previous target nearby.
- **Page Down / Page Up**: Jump to the next or previous *category* of target (people, items, doors, etc.).
- **Home**: Repeat the name and details of your current target.
- **Equals (`=`)**: Perform the default interaction with the current target (talk, inspect, open).
- **Ctrl + Equals (`Ctrl + =`)**: Open a list of *all* possible actions you can take with this target.
- **Ctrl + End**: Pick up and carry someone.
- **Ctrl + Z**: Search for a person or object in the room by typing their name.

### Moving & Spatial Awareness
- **End**: Auto-walk directly toward your selected target.
- **Ctrl + Home**: Turn in place to face your target directly.
- **J / L**: Turn left and right in small increments (hold **Shift** for a crisp 90-degree quarter turn).
- **Shift + Page Up / Page Down**: Face the next or previous target without walking.
- **Comma (`,`)**: Emergency stop — halts walking and cancels current interaction menus.
- **Ctrl + B**: Toggle the **Audio Beacon**. When turned on, a spatial tone chirps from your target's position, helping you gauge distance and direction naturally by ear.

### Finding Out Where You Are
- **Ctrl + R**: "Where am I?" — speaks your current room and zone.
- **Ctrl + D**: Doorways & exits — lists every way out of your current room.
- **Alt + R**: Room scan — gives you a quick rundown of everything and everyone currently in the room.
- **Ctrl + P**: People nearby — lists who is within chatting distance.
- **Alt + Home**: Look directly ahead and speak what is right in front of you.
- **Ctrl + V**: Who can see me? — very handy when attempting sneaky party mischief!
- **Alt + Equals / Alt + Minus**: Expand or shrink the targeting search radius.

### Player Status, Items & Dialogue
- **F3**: Check your current player status.
- **F2**: Read whatever is currently on the screen.
- **F4**: Detailed description of the targeted person (what they look like, mood, etc.).
- **Ctrl + I**: Open your Inventory.
- **Dash (`-`)**: Open your Hands — hold, drop, or throw items.
- **Ctrl + T**: Focus the text input prompt when the game asks you to type something.

### Quick List Navigation (The Number Keys)
Every list in the game—dialogue choices, inventory items, actions, menus—is designed for speed:
- Use **Up / Down Arrows** to browse and **Enter** to select.
- **1 through 9, and 0**: Press any number key (top row or numpad) to hear that item read out instantly (0 is item 10).
- **Ctrl + [Number]**: Instantly choose/select that numbered item!
- If a list has fewer items than the number you pressed, the mod gently announces how many items are in the list.

### Photography & Camera (When taking photos)
- **Alt + P**: Review photos you've taken.
- **Alt + K**: Switch camera views.
- **Alt + G**: Change shot type.
- **Alt + F**: Frame the subject.
- **Alt + H**: Adjust camera height up or down.

### Mod Utilities
- **Ctrl + K**: In-game key rebinding menu.
- **Ctrl + F1**: Run a speech test (checks your screen reader backend).
- **Ctrl + F12**: Cycle through speech backends on the fly (NVDA -> UniversalSpeech -> SAPI).
- **Ctrl + F3**: Temporarily mute or toggle House Access on/off.
- **Ctrl + F2**: Generate a diagnostics report in `UserData\HouseAccess`.

---

## Making It Your Own: Custom Labels & Descriptions

House Access lets you personalize the game world:
- Look in `UserData\HouseAccess\unlabeled.txt`: If the mod discovers an unlabelled button or object, it quietly logs it here.
- Copy any line from `unlabeled.txt` into `UserData\HouseAccess\labels.txt` and give it whatever friendly name you want (e.g. `rawButtonName = Kitchen Light Switch`).
- You can also add custom room/object descriptions in `descriptions.txt` and cutscene descriptions in `cutscenes.txt`.

---

## Honest Notes on Gameplay Limitations

We believe in complete transparency so you never wonder if something is broken:
Because modern House Party replaced older MelonLoader systems with a single-assembly IL2CPP architecture, some internal variables from the old 1.1.0 mod no longer exist in the game code. Rather than failing silently, the mod will speak aloud to let you know:
- Relationship percentages and deep opportunity meters are not exposed by the current game code.
- Detailed clothing lists (outside of general nudity state) are not accessible.
- Fighting minigames, the developer console, and the in-game phone screen cannot be read at this time.

Everything else—the dialogues, choices, quest items, roaming, chatting, party fun, and branching outcomes—is yours to enjoy.

---

## Troubleshooting

- **Game closes immediately on launch without any sound**:  
  Check Step 3! This almost always means `UnityLogListening` is still set to `true` in `BepInEx\config\BepInEx.cfg`. Run `fix-bepinex-config.bat` or edit the file manually.
- **Game starts, but no voice comes out**:  
  Press **Ctrl + F1** to run the speech diagnostic test, and press **Ctrl + F12** to try switching backends. If you use NVDA, verify that `nvdaControllerClient.dll` is located right next to `HouseParty.exe`.
- **Need to report an issue or get help?**:  
  Turn on `VerboseLog = true` in `BepInEx\config\HouseAccess.HouseAccess.cfg`, press **Ctrl + F2** in-game to write a diagnostic log, and share `BepInEx\LogOutput.log`.

---

## Building From Source

If you want to build House Access yourself:
1. Ensure you have the .NET 6 SDK installed.
2. Clone this repository into `<House Party>\_houseaccess_work`.
3. Open a terminal and run:
   ```shell
   dotnet build port\HouseAccess.csproj -c Release
   ```
4. Copy the compiled `port\bin\Release\HouseAccess.dll` into your `<House Party>\BepInEx\plugins\` folder.

Have a wonderful time at the party!
