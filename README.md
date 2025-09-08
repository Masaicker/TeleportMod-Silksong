# Teleport Mod

A simple teleportation mod for Hollow Knight: Silksong.

## English Documentation

### Usage

#### Keyboard Controls
- `Ctrl + 1~5`: Save position 🔊 Sound effect
- `Alt + 1~5`: Load position
- `Alt + 6`: **Safe respawn** (cycle entry points)
- `Alt + 0`: **Reset all coordinates**
- No save data: Auto teleport to bench

```
💡 Note: All key combinations are fully customizable in the config file, including modifier keys and function keys!
```

#### Gamepad Controls
- `LB + RB + Direction/A`: Teleport to slot
- `LB + Start + Direction/A`: Save to slot 🔊 Sound effect
- `LB + RB + Y`: **Safe respawn**
- `LB + Select + Start`: **Reset all coordinates**

```
💡 Function Notes: Safe respawn is for escaping when stuck in bugs, reset coordinates is for clearing all data when stuck.
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

#### Usage frequency limits:
⏱️ Avoid multiple teleportations in short time (like teleporting multiple times within 1 second)

#### Key information:
🎹 Default keyboard keys are main keyboard numbers `1-6`, `0`, **NOT numpad keys**  
🎹 To use numpad, manually change to `Keypad1-6` in config file

### Troubleshooting:

🆘 **If stuck/camera lost:**

1. First try: `Alt + 6` **(Safe Respawn)** (Keyboard: `Alt+6`, Gamepad: `LB+RB+Y`)
2. Still not working, completely unresponsive: **Restart the game**

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
2. Put `Teleport.dll` in `BepInEx/plugins/`
3. Start game

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
- `Reset All Key` (default: `Alpha0`): Key for reset all function

#### Data Storage
Coordinate data will be automatically saved to:  
`C:\Users\[Username]\AppData\LocalLow\Team Cherry\Hollow Knight Silksong\TeleportMod\savedata.json`

---

# 传送模组

空洞骑士：丝之歌的简单传送模组。

## 中文文档

### 使用方法

#### 键盘操作
- `Ctrl + 1~5`: 保存位置 🔊 有音效提示
- `Alt + 1~5`: 读取位置
- `Alt + 6`: **安全重生**（轮换入口点）
- `Alt + 0`: **重置所有坐标**
- 无存档时自动传送到椅子

```
💡 提示：所有按键组合都可以在配置文件中完全自定义，包括修饰键和功能键！
```

#### 手柄操作
- `LB + RB + 方向键/A`: 传送到档位
- `LB + Start + 方向键/A`: 保存到档位 🔊 有音效提示
- `LB + RB + Y`: **安全重生**
- `LB + Select + Start`: **重置所有坐标**

```
💡 功能说明：安全重生功能用于卡BUG时脱困，重置坐标功能用于防止卡死时清空所有坐标重新开始。
```

### 安全使用指南

```
⚠️ 重要提醒：为避免游戏BUG和数据损坏，请务必遵循以下安全准则！
```

#### 何时可以安全使用:
✅ 只在角色完全可控制时保存或传送  
✅ 在正常游戏场景中，无战斗状态时

#### 危险情况 - 请勿使用:
❌ **BOSS战期间**  
❌ **封闭战斗区域内**  
❌ 坐在椅子上时  
❌ 过场动画播放时  
❌ 角色被控制或无法移动时  
❌ 加载/保存过程中  
❌ 任何特殊状态或事件触发时

#### 使用频率限制:
⏱️ 不要在极短时间内多次读档传送（如1秒内连续传送多次）

#### 按键说明:
🎹 默认键盘按键是主键盘数字键`1-6`、`0`，**非小键盘数字键**  
🎹 如需使用小键盘，请在配置文件中自行修改为`Keypad1-6`等

### 故障处理:

🆘 **如遇卡死/视角丢失：**

1. 首先尝试：`Alt + 6` **(安全重生)**（键盘：`Alt+6`，手柄：`LB+RB+Y`）
2. 仍无效果，完全没反应时：**重启游戏**

### 紧急救援配置:

如果所有方法都无效，可以手动编辑存档文件，使用以下安全配置：  
⚠️ **重要：** 先关闭游戏，修改保存后再启动游戏，进入游戏后读取1号存档即可（键盘：`Alt+1`，手柄：`LB+RB+方向上`）

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

1号档位默认在初始小镇的车站里，这是一个**绝对安全的位置**。

**配置文件位置：**  
`C:\Users\[用户名]\AppData\LocalLow\Team Cherry\Hollow Knight Silksong\TeleportMod\savedata.json`

### 安装

1. 安装BepInEx
2. 将`Teleport.dll`文件放入`BepInEx/plugins/`文件夹
3. 启动游戏

### 配置

- `启用详细日志` (默认: `false`): 显示详细日志
- `启用手柄支持` (默认: `true`): 启用手柄输入
- `启用彩蛋音效` (默认: `false`): 🎵 启用存档时的特殊音效
- `音效音量` (默认: `0.5`): 🔊 存档音效音量大小（0.0-1.0，设置为0关闭音效）
- `保存修饰键` (默认: `LeftControl`): 保存坐标使用的修饰键
- `传送修饰键` (默认: `LeftAlt`): 传送坐标使用的修饰键
- `重置修饰键` (默认: `LeftAlt`): 重置功能使用的修饰键
- `存档槽1-5按键` (默认: `Alpha1-5`): 存档槽使用的按键
- `安全重生按键` (默认: `Alpha6`): 安全重生功能按键
- `重置所有坐标按键` (默认: `Alpha0`): 重置所有坐标功能按键

#### 数据存储
坐标数据会自动保存在以下路径：  
`C:\Users\[用户名]\AppData\LocalLow\Team Cherry\Hollow Knight Silksong\TeleportMod\savedata.json`