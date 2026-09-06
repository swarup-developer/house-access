# House Access 1.1.3

Welcome to version **1.1.3** of House Access! This update is all about making interactions feel smooth, intuitive, and responsive so you can focus entirely on enjoying the party.

---

## Highlights of What's New

### 1. Menus Speak Naturally
- **Real-time feedback**: Sliders, option boxes, and dropdown menus now announce their values the moment you change them—not just when you first highlight them.
- **Immediate focus**: Opening any menu reads your current selection right away instead of falling into awkward silence.
- **Human-friendly labels**: We mapped internal game names to the text you actually see on screen. You will hear crisp, meaningful names rather than cryptic engine strings like `btnHolder`.
- **Quick repeat**: Pressing **Home** will repeat the current item anywhere—including inside menus!

### 2. Number Keys Everywhere
We’ve expanded number key support across all 11 game lists (dialogues, inventory, action wheels, options, search results, and menus):
- **Press 1–9 or 0**: Instantly speaks that numbered option (0 is the 10th item).
- **Hold Ctrl + Number**: Instantly picks that item!
- Works from both the number row and the numeric keypad.
- If you tap a number beyond the list size, the mod helpfully tells you how many items are available.

---

## Smooth Installation & The BepInEx Startup Fix

Because BepInEx 6 has an internal logging issue that causes the game to silently crash on launch before any screen reader or mod can wake up, we've included **`fix-bepinex-config.bat`**.

1. Extract **BepInEx 6 (IL2CPP x64)** into your House Party folder.
2. Launch House Party once, let BepInEx generate its files, then close the game.
3. Run **`fix-bepinex-config.bat`** to set `UnityLogListening = false` automatically. (No need to edit config files by hand!).
4. Place **`HouseAccess.dll`** in `BepInEx\plugins` and your speech DLLs next to `HouseParty.exe`.
5. Launch the game with your screen reader running and have fun!

---

*For full instructions, controls, and troubleshooting, please check the main [README.md](README.md).*
