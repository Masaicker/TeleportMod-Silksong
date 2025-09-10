# 传送模组

> [English](README.md) | **中文**

空洞骑士：丝之歌的简单传送模组。

## 中文文档

### 使用方法

#### 键盘操作
- `Ctrl + 1~5`: 保存位置 🔊 有音效提示
- `Alt + 1~5`: 读取位置（无存档时自动传送到椅子）
- `Alt + 6`: **安全重生**（轮换入口点）
- `Alt + 7`: **传送到椅子**（最后的重生点）
- `Alt + 0`: **重置所有坐标**
- `Alt + -`: **紧急传送**（传送到预设安全地点）[减号键]
- **🆘 `Ctrl + F9`: 紧急返回主菜单**（角色失控/卡死时使用）

```
💡 提示：默认使用主键盘数字键，非小键盘。所有按键组合都可以在配置文件中完全自定义，包括修饰键和功能键！
```

#### 手柄操作
- `LB + RB + 方向键/A`: 传送到档位
- `LB + Start + 方向键/A`: 保存到档位 🔊 有音效提示
- `LB + RB + Y`: **安全重生**
- `LB + RB + B`: **传送到椅子**（最后的重生点）
- `LB + Select + Start`: **重置所有坐标**
- `LB + RB + X`: **紧急传送**（传送到预设安全地点）

```
💡 提示：所有手柄按键均支持自定义配置，可根据个人习惯调整操作方式。
```

#### 🎮 手柄按键对照表详细说明

所有手柄操作都可以在配置文件中完全自定义。以下是完整按键对照：

**方向键:**
- `DPadUp` = 方向键上
- `DPadDown` = 方向键下
- `DPadLeft` = 方向键左  
- `DPadRight` = 方向键右

**面部按钮:**
- `JoystickButton0` = A按钮
- `JoystickButton1` = B按钮
- `JoystickButton2` = X按钮
- `JoystickButton3` = Y按钮

**肩键扳机:**
- `LeftBumper` = 左肩键(LB)
- `RightBumper` = 右肩键(RB)
- `LeftTrigger` = 左扳机(LT)
- `RightTrigger` = 右扳机(RT)

**系统按钮:**
- `JoystickButton6` = Select/Back按钮
- `JoystickButton7` = Start按钮
- `JoystickButton8` = Home/Guide按钮

**摇杆方向 (可选配置):**
- `LeftStickUp/Down/Left/Right` = 左摇杆方向
- `RightStickUp/Down/Left/Right` = 右摇杆方向
- `LeftStickButton` = 左摇杆按下(L3)
- `RightStickButton` = 右摇杆按下(R3)

**默认手柄配置:**
- 存档槽1: `DPadUp` (方向键上)
- 存档槽2: `DPadDown` (方向键下)
- 存档槽3: `DPadLeft` (方向键左)
- 存档槽4: `DPadRight` (方向键右)
- 存档槽5: `JoystickButton0` (A按钮)
- 传送修饰键: `LeftBumper` + `RightBumper` (LB + RB)
- 保存修饰键: `LeftBumper` + `JoystickButton7` (LB + Start)
- 安全重生: `JoystickButton3` (Y按钮) [传送模式下]
- 硬编码传送: `JoystickButton2` (X按钮) [传送模式下]
- 椅子传送: `JoystickButton1` (B按钮) [传送模式下]
- 重置所有: `LeftBumper` + `JoystickButton6` + `JoystickButton7` (LB + Select + Start)

```
⚙️ 所有手柄操作都可以在游戏配置文件中自定义。
💡 如果旧版本配置项过多影响查看，可删除.cfg配置文件，重启游戏自动生成最新配置。
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

#### 注意事项:
⏱️ 不要在极短时间内多次读档传送（如1秒内连续传送多次）  
⚠️ 不要死亡后马上传送

### 故障处理:

🆘 **如遇卡死/视角丢失/角色起飞无法呼出菜单：**

1. 首先尝试：**安全重生**（键盘：`Alt+6`，手柄：`LB+RB+Y`），如果传送到入口，再按需读档传送  
   例如：读档传送后角色在屏幕外，使用Alt+6，随后再读刚才的档，能解决卡视野问题
2. 角色完全失控时：**紧急返回主菜单**（键盘：`Ctrl+F9`，无手柄按键 - 这是非常时期的特殊手段）
3. 仍无效果，完全没反应时：**重启游戏**

🚨 **所有存档丢失导致困死在某个场景：**

如果你的所有存档槽都丢失导致困在无法逃脱的场景中，使用**紧急传送**（键盘：`Alt+-` [减号键]，手柄：`LB+RB+X`）立即传送到预设安全地点（初始小镇车站）。此功能独立于存档数据运行。

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
2. 解压后将`Teleport`相关文件夹整体放入`BepInEx/plugins/`文件夹
3. 确保文件夹下的`Teleport.dll`、`manbo.wav`和`Gamesave.wav`在同一目录
4. 启动游戏

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
- `椅子传送按键` (默认: `Alpha7`): 椅子传送功能按键
- `紧急传送按键` (默认: `Minus`): 紧急传送功能按键
- `重置所有坐标按键` (默认: `Alpha0`): 重置所有坐标功能按键

#### 数据存储
坐标数据路径：  
`C:\Users\[用户名]\AppData\LocalLow\Team Cherry\Hollow Knight Silksong\TeleportMod\savedata.json`

配置文件路径：  
`{游戏安装目录}\BepInEx\config\Mhz.TeleportMod.cfg`

```
💡 提示：如果没有配置文件或文件夹，请先运行一次游戏让其自动生成。
```
