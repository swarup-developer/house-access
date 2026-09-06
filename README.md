# House Access

<div align="center">

![Accessibility: Screen Reader Supported](https://img.shields.io/badge/Accessibility-Screen%20Reader%20Ready-success?style=for-the-badge&logo=accessibility)
![Game: House Party](https://img.shields.io/badge/Game-House%20Party-blueviolet?style=for-the-badge&logo=unity)
![Platform: Windows 64--bit](https://img.shields.io/badge/Platform-Windows%20x64-blue?style=for-the-badge&logo=windows)
![Framework: BepInEx 6 IL2CPP](https://img.shields.io/badge/Framework-BepInEx%206%20IL2CPP-orange?style=for-the-badge)

### The Complete Screen Reader Accessibility Mod for *House Party*
*Spoken menus, dialogues, 3D room radar, spatial audio beacons, and full narrative freedom without sight.*

[Game on GOG](https://www.gog.com/en/game/house_party) • [Download BepInEx 6](https://builds.bepinex.dev/projects/bepinex_be) • [Original AudioGames.net Thread](https://forum.audiogames.net/topic/60065/house-party-access-mod-18/) • [Changelog](CHANGELOG.md) • [Installation Guide](#-getting-started--installation)

---
---

### ⚠️ Content Warning (18+ Adult Content)
*House Party* is an explicit, mature-rated 3D comedy adventure game containing strong sexual content, nudity, coarse language, alcohol/substance use, and adult humor. Please ensure you meet the legal age requirements in your region and are comfortable with adult themes before playing.

---


</div>

## Overview

Welcome to **House Access**, an accessibility mod built with care for [**House Party**](https://www.gog.com/en/game/house_party) by Eek! Games.

House Party is a 3D narrative comedy adventure packed with branching choices, eccentric party guests, item combinations, and chaotic social puzzles. **House Access** hooks directly into the game engine to bridge the entire visual experience to your favorite screen reader.

Whether exploring rooms, listening for spatial audio cues to pinpoint guests, chatting through dynamic dialogue branches, or managing your inventory, House Access makes House Party fully playable without sight.

---

## Downloads & Requirements

| Component | Requirement / Recommendation | Link |
| :--- | :--- | :--- |
| **The Game** | **House Party** (Current 64-bit Windows build, e.g., GOG v1.1.7) | [Buy / Download on GOG.com](https://www.gog.com/en/game/house_party) |
| **Modding Framework** | **BepInEx 6 for Unity IL2CPP (x64)** (Build `6.0.0-be.785` tested) | [Download at builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be) • [GitHub](https://github.com/BepInEx/BepInEx) |
| **Primary Screen Readers** | **NVDA** (Free, open-source) or **JAWS / SuperNova** | [NV Access (NVDA)](https://www.nvaccess.org/) |
| **Speech Libraries** | `UniversalSpeech.dll`, `nvdaControllerClient.dll`, or `ZDSRAPI.dll` | [UniversalSpeech GitHub](https://github.com/accessibleapps/UniversalSpeech/releases) |
| **Fallback Audio** | Built-in Windows SAPI (Zero setup required if no screen reader is open) | *Built into Windows* |

---

## 🚀 Getting Started & Installation

Installing mods into modern IL2CPP Unity games can feel daunting. We've simplified the entire setup into a quick, repeatable process.

### Step 1: Install BepInEx 6
1. Download **BepInEx Unity (IL2CPP) for Windows (x64)** from the [BepInEx Bleeding Edge Builds](https://builds.bepinex.dev/projects/bepinex_be).
2. Extract the archive directly into your **House Party game directory** (the folder containing `HouseParty.exe`).
3. When placed correctly, you should see a `BepInEx` folder and `winhttp.dll` sitting side-by-side with `HouseParty.exe`.

### Step 2: First Boot (Generate Interop Assemblies)
1. Launch `HouseParty.exe` once.
2. **Be patient:** On this initial launch, BepInEx unpacks and generates its IL2CPP interop assemblies. It may look like nothing is happening for 1–2 minutes while Unity loads.
3. As soon as you reach the game's main menu, close the game.

### Step 3: Prevent the Start-up Crash (The 1-Click Fix)

> [!IMPORTANT]
> **A heartfelt heads-up about BepInEx 6:**  
> BepInEx 6 has an internal logging issue where `UnityLogListening = true` causes an immediate access violation crash during engine startup.  
> Because this crash occurs at the engine level *before* House Access even has a chance to initialize, **the mod cannot catch it or speak an apology out loud**. The game will simply vanish without sound.  
>  
> To make fixing this effortless, we've provided a simple 1-click helper script:

* **Option A (One-Click Automated Fix - Recommended):**  
  Grab **[`fix-bepinex-config.bat`](fix-bepinex-config.bat)** (also included in our [GitHub Releases](https://github.com/swarup-developer/house-access/releases)), copy it next to `HouseParty.exe`, and run it. It will automatically set `UnityLogListening = false` in your config!
* **Option B (Manual Edit):**  
  Open `BepInEx\config\BepInEx.cfg` in Notepad, locate the `[Logging]` section, and set:
  ```ini
  UnityLogListening = false
  ```
  Save and close the file.

### Step 4: Install House Access & Speech DLLs
1. Copy **`HouseAccess.dll`** into `BepInEx\plugins\`.
2. Place the required speech DLLs (`UniversalSpeech.dll`, `nvdaControllerClient.dll`, and/or `ZDSRAPI.dll`) directly beside `HouseParty.exe` (or inside a folder named `universal speech` in the game root).
   * *Note for NVDA users:* Ensure `nvdaControllerClient.dll` is placed next to `HouseParty.exe` so NVDA receives speech calls.

### Step 5: Start the Party!
1. Turn on your screen reader (NVDA, JAWS, etc.).
2. Launch **House Party**!
3. Within moments, House Access will speak its welcome announcement, and you can navigate the menus freely with your arrow keys.

---

## 🎮 Controls & How to Play

You can customize every key at any time by pressing **Ctrl + K** in-game, or by editing `BepInEx\config\HouseAccess.HouseAccess.cfg`.  
Press **F1** at any time while playing to hear the in-game spoken key reference.

### 🎯 Targeting & Interaction
| Key | Action | Description |
| :--- | :--- | :--- |
| **Ctrl + Page Down** | **Next Target** | Cycles to the next person or object in range. |
| **Ctrl + Page Up** | **Previous Target** | Cycles to the previous person or object. |
| **Page Down / Page Up** | **Next / Prev Category** | Jumps across categories (Guests, Items, Doors, Furniture). |
| **Home** | **Repeat Target** | Reads out the current target's name and details again. |
| **Equals (`=`)** | **Primary Interact** | Performs the default action (talk to guest, examine, open door). |
| **Ctrl + Equals (`Ctrl + =`)** | **All Actions** | Opens a full menu of every possible interaction with the target. |
| **Ctrl + End** | **Carry** | Pick up and carry someone. |
| **Ctrl + Z** | **Search by Name** | Type a name to find anyone or anything in the area. |

### 🧭 Movement & Spatial Navigation
| Key | Action | Description |
| :--- | :--- | :--- |
| **End** | **Auto-Walk to Target** | Automatically walks straight to your selected target. |
| **Ctrl + Home** | **Face Target** | Smoothly turns you to face your target directly. |
| **J / L** | **Turn Left / Right** | Small turns. Hold **Shift + J / L** for a crisp 90-degree quarter turn. |
| **Shift + PgUp / PgDn**| **Face Next / Prev** | Turns toward neighboring targets without moving. |
| **Comma (`,`)** | **Emergency Halt** | Instantly stops walking and dismisses open interaction menus. |
| **Ctrl + B** | **Audio Beacon** | Toggles a 3D audio chirp that guides your ears directly to the target. |

### 🔍 Situational Awareness
| Key | Action | Description |
| :--- | :--- | :--- |
| **Ctrl + R** | **Where Am I?** | Announces your current room and zone. |
| **Ctrl + D** | **Find Doorways** | Lists every exit and passage leading out of the current room. |
| **Alt + R** | **Room Scanner** | Reads an inventory of everyone and everything currently in the room. |
| **Ctrl + P** | **Nearby People** | Lists everyone within immediate speaking distance. |
| **Alt + Home** | **Look Ahead** | Describes whatever is directly in front of your character. |
| **Ctrl + V** | **Line of Sight** | "Who can see me?" — essential for sneaking and stealthy antics! |
| **Alt + `=` / Alt + `-`** | **Radius Range** | Expands or contracts the targeting search radius. |

### 🗣️ Dialogue, Status & Inventory
| Key | Action | Description |
| :--- | :--- | :--- |
| **F3** | **Player Status** | Speaks your health, state, and relevant status details. |
| **F2** | **Read Screen** | Re-reads active on-screen prompts or subtitles. |
| **F4** | **Target Profile** | Describes your targeted guest (appearance, mood, state). |
| **Ctrl + I** | **Inventory** | Opens your carried items. |
| **Dash (`-`)** | **Hands Menu** | Inspect what you are holding, put items down, or throw. |
| **Ctrl + T** | **Type Text** | Focuses text input when the game prompts you to type. |

### ⚡ Rapid Number Navigation
Every list in House Access (dialogue choices, inventory items, menus, action lists) supports instant numbered selection:
- **Keys 1 through 9, and 0**: Reads the item at that position immediately (0 is the 10th item).
- **Ctrl + Number**: Instantly picks/executes that choice!
- Pressing a number higher than the list size politely announces the total number of items available.
- **Up / Down arrows**: Move through the list one item at a time — each is read as you go ("reply, 2 of 5"). **Enter** picks the item you are on. Works in dialogue replies, inventories, wheels, action lists and menus alike, and the game's own highlight follows the item being read.

### 📷 Camera & Photography
| Key | Action |
| :--- | :--- |
| **Alt + P** | Review captured photos |
| **Alt + K** | Switch camera perspective |
| **Alt + G** | Cycle camera shot type |
| **Alt + F** | Frame subject automatically |
| **Alt + H** | Adjust camera height |

### 🛠️ Mod Diagnostics & Audio Controls
| Key | Action |
| :--- | :--- |
| **Ctrl + K** | Open in-game Keybinding Editor |
| **Ctrl + F1** | Run Speech Engine Test |
| **Ctrl + F12** | Cycle Speech Backends (NVDA ➔ UniversalSpeech ➔ SAPI) |
| **Ctrl + F3** | Toggle Mod Speech On / Off |
| **Ctrl + F2** | Dump Diagnostics Log to `UserData\HouseAccess` |

---

## 🏷️ Custom Labels & Descriptions

You can personalize how the game describes objects and rooms:
- **`UserData\HouseAccess\unlabeled.txt`**: When the mod encounters a button or control without built-in text, it logs it here.
- **`UserData\HouseAccess\labels.txt`**: Copy raw IDs here and give them friendly names (e.g. `btnLightSwitch = Living Room Light Switch`).
- **`descriptions.txt`** & **`cutscenes.txt`**: Add custom descriptive text for rooms, items, and cutscenes that read out when triggered.

---

## ⚠️ Notes on Engine Limitations

To keep expectations transparent: modern House Party uses a single-assembly IL2CPP layout. A few minor subsystems from the vintage 1.1.0 MelonLoader era no longer have internal game variables behind them:
- Exact relationship percentages and deep opportunity meters are unexposed in current game code.
- Granular clothing layers (beyond general clothing/nudity status) are not accessible.
- Fighting minigames, the developer console, and the in-game smartphone screen cannot currently be read.

Rather than failing silently, House Access will audibly let you know if you press a key corresponding to an unsupported feature.

---


## 📜 Heritage, Credits & Attribution

House Access was originally created by **hasajaza** as an accessibility mod for MelonLoader and shared with the blind gaming community on [AudioGames.net Forum (Topic #60065)](https://forum.audiogames.net/topic/60065/house-party-access-mod-18/).

When House Party transitioned to a modern single-assembly IL2CPP architecture, MelonLoader 1.1.0 ceased to work. This repository honors hasajaza's original pioneering work by rebuilding and porting the mod to **BepInEx 6 IL2CPP**, fixing start-up access violations, ensuring sliders/dropdowns speak in real time, and making the modern game completely playable once again.

- **Original Concept & Mod Author**: **hasajaza** ([AudioGames.net Thread](https://forum.audiogames.net/topic/60065/house-party-access-mod-18/))
- **BepInEx 6 IL2CPP Port & v1.1.3 Fixes**: Rebuilt with love for the blind and visually impaired gaming community.
- **Third-Party Libraries**: UniversalSpeech, NVDA Controller Client, ZDSR, and BepInEx.

## 🔧 Building From Source

Developers wishing to contribute or customize House Access can build the project directly:
1. Install the **.NET 6 SDK**.
2. Build the project:
   ```shell
   dotnet build port\HouseAccess.csproj -c Release
   ```
   The project finds the game's BepInEx install automatically: it scans the fixed drives for a folder containing `HouseParty.exe` with `BepInEx\core` and `BepInEx\interop` next to it, and compiles against that. If none is found it falls back to the copy stashed in the repo's `tmp\BepInEx788-aside`, so a plain clone still builds. To force a specific install, point `BepInExRoot` at its `BepInEx` folder:
   ```shell
   dotnet build port\HouseAccess.csproj -c Release -p:BepInExRoot="C:\path\to\BepInEx"
   ```
3. Copy `port\bin\Release\HouseAccess.dll` to your `<House Party>\BepInEx\plugins\` directory.

---

<div align="center">

**Have a blast at the party!**  
*Contributions, suggestions, and feedback from the accessibility community are warmly welcomed.*

</div>
