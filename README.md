# Teleport Mod

> **English** | [中文](README_zh.md)

A simple teleportation mod for Hollow Knight: Silksong.

## English Documentation

### Usage

#### Keyboard Controls
- `Ctrl + 1~5`: Save position 🔊 Sound effect
- `Alt + 1~5`: Load position (no save data: auto teleport to bench)
- `Alt + 6`: **Safe respawn** (cycle entry points)
- `Alt + 7`: **Teleport to bench** (last respawn point)
- `Alt + 0`: **Reset all coordinates**
- `Alt + -`: **Emergency teleport** (to preset safe location) [minus key]
- **🆘 `Ctrl + F9`: Emergency return to main menu** (character out of control/stuck)

```
💡 Note: Default uses main keyboard number keys, NOT numpad keys. All key combinations are fully customizable in the config file, including modifier keys and function keys!
```

#### Gamepad Controls
- `LB + RB + Direction/A`: Teleport to slot
- `LB + Start + Direction/A`: Save to slot 🔊 Sound effect
- `LB + RB + Y`: **Safe respawn**
- `LB + RB + B`: **Teleport to bench** (last respawn point)
- `LB + Select + Start`: **Reset all coordinates**
- `LB + RB + X`: **Emergency teleport** (to preset safe location)

```
💡 Note: All gamepad controls are fully customizable to match your personal preferences.
```

#### 🎮 Gamepad Key Reference

All gamepad controls are fully customizable in the config file. Here's the complete key mapping:

**Direction Keys:**
- `DPadUp` = D-Pad Up
- `DPadDown` = D-Pad Down  
- `DPadLeft` = D-Pad Left
- `DPadRight` = D-Pad Right

**Face Buttons:**
- `JoystickButton0` = A Button
- `JoystickButton1` = B Button
- `JoystickButton2` = X Button
- `JoystickButton3` = Y Button

**Shoulders & Triggers:**
- `LeftBumper` = Left Bumper (LB)
- `RightBumper` = Right Bumper (RB)
- `LeftTrigger` = Left Trigger (LT)
- `RightTrigger` = Right Trigger (RT)

**System Buttons:**
- `JoystickButton6` = Select/Back Button
- `JoystickButton7` = Start Button
- `JoystickButton8` = Home/Guide Button

**Default Gamepad Configuration:**
- Slot 1: `DPadUp` (D-Pad Up)
- Slot 2: `DPadDown` (D-Pad Down)
- Slot 3: `DPadLeft` (D-Pad Left)
- Slot 4: `DPadRight` (D-Pad Right)
- Slot 5: `JoystickButton0` (A Button)
- Teleport Modifiers: `LeftBumper` + `RightBumper` (LB + RB)
- Save Modifiers: `LeftBumper` + `JoystickButton7` (LB + Start)
- Safe Respawn: `JoystickButton3` (Y Button) [in teleport mode]
- Hardcoded Teleport: `JoystickButton2` (X Button) [in teleport mode]
- Bench Teleport: `JoystickButton1` (B Button) [in teleport mode]
- Reset All: `LeftBumper` + `JoystickButton6` + `JoystickButton7` (LB + Select + Start)

```
⚙️ All gamepad controls can be customized in the game's config file.
```

### Safety Guidelines

```
⚠️ Important: Please follow these safety guidelines to avoid game bugs and data corruption!
```

#### When it's safe to use:
✅ Only save or teleport when your character is fully controllable  
✅ In normal game scenes, when not in combat

#### Dangerous situations - DO NOT use:
❌ **During boss battles**  
❌ **Inside closed combat areas**  
❌ While sitting on benches  
❌ During cutscenes or animations  
❌ When character is controlled or immobilized  
❌ During loading/saving processes  
❌ During any special states or triggered events

#### Important Notes:
⏱️ Avoid multiple teleportations in short time (like teleporting multiple times within 1 second)  
⚠️ Do not teleport immediately after death

### Troubleshooting:

🆘 **If stuck/camera lost/character floating and unable to open menu:**

1. First try: **Safe Respawn** (Keyboard: `Alt+6`, Gamepad: `LB+RB+Y`), if teleported to entry point, then use load position as needed  
   Example: If character is off-screen after loading save, use Alt+6, then reload the same save slot to fix camera issues
2. Character completely out of control: **Emergency return to main menu** (Keyboard: `Ctrl+F9`, no gamepad shortcut - this is an emergency measure for critical situations)
3. Still not working, completely unresponsive: **Restart the game**

🚨 **All save data lost or stuck in a scene:**

If all your save slots are lost or you're trapped in an unescapable scene, use **Emergency Teleport** (Keyboard: `Alt+-` [minus key], Gamepad: `LB+RB+X`) to instantly teleport to a preset safe location (starting town station). This works independently of your save data.

### Emergency rescue config:

If all methods fail, manually edit the save file with this safe config:  
⚠️ **Important:** Close the game first, modify and save, then restart the game, and load slot 1 after entering the game (Keyboard: `Alt+1`, Gamepad: `LB+RB+Up`)

```json
{
  "saveSlots": {
    "1": {
      "x": 71.42231,
      "y": 9.597684,
      "z": 0.004,
      "scene": "Bellway_01",
      "hasData": true
    }
  },
  "currentEntryPointIndex": 0
}
```

Slot 1 defaults to the station in the starting town, which is **absolutely safe**.

**Config file location:**  
`C:\Users\[Username]\AppData\LocalLow\Team Cherry\Hollow Knight Silksong\TeleportMod\savedata.json`

### Installation

1. Install BepInEx
2. Extract and put the `Teleport` related folder into `BepInEx/plugins/` folder
3. Ensure `Teleport.dll`, `manbo.wav`, and `Gamesave.wav` are in the same directory under the folder
4. Start game

### Config

- `Enable Detailed Logging` (default: `false`): Show detailed logs
- `Enable Gamepad Support` (default: `true`): Enable controller input
- `Enable Easter Egg Audio` (default: `false`): 🎵 Enable special sound effect for saving
- `Audio Volume` (default: `0.5`): 🔊 Volume level for save sound effect (0.0-1.0, 0=disable)
- `Save Modifier Key` (default: `LeftControl`): Modifier key for saving coordinates
- `Teleport Modifier Key` (default: `LeftAlt`): Modifier key for teleporting
- `Reset Modifier Key` (default: `LeftAlt`): Modifier key for reset functions
- `Slot 1-5 Keys` (default: `Alpha1-5`): Keys for save slots
- `Safe Respawn Key` (default: `Alpha6`): Key for safe respawn function
- `Bench Teleport Key` (default: `Alpha7`): Key for bench teleport function
- `Emergency Teleport Key` (default: `Minus`): Key for emergency teleport function
- `Reset All Key` (default: `Alpha0`): Key for reset all function

#### Data Storage
Coordinate data path:  
`C:\Users\[Username]\AppData\LocalLow\Team Cherry\Hollow Knight Silksong\TeleportMod\savedata.json`

Configuration file location:  
`{Game Install Directory}\BepInEx\config\Mhz.TeleportMod.cfg`

```
💡 Note: If the config file or folder doesn't exist, please run the game once to generate it first.
```
