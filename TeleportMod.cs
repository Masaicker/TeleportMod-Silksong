using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using GlobalEnums;

[BepInPlugin("Mhz.TeleportMod", "Teleport Mod", "1.0.5")]
public class TeleportMod : BaseUnityPlugin
{
    private new static ManualLogSource? Logger;

    // 配置项
    private static ConfigEntry<bool>? enableDetailedLogging;
    private static ConfigEntry<bool>? enableGamepadSupport;
    private static ConfigEntry<bool>? enableEasterEggAudio;
    private static ConfigEntry<string>? saveModifierKey;
    private static ConfigEntry<string>? teleportModifierKey;
    private static ConfigEntry<string>? resetModifierKey;

    // 存档槽按键配置
    private static ConfigEntry<string>? slot1Key;
    private static ConfigEntry<string>? slot2Key;
    private static ConfigEntry<string>? slot3Key;
    private static ConfigEntry<string>? slot4Key;
    private static ConfigEntry<string>? slot5Key;

    // 特殊功能按键配置
    private static ConfigEntry<string>? safeRespawnKey;
    private static ConfigEntry<string>? resetAllKey;
    private static ConfigEntry<string>? hardcodedTeleportKey;
    private static ConfigEntry<string>? benchTeleportKey;

    // 音效设置
    private static ConfigEntry<float>? audioVolume;

    // 多档位存档系统
    private static Dictionary<int, SaveSlot> saveSlots = new Dictionary<int, SaveSlot>();

    // Alt+6功能的入口点轮换索引
    private static int currentEntryPointIndex = 0;

    // 手柄轴输入状态跟踪
    private static bool wasVerticalPressed = false;
    private static bool wasHorizontalPressed = false;

    // 音频播放器复用
    private static GameObject? audioPlayerObject = null;
    private static AudioSource? audioPlayerSource = null;

    // 音频缓存
    private static AudioClip? cachedSaveAudioClip = null;
    private static float lastSaveAudioTime = 0f;
    private const float AUDIO_COOLDOWN = 0.1f; // 音频播放冷却时间

    // 存档数据结构
    public struct SaveSlot
    {
        public Vector3 position;
        public string scene;
        public bool hasData;

        public SaveSlot(Vector3 pos, string sceneName)
        {
            position = pos;
            scene = sceneName;
            hasData = true;
        }
    }

    // 可序列化的存档数据
    [System.Serializable]
    public class PersistentData
    {
        public Dictionary<int, SerializableSaveSlot> saveSlots = new Dictionary<int, SerializableSaveSlot>();
        public int currentEntryPointIndex = 0;
    }

    [System.Serializable]
    public class SerializableSaveSlot
    {
        public float x, y, z;
        public string scene = "";
        public bool hasData = false;

        public SerializableSaveSlot() { }

        public SerializableSaveSlot(SaveSlot slot)
        {
            x = slot.position.x;
            y = slot.position.y;
            z = slot.position.z;
            scene = slot.scene ?? "";
            hasData = slot.hasData;
        }

        public SaveSlot ToSaveSlot()
        {
            return new SaveSlot(new Vector3(x, y, z), scene);
        }
    }

    private void Awake()
    {
        Logger = base.Logger;

        // 初始化配置项
        enableDetailedLogging = Config.Bind("日志设置 | Logging", "启用详细日志 | Enable Detailed Logging", false, "是否启用详细的传送日志输出 | Enable detailed teleport logging output");
        enableGamepadSupport = Config.Bind("控制设置 | Controls", "启用手柄支持 | Enable Gamepad Support", true,
            "是否启用手柄控制传送功能。操作方法：传送=LB+RB+方向键/A，保存=LB+Start+方向键/A，安全重生=LB+RB+Y，硬编码传送=LB+RB+X，重置所有坐标=LB+Select+Start | " +
            "Enable gamepad control for teleport functions. Controls: Teleport=LB+RB+Directional/A, Save=LB+Start+Directional/A, Safe respawn=LB+RB+Y, Hardcoded teleport=LB+RB+X, Reset all coordinates=LB+Select+Start");

        // 音效设置
        enableEasterEggAudio = Config.Bind("音效设置 | Audio Settings", "启用彩蛋音效 | Enable Easter Egg Audio", false,
            "是否启用彩蛋音效。开启后存档时播放特殊音效，关闭时播放默认音效。需要重启游戏生效 | Enable easter egg audio effect. When enabled, plays special sound effect when saving, otherwise plays default sound effect. Requires game restart to take effect");

        audioVolume = Config.Bind("音效设置 | Audio Settings", "音效音量 | Audio Volume", 0.5f,
            "存档音效的音量大小。范围0.0-1.0，设置为0关闭音效 | Volume level for save sound effect. Range 0.0-1.0, set to 0 to disable audio");

        // 按键设置
        saveModifierKey = Config.Bind("按键设置 | Key Settings", "保存修饰键 | Save Modifier Key", "LeftControl",
            "保存坐标时使用的修饰键。可选值：LeftControl, RightControl, LeftAlt, RightAlt, LeftShift, RightShift | " +
            "Modifier key for saving coordinates. Options: LeftControl, RightControl, LeftAlt, RightAlt, LeftShift, RightShift");

        teleportModifierKey = Config.Bind("按键设置 | Key Settings", "传送修饰键 | Teleport Modifier Key", "LeftAlt",
            "传送坐标时使用的修饰键。可选值：LeftControl, RightControl, LeftAlt, RightAlt, LeftShift, RightShift | " +
            "Modifier key for teleporting coordinates. Options: LeftControl, RightControl, LeftAlt, RightAlt, LeftShift, RightShift");

        resetModifierKey = Config.Bind("按键设置 | Key Settings", "重置修饰键 | Reset Modifier Key", "LeftAlt",
            "重置坐标和安全重生时使用的修饰键。可选值：LeftControl, RightControl, LeftAlt, RightAlt, LeftShift, RightShift | " +
            "Modifier key for reset and safe respawn functions. Options: LeftControl, RightControl, LeftAlt, RightAlt, LeftShift, RightShift");

        // 存档槽按键配置
        slot1Key = Config.Bind("存档槽按键 | Slot Keys", "存档槽1按键 | Slot 1 Key", "Alpha1",
            "存档槽1使用的按键。可用：Alpha0-9, F1-F12, Q, W, E, R, T, Y, U, I, O, P等 | Key for slot 1. Available: Alpha0-9, F1-F12, Q, W, E, R, T, Y, U, I, O, P, etc.");
        slot2Key = Config.Bind("存档槽按键 | Slot Keys", "存档槽2按键 | Slot 2 Key", "Alpha2",
            "存档槽2使用的按键 | Key for slot 2");
        slot3Key = Config.Bind("存档槽按键 | Slot Keys", "存档槽3按键 | Slot 3 Key", "Alpha3",
            "存档槽3使用的按键 | Key for slot 3");
        slot4Key = Config.Bind("存档槽按键 | Slot Keys", "存档槽4按键 | Slot 4 Key", "Alpha4",
            "存档槽4使用的按键 | Key for slot 4");
        slot5Key = Config.Bind("存档槽按键 | Slot Keys", "存档槽5按键 | Slot 5 Key", "Alpha5",
            "存档槽5使用的按键 | Key for slot 5");

        // 特殊功能按键配置
        safeRespawnKey = Config.Bind("特殊功能按键 | Special Keys", "安全重生按键 | Safe Respawn Key", "Alpha6",
            "安全重生功能使用的按键 | Key for safe respawn function");
        resetAllKey = Config.Bind("特殊功能按键 | Special Keys", "重置所有坐标按键 | Reset All Key", "Alpha0",
            "重置所有坐标功能使用的按键 | Key for reset all coordinates function");
        hardcodedTeleportKey = Config.Bind("特殊功能按键 | Special Keys", "硬编码传送按键 | Hardcoded Teleport Key", "Minus",
            "传送到预设坐标的按键。默认是减号键(-) | Key for teleporting to preset coordinates. Default is minus key (-)");
        benchTeleportKey = Config.Bind("特殊功能按键 | Special Keys", "椅子传送按键 | Bench Teleport Key", "Alpha7",
            "传送到椅子（最后重生点）的按键 | Key for teleporting to bench (last respawn point)");

        Logger.LogInfo("Teleport Mod 已加载!");

        if (enableDetailedLogging?.Value == true)
        {
            Logger.LogInfo("详细日志已启用 | Detailed logging enabled");
        }
        else
        {
            Logger.LogInfo("详细日志已禁用，只显示重要信息 | Detailed logging disabled, showing important messages only");
        }

        if (enableGamepadSupport?.Value == true)
        {
            Logger.LogInfo("手柄支持已启用 | Gamepad support enabled");
        }
        else
        {
            Logger.LogInfo("手柄支持已禁用 | Gamepad support disabled");
        }

        // 加载持久化数据
        LoadPersistentData();

        // 预加载音频文件
        StartCoroutine(PreloadAudioClip());
    }

    // 加载持久化数据
    private void LoadPersistentData()
    {
        try
        {
            string filePath = GetSaveFilePath();
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                PersistentData? data = JsonConvert.DeserializeObject<PersistentData>(json);

                if (data != null && data.saveSlots != null)
                {
                    // 恢复存档槽数据
                    saveSlots.Clear();
                    foreach (var kvp in data.saveSlots)
                    {
                        if (kvp.Value != null && kvp.Value.hasData)
                        {
                            saveSlots[kvp.Key] = kvp.Value.ToSaveSlot();
                        }
                    }

                    // 恢复入口点索引
                    currentEntryPointIndex = data.currentEntryPointIndex;

                    Logger?.LogInfo($"已加载持久化数据：{data.saveSlots.Count} 个存档槽 | Loaded persistent data: {data.saveSlots.Count} save slots");
                }
            }
            else
            {
                LogInfo("未找到存档文件，使用默认设置 | No save file found, using defaults");
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"加载持久化数据时发生错误 | Error loading persistent data: {ex.Message}");
        }
    }

    // 保存持久化数据
    private void SavePersistentData()
    {
        try
        {
            PersistentData data = new PersistentData();

            // 保存存档槽数据
            foreach (var kvp in saveSlots)
            {
                if (kvp.Value.hasData)
                {
                    data.saveSlots[kvp.Key] = new SerializableSaveSlot(kvp.Value);
                }
            }

            // 保存入口点索引
            data.currentEntryPointIndex = currentEntryPointIndex;

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            string filePath = GetSaveFilePath();

            // 确保目录存在
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, json);
            LogInfo($"已保存持久化数据：{data.saveSlots.Count} 个存档槽 | Saved persistent data: {data.saveSlots.Count} save slots");
        }
        catch (Exception ex)
        {
            Logger?.LogError($"保存持久化数据时发生错误 | Error saving persistent data: {ex.Message}");
        }
    }

    // 获取存档文件路径
    private string GetSaveFilePath()
    {
        try
        {
            // 使用Unity的persistentDataPath获取游戏数据目录
            // 通常是: C:\Users\[用户名]\AppData\LocalLow\Team Cherry\Hollow Knight Silksong
            string gameDataPath = Application.persistentDataPath;

            // 在游戏数据目录下创建TeleportMod子文件夹
            string modDataPath = Path.Combine(gameDataPath, "TeleportMod");

            // 返回完整的JSON文件路径
            string saveFilePath = Path.Combine(modDataPath, "savedata.json");

            LogInfo($"存档文件路径 | Save file path: {saveFilePath}");
            return saveFilePath;
        }
        catch (Exception ex)
        {
            Logger?.LogError($"获取存档文件路径时发生错误 | Error getting save file path: {ex.Message}");
            // 出错时回退到相对路径
            return Path.Combine("TeleportMod", "savedata.json");
        }
    }

    // 辅助方法：只在配置启用时输出详细日志
    private static void LogInfo(string message)
    {
        if (enableDetailedLogging?.Value == true)
        {
            Logger?.LogInfo(message);
        }
    }

    private void Update()
    {
        // 使用UnsafeInstance避免游戏启动时的错误日志
        if (GameManager.UnsafeInstance == null)
        {
            return;
        }

        // 手柄输入检测
        if (enableGamepadSupport?.Value == true)
        {
            HandleGamepadInput();
        }

        // 键盘输入检测
        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        // 优先处理紧急重启按键，即使游戏暂停或状态异常也要响应
        HandleEmergencyRestartInput();

        // 检查游戏是否暂停，如果暂停则忽略其他输入
        var gm = GameManager.UnsafeInstance;
        if (gm == null || gm.isPaused || gm.GameState != GlobalEnums.GameState.PLAYING)
        {
            return;
        }

        // 保存修饰键+存档槽按键 保存对应档位
        if (IsModifierKeyPressed(saveModifierKey?.Value ?? "LeftControl"))
        {
            for (int i = 1; i <= 5; i++)
            {
                KeyCode slotKey = GetSlotKey(i);
                if (slotKey != KeyCode.None && Input.GetKeyDown(slotKey))
                {
                    SaveToSlot(i);
                    break;
                }
            }
        }
        // 传送修饰键+存档槽按键 读取对应档位
        else if (IsModifierKeyPressed(teleportModifierKey?.Value ?? "LeftAlt"))
        {
            for (int i = 1; i <= 5; i++)
            {
                KeyCode slotKey = GetSlotKey(i);
                if (slotKey != KeyCode.None && Input.GetKeyDown(slotKey))
                {
                    LoadFromSlot(i);
                    break;
                }
            }
        }

        // 重置修饰键+特殊功能键
        if (IsModifierKeyPressed(resetModifierKey?.Value ?? "LeftAlt"))
        {
            // 重置修饰键+安全重生按键
            KeyCode safeRespawnKeyCode = ParseKeyCode(safeRespawnKey?.Value ?? "Alpha6");
            if (safeRespawnKeyCode != KeyCode.None && Input.GetKeyDown(safeRespawnKeyCode))
            {
                RespawnToSafeEntryPoint();
                return;
            }

            // 重置修饰键+重置所有坐标按键
            KeyCode resetAllKeyCode = ParseKeyCode(resetAllKey?.Value ?? "Alpha0");
            if (resetAllKeyCode != KeyCode.None && Input.GetKeyDown(resetAllKeyCode))
            {
                ClearAllSaveSlots();
                return;
            }

            // 重置修饰键+硬编码传送按键
            KeyCode hardcodedTeleportKeyCode = ParseKeyCode(hardcodedTeleportKey?.Value ?? "Minus");
            if (hardcodedTeleportKeyCode != KeyCode.None && Input.GetKeyDown(hardcodedTeleportKeyCode))
            {
                TeleportToHardcodedPosition();
                return;
            }
        }

        // 传送修饰键+椅子传送按键
        if (IsModifierKeyPressed(teleportModifierKey?.Value ?? "LeftAlt"))
        {
            KeyCode benchTeleportKeyCode = ParseKeyCode(benchTeleportKey?.Value ?? "Alpha7");
            if (benchTeleportKeyCode != KeyCode.None && Input.GetKeyDown(benchTeleportKeyCode))
            {
                TeleportToBench();
                return;
            }
        }
    }

    private void HandleGamepadInput()
    {
        try
        {
            // 检查游戏是否暂停，如果暂停则忽略所有输入
            var gm = GameManager.UnsafeInstance;
            if (gm == null || gm.isPaused || gm.GameState != GlobalEnums.GameState.PLAYING)
            {
                return;
            }

            // 检查LB+RB是否被按下（传送模式激活）
            bool teleportModeActive = Input.GetKey(KeyCode.JoystickButton4) && Input.GetKey(KeyCode.JoystickButton5);

            // 检查保存模式：LB+Start按钮
            bool saveModeActive = Input.GetKey(KeyCode.JoystickButton4) &&
                                 (Input.GetKey(KeyCode.JoystickButton7) || Input.GetKey(KeyCode.JoystickButton9)); // LB + Start

            // 如果既不是传送模式也不是保存模式，直接返回
            if (!teleportModeActive && !saveModeActive) return;

            // Y按钮 - 轮换安全入口点（仅在传送模式下）
            if (teleportModeActive && Input.GetKeyDown(KeyCode.JoystickButton3))
            {
                RespawnToSafeEntryPoint();
                return;
            }

            // X按钮 - 硬编码传送（仅在传送模式下）
            if (teleportModeActive && Input.GetKeyDown(KeyCode.JoystickButton2))
            {
                TeleportToHardcodedPosition();
                return;
            }

            // B按钮 - 椅子传送（仅在传送模式下）
            if (teleportModeActive && Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                TeleportToBench();
                return;
            }

            // LB+Select+Start 清空所有存档坐标（重置功能）
            bool selectPressed = Input.GetKey(KeyCode.JoystickButton6) || Input.GetKey(KeyCode.JoystickButton8); // Select按钮可能是6或8
            bool startPressed = Input.GetKey(KeyCode.JoystickButton7) || Input.GetKey(KeyCode.JoystickButton9); // Start按钮可能是7或9
            if (Input.GetKey(KeyCode.JoystickButton4) && selectPressed && startPressed)
            {
                // 检测Start按钮被按下的瞬间
                if (Input.GetKeyDown(KeyCode.JoystickButton7) || Input.GetKeyDown(KeyCode.JoystickButton9))
                {
                    ClearAllSaveSlots();
                    return;
                }
            }

            // 方向键和A按钮 - 档位操作
            int slotNumber = 0;

            // 使用轴输入检测方向键
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // 检测方向键按下（需要轴值变化）
            if (Mathf.Abs(vertical) > 0.8f && !wasVerticalPressed)
            {
                if (vertical > 0) // Up
                    slotNumber = 1;
                else // Down
                    slotNumber = 2;
                wasVerticalPressed = true;
            }
            else if (Mathf.Abs(horizontal) > 0.8f && !wasHorizontalPressed)
            {
                if (horizontal < 0) // Left
                    slotNumber = 3;
                else // Right
                    slotNumber = 4;
                wasHorizontalPressed = true;
            }
            else if (Input.GetKeyDown(KeyCode.JoystickButton0)) // A按钮
            {
                slotNumber = 5;
            }

            // 重置轴按下状态
            if (Mathf.Abs(vertical) < 0.3f)
                wasVerticalPressed = false;
            if (Mathf.Abs(horizontal) < 0.3f)
                wasHorizontalPressed = false;

            // 执行档位操作
            if (slotNumber > 0)
            {
                if (saveModeActive)
                {
                    SaveToSlot(slotNumber);
                }
                else if (teleportModeActive)
                {
                    LoadFromSlot(slotNumber);
                }
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"处理手柄输入时发生错误 | Error handling gamepad input: {ex.Message}");
        }
    }

    // 清空所有存档坐标（重置功能）
    private void ClearAllSaveSlots()
    {
        try
        {
            // 清空内存中的存档数据
            saveSlots.Clear();

            // 重置入口点索引
            currentEntryPointIndex = 0;

            // 保存空数据到JSON文件
            SavePersistentData();

            Logger?.LogWarning("已清空所有存档坐标！| All save slots cleared!");
            Logger?.LogInfo("所有传送位置已重置，可以重新保存坐标 | All teleport positions reset, you can save new coordinates");
        }
        catch (Exception ex)
        {
            Logger?.LogError($"清空存档坐标时发生错误 | Error clearing save slots: {ex.Message}");
        }
    }

    // 处理紧急重启输入（独立方法，优先级最高）
    private void HandleEmergencyRestartInput()
    {
        try
        {
            // 紧急返回主菜单：Ctrl+F9（固定按键）
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F9))
            {
                EmergencyReturnToMainMenu();
                return;
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"处理紧急重启输入时发生错误 | Error handling emergency restart input: {ex.Message}");
        }
    }

    // 紧急返回主菜单功能
    private void EmergencyReturnToMainMenu()
    {
        try
        {
            Logger?.LogWarning("=== 紧急返回主菜单 | EMERGENCY RETURN TO MAIN MENU ===");
            Logger?.LogWarning("正在强制返回主菜单，不保存当前进度！| Force returning to main menu without saving current progress!");

            // 检查GameManager是否可用
            if (GameManager.instance == null)
            {
                Logger?.LogError("GameManager实例未找到，无法返回主菜单 | GameManager instance not found, cannot return to main menu");
                return;
            }

            // 使用GameManager的不保存返回主菜单方法
            GameManager.instance.ReturnToMainMenuNoSave();
            Logger?.LogInfo("已触发返回主菜单 | Return to main menu triggered");
        }
        catch (Exception ex)
        {
            Logger?.LogError($"紧急返回主菜单时发生错误 | Error during emergency return to main menu: {ex.Message}");
        }
    }

    // 直接传送到椅子功能
    private void TeleportToBench()
    {
        try
        {
            if (HeroController.instance == null || GameManager.instance == null)
            {
                Logger?.LogWarning("HeroController 或 GameManager 未找到，无法传送到椅子");
                return;
            }

            LogInfo("=== 传送到椅子 | TELEPORT TO BENCH ===");

            // 获取椅子位置信息
            var benchInfo = GetBenchPositionAndScene();
            if (benchInfo.position == Vector3.zero || string.IsNullOrEmpty(benchInfo.scene))
            {
                Logger?.LogWarning("未找到有效的椅子位置或场景信息 | No valid bench position or scene found");
                return;
            }

            string currentScene = GameManager.instance.sceneName;
            LogInfo($"准备传送到椅子: {benchInfo.position} 在场景: {benchInfo.scene}");

            // 检查是否需要切换场景
            if (!string.IsNullOrEmpty(benchInfo.scene) && currentScene != benchInfo.scene)
            {
                LogInfo($"需要切换场景传送到椅子: {currentScene} -> {benchInfo.scene}");
                StartCoroutine(TeleportWithSceneChange(benchInfo.scene, benchInfo.position));
            }
            else
            {
                // 在同一场景，直接传送
                LogInfo("在当前场景传送到椅子");
                PerformTeleport(benchInfo.position);
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"传送到椅子时发生错误 | Error during teleport to bench: {ex.Message}");
        }
    }


    // 将字符串转换为KeyCode
    private KeyCode ParseKeyCode(string keyString)
    {
        try
        {
            return (KeyCode)System.Enum.Parse(typeof(KeyCode), keyString, true);
        }
        catch
        {
            Logger?.LogWarning($"无法解析按键设置: {keyString}，使用默认值 | Cannot parse key setting: {keyString}, using default");
            return KeyCode.None;
        }
    }

    // 检查修饰键是否被按下
    private bool IsModifierKeyPressed(string modifierKeyString)
    {
        KeyCode keyCode = ParseKeyCode(modifierKeyString);
        if (keyCode == KeyCode.None) return false;

        return Input.GetKey(keyCode);
    }

    // 获取配置的存档槽按键
    private KeyCode GetSlotKey(int slotNumber)
    {
        string keyString = slotNumber switch
        {
            1 => slot1Key?.Value ?? "Alpha1",
            2 => slot2Key?.Value ?? "Alpha2",
            3 => slot3Key?.Value ?? "Alpha3",
            4 => slot4Key?.Value ?? "Alpha4",
            5 => slot5Key?.Value ?? "Alpha5",
            _ => "None"
        };
        return ParseKeyCode(keyString);
    }


    // Alt+6 功能：重新进入当前场景的安全入口点
    private void RespawnToSafeEntryPoint()
    {
        try
        {
            if (HeroController.instance == null || GameManager.instance == null)
            {
                Logger?.LogWarning("HeroController 或 GameManager 未找到，无法执行安全重生");
                return;
            }

            string currentScene = GameManager.instance.sceneName;
            LogInfo($"正在重新进入当前场景的安全入口点: {currentScene}");

            // 获取当前场景的下一个安全入口点（轮换）
            string? safeEntryPoint = GetNextSafeEntryPointForCurrentScene();
            if (string.IsNullOrEmpty(safeEntryPoint))
            {
                Logger?.LogWarning("未找到当前场景的安全入口点，使用椅子位置");
                var benchInfo = GetBenchPositionAndScene();
                if (benchInfo.position != Vector3.zero && !string.IsNullOrEmpty(benchInfo.scene))
                {
                    if (benchInfo.scene == currentScene)
                    {
                        PerformTeleport(benchInfo.position);
                    }
                    else
                    {
                        StartCoroutine(TeleportWithSceneChange(benchInfo.scene, benchInfo.position));
                    }
                }
                return;
            }

            // 使用轮换选择的安全入口点重新进入当前场景
            LogInfo($"使用安全入口点 {currentEntryPointIndex}: {safeEntryPoint}");
            StartCoroutine(TeleportWithSceneChange(currentScene, Vector3.zero, safeEntryPoint));
        }
        catch (Exception ex)
        {
            Logger?.LogError($"执行安全重生时发生错误: {ex.Message}");
        }
    }

    // Alt+- 功能：传送到硬编码的预设坐标
    private void TeleportToHardcodedPosition()
    {
        try
        {
            if (HeroController.instance == null || GameManager.instance == null)
            {
                Logger?.LogWarning("HeroController 或 GameManager 未找到，无法执行硬编码传送");
                return;
            }

            // 硬编码的预设坐标
            Vector3 targetPosition = new Vector3(71.42231f, 9.597684f, 0.004f);
            string targetScene = "Bellway_01";

            LogInfo($"执行硬编码传送到: {targetPosition} 在场景: {targetScene}");

            // 检查是否需要切换场景
            string currentScene = GameManager.instance.sceneName;
            if (!string.IsNullOrEmpty(targetScene) && currentScene != targetScene)
            {
                LogInfo($"需要切换场景进行硬编码传送: {currentScene} -> {targetScene}");
                StartCoroutine(TeleportWithSceneChange(targetScene, targetPosition));
            }
            else
            {
                // 在同一场景，直接传送
                LogInfo("在当前场景执行硬编码传送");
                PerformTeleport(targetPosition);
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"执行硬编码传送时发生错误: {ex.Message}");
        }
    }

    // 检查并修复当前场景的位置安全性
    private Vector3 CheckAndFixPositionInCurrentScene(Vector3 targetPosition, int slotNumber)
    {
        try
        {
            if (HeroController.instance == null) return targetPosition;

            // 检查位置是否安全
            if (IsPositionSafe(targetPosition))
            {
                LogInfo($"档位 {slotNumber} 位置安全: {targetPosition}");
                return targetPosition;
            }

            // 位置不安全，寻找附近的安全位置
            Logger?.LogWarning($"档位 {slotNumber} 位置不安全，正在查找安全位置: {targetPosition}");
            Vector3 safePosition = FindSafePositionNearby(targetPosition);

            if (safePosition != Vector3.zero)
            {
                // 找到安全位置，更新存档槽
                string currentScene = GameManager.instance.sceneName;
                saveSlots[slotNumber] = new SaveSlot(safePosition, currentScene);
                LogInfo($"档位 {slotNumber} 已修正为安全位置: {targetPosition} -> {safePosition}");
                return safePosition;
            }
            else
            {
                Logger?.LogWarning($"档位 {slotNumber} 无法找到安全位置，将在传送后尝试修复");
                return targetPosition;
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"检查位置安全性时发生错误: {ex.Message}");
            return targetPosition;
        }
    }

    // 检查位置是否安全（不会卡在地形中）
    private bool IsPositionSafe(Vector3 position)
    {
        try
        {
            var heroCollider = HeroController.instance?.GetComponent<Collider2D>();
            if (heroCollider == null) return true; // 如果获取不到碰撞器，假设安全

            var groundLayerMask = LayerMask.GetMask("Terrain");

            // 检查该位置是否与地形重叠
            var overlap = Physics2D.OverlapBox(
                position,
                heroCollider.bounds.size,
                0f,
                groundLayerMask
            );

            bool isSafe = overlap == null;
            LogInfo($"位置安全检查: {position} -> {(isSafe ? "安全" : "不安全")}");
            return isSafe;
        }
        catch (Exception ex)
        {
            Logger?.LogError($"检查位置安全性时发生错误: {ex.Message}");
            return true; // 出错时假设安全
        }
    }

    // 获取当前场景的下一个安全入口点（轮换模式）
    private string? GetNextSafeEntryPointForCurrentScene()
    {
        try
        {
            var allSafeEntryPoints = GetAllSafeEntryPointsForCurrentScene();
            if (allSafeEntryPoints == null || allSafeEntryPoints.Count == 0)
            {
                LogInfo("当前场景没有可用的安全入口点");
                return null;
            }

            // 循环选择入口点
            if (currentEntryPointIndex >= allSafeEntryPoints.Count)
            {
                currentEntryPointIndex = 0; // 回到开头
            }

            string selectedEntryPoint = allSafeEntryPoints[currentEntryPointIndex];
            LogInfo($"选择安全入口点 {currentEntryPointIndex + 1}/{allSafeEntryPoints.Count}: {selectedEntryPoint}");

            // 为下次使用准备索引
            currentEntryPointIndex++;

            return selectedEntryPoint;
        }
        catch (Exception ex)
        {
            Logger?.LogError($"获取下一个安全入口点时发生错误: {ex.Message}");
            return null;
        }
    }

    // 获取当前场景所有可用的安全入口点列表
    private List<string>? GetAllSafeEntryPointsForCurrentScene()
    {
        try
        {
            // 获取当前场景的所有TransitionPoint
            var transitionPoints = TransitionPoint.TransitionPoints;
            if (transitionPoints == null || transitionPoints.Count == 0)
            {
                LogInfo("当前场景没有TransitionPoint");
                return null;
            }

            var safeEntryPoints = new List<string>();

            // 优先添加门类型的入口点
            var doorEntries = transitionPoints.Where(tp => tp != null &&
                tp.name.Contains("door") &&
                !tp.isInactive).ToList();

            foreach (var door in doorEntries)
            {
                safeEntryPoints.Add(door.name);
                LogInfo($"找到门入口点: {door.name}");
            }

            // 然后添加其他方向入口点
            var otherEntries = transitionPoints.Where(tp => tp != null &&
                !tp.isInactive &&
                !tp.name.Contains("door") &&
                (tp.name.Contains("left") || tp.name.Contains("right") ||
                 tp.name.Contains("top") || tp.name.Contains("bot"))).ToList();

            foreach (var entry in otherEntries)
            {
                safeEntryPoints.Add(entry.name);
                LogInfo($"找到其他入口点: {entry.name}");
            }

            // 如果还是没有，添加所有可用的入口点
            if (safeEntryPoints.Count == 0)
            {
                var allAvailable = transitionPoints.Where(tp => tp != null && !tp.isInactive).ToList();
                foreach (var tp in allAvailable)
                {
                    safeEntryPoints.Add(tp.name);
                    LogInfo($"找到可用入口点: {tp.name}");
                }
            }

            LogInfo($"总共找到 {safeEntryPoints.Count} 个安全入口点");
            return safeEntryPoints.Count > 0 ? safeEntryPoints : null;
        }
        catch (Exception ex)
        {
            Logger?.LogError($"获取所有安全入口点时发生错误: {ex.Message}");
            return null;
        }
    }

    // 获取当前场景的安全入口点（保持原有方法用于其他地方）
    private string? GetSafeEntryPointForCurrentScene()
    {
        try
        {
            // 获取当前场景的所有TransitionPoint
            var transitionPoints = TransitionPoint.TransitionPoints;
            if (transitionPoints == null || transitionPoints.Count == 0)
            {
                LogInfo("当前场景没有TransitionPoint");
                return null;
            }

            // 优先查找门类型的入口点
            var doorEntries = transitionPoints.Where(tp => tp != null &&
                tp.name.Contains("door") &&
                !tp.isInactive).ToList();

            if (doorEntries.Count > 0)
            {
                LogInfo($"找到门入口点: {doorEntries[0].name}");
                return doorEntries[0].name;
            }

            // 如果没有门，查找其他入口点（left, right, top, bottom）
            var otherEntries = transitionPoints.Where(tp => tp != null &&
                !tp.isInactive &&
                (tp.name.Contains("left") || tp.name.Contains("right") ||
                 tp.name.Contains("top") || tp.name.Contains("bot"))).ToList();

            if (otherEntries.Count > 0)
            {
                LogInfo($"找到其他入口点: {otherEntries[0].name}");
                return otherEntries[0].name;
            }

            // 最后fallback到第一个可用的入口点
            var firstAvailable = transitionPoints.FirstOrDefault(tp => tp != null && !tp.isInactive);
            if (firstAvailable != null)
            {
                LogInfo($"使用第一个可用入口点: {firstAvailable.name}");
                return firstAvailable.name;
            }

            return null;
        }
        catch (Exception ex)
        {
            Logger?.LogError($"获取安全入口点时发生错误: {ex.Message}");
            return null;
        }
    }

    private void SaveToSlot(int slotNumber)
    {
        try
        {
            if (HeroController.instance != null && GameManager.instance != null)
            {
                Vector3 currentPosition = HeroController.instance.transform.position;
                string currentScene = GameManager.instance.sceneName;

                saveSlots[slotNumber] = new SaveSlot(currentPosition, currentScene);
                LogInfo($"档位 {slotNumber} 已保存: {currentPosition} 在场景: {currentScene}");

                // 播放存档音效提示
                PlaySaveSound();

                // 保存持久化数据
                SavePersistentData();
            }
            else
            {
                Logger?.LogWarning("HeroController 或 GameManager 未找到，无法保存位置");
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"保存档位 {slotNumber} 时发生错误: {ex.Message}");
        }
    }

    // 优化后的音频播放 - 使用缓存避免重复加载
    private void PlaySaveSound()
    {
        try
        {
            // 检查音量设置，如果为0则不播放音效
            if (audioVolume?.Value <= 0f)
            {
                LogInfo("音效音量设置为0，跳过音效播放");
                return;
            }

            // 检查音频播放冷却时间，防止快速连续播放
            float currentTime = Time.time;
            if (currentTime - lastSaveAudioTime < AUDIO_COOLDOWN)
            {
                LogInfo("音频播放在冷却中，跳过此次播放");
                return;
            }
            lastSaveAudioTime = currentTime;

            // 确保音频播放器存在
            EnsureAudioPlayer();

            // 使用缓存的AudioClip
            if (cachedSaveAudioClip != null && audioPlayerSource != null)
            {
                // 使用PlayOneShot避免中断当前播放，使用配置的音量
                audioPlayerSource.PlayOneShot(cachedSaveAudioClip, audioVolume?.Value ?? 0.5f);
                LogInfo($"使用缓存音频播放存档音效，音量: {audioVolume?.Value ?? 0.5f}");
            }
            else
            {
                LogInfo("音频未预加载完成，跳过播放");
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"播放存档音效时发生错误: {ex.Message}");
        }
    }

    // 确保音频播放器存在（复用机制）
    private void EnsureAudioPlayer()
    {
        if (audioPlayerObject == null || audioPlayerSource == null)
        {
            // 创建新的音频播放器（只创建一次）
            audioPlayerObject = new GameObject("TeleportAudioPlayer");
            audioPlayerSource = audioPlayerObject.AddComponent<AudioSource>();

            // 配置为2D音频
            audioPlayerSource.volume = audioVolume?.Value ?? 0.5f;
            audioPlayerSource.spatialBlend = 0f; // 2D音频
            audioPlayerSource.playOnAwake = false;
            audioPlayerSource.loop = false;

            // 设置为不销毁，可以复用
            UnityEngine.Object.DontDestroyOnLoad(audioPlayerObject);
            LogInfo("创建并复用音频播放器对象");
        }
    }

    // 预加载音频文件协程 - 启动时一次性加载
    private IEnumerator PreloadAudioClip()
    {
        LogInfo("开始预加载音频文件...");

        // 根据配置选择音频文件名（去掉Teleport.前缀）
        var fileName = (enableEasterEggAudio?.Value == true) ? "manbo.wav" : "Gamesave.wav";

        // 获取当前DLL所在的目录（BepInEx\plugins或其子目录）
        var assembly = Assembly.GetExecutingAssembly();
        string dllDirectory = Path.GetDirectoryName(assembly.Location);
        string audioFilePath = Path.Combine(dllDirectory, fileName);
        LogInfo($"选择音频文件: {fileName} (彩蛋音效: {enableEasterEggAudio?.Value})");
        LogInfo($"音频文件路径: {audioFilePath}");

        string tempPath = "";

        try
        {
            // 检查音频文件是否存在
            if (!File.Exists(audioFilePath))
            {
                Logger?.LogWarning($"未找到音频文件: {audioFilePath}");
                yield break;
            }

            // 直接读取音频文件
            byte[] audioData = File.ReadAllBytes(audioFilePath);
            LogInfo($"成功读取音频文件，大小: {audioData.Length} 字节");

            // 创建临时文件用于加载（只在预加载时创建一次）
            tempPath = Path.GetTempFileName() + ".wav";
            File.WriteAllBytes(tempPath, audioData);
        }
        catch (Exception ex)
        {
            Logger?.LogError($"读取音频文件时发生错误: {ex.Message}");
            yield break;
        }

        // 使用UnityWebRequest加载音频（在try-catch外部）
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.WAV))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                cachedSaveAudioClip = DownloadHandlerAudioClip.GetContent(request);
                if (cachedSaveAudioClip != null)
                {
                    // 设置音频不销毁，保持缓存
                    UnityEngine.Object.DontDestroyOnLoad(cachedSaveAudioClip);
                    LogInfo($"音频预加载成功 - 长度: {cachedSaveAudioClip.length}秒, 频率: {cachedSaveAudioClip.frequency}, 声道: {cachedSaveAudioClip.channels}");
                }
                else
                {
                    Logger?.LogWarning("无法获取AudioClip");
                }
            }
            else
            {
                Logger?.LogError($"预加载音频失败: {request.error}");
            }
        }

        // 清理临时文件
        try
        {
            if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
            {
                File.Delete(tempPath);
                LogInfo("已清理预加载临时文件");
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"删除预加载临时文件失败: {ex.Message}");
        }
    }

    private void LoadFromSlot(int slotNumber)
    {
        try
        {
            if (HeroController.instance == null || GameManager.instance == null)
            {
                Logger?.LogWarning("HeroController 或 GameManager 未找到，无法传送");
                return;
            }

            Vector3 targetPosition;
            string targetScene;

            // 检查指定档位是否有存档数据
            if (saveSlots.ContainsKey(slotNumber) && saveSlots[slotNumber].hasData)
            {
                // 有存档数据，传送到存档位置
                var slot = saveSlots[slotNumber];
                targetPosition = slot.position;
                targetScene = slot.scene;
                LogInfo($"准备传送到档位 {slotNumber}: {targetPosition} 在场景: {targetScene}");
            }
            else
            {
                // 没有存档数据，回退到椅子传送逻辑
                LogInfo($"档位 {slotNumber} 没有存档数据，传送到椅子位置");
                var benchInfo = GetBenchPositionAndScene();
                targetPosition = benchInfo.position;
                targetScene = benchInfo.scene;

                if (targetPosition == Vector3.zero || string.IsNullOrEmpty(targetScene))
                {
                    Logger?.LogWarning("未找到有效的椅子位置或场景信息");
                    return;
                }
                LogInfo($"准备传送到椅子位置: {targetPosition} 在场景: {targetScene}");
            }

            // 检查是否需要切换场景
            string currentScene = GameManager.instance.sceneName;
            if (!string.IsNullOrEmpty(targetScene) && currentScene != targetScene)
            {
                LogInfo($"需要切换场景: {currentScene} -> {targetScene}");
                StartCoroutine(TeleportWithSceneChange(targetScene, targetPosition));
            }
            else
            {
                // 在同一场景，先检查位置安全性
                Vector3 safePosition = CheckAndFixPositionInCurrentScene(targetPosition, slotNumber);
                // 已经预先检查过安全性，直接传送，无需重复检查
                PerformTeleport(safePosition);
            }

        }
        catch (Exception ex)
        {
            Logger?.LogError($"从档位 {slotNumber} 传送时发生错误: {ex.Message}");
        }
    }

    private (Vector3 position, string scene) GetBenchPositionAndScene()
    {
        try
        {
            if (PlayerData.instance == null)
            {
                Logger?.LogWarning("PlayerData 未找到");
                return (Vector3.zero, "");
            }

            string respawnMarkerName = PlayerData.instance.respawnMarkerName;
            string respawnScene = PlayerData.instance.respawnScene;

            if (string.IsNullOrEmpty(respawnMarkerName) || string.IsNullOrEmpty(respawnScene))
            {
                Logger?.LogWarning("未找到椅子标记名称或场景信息");
                return (Vector3.zero, "");
            }

            LogInfo($"查找椅子: {respawnMarkerName} 在场景: {respawnScene}");

            // 检查椅子是否在当前场景
            string currentScene = GameManager.instance?.sceneName ?? "";
            if (currentScene == respawnScene)
            {
                // 椅子在当前场景，直接查找位置
                if (RespawnMarker.Markers != null)
                {
                    var targetMarker = RespawnMarker.Markers
                        .FirstOrDefault(marker => marker != null && marker.gameObject.name == respawnMarkerName);

                    if (targetMarker != null)
                    {
                        LogInfo($"在当前场景找到椅子: {targetMarker.gameObject.name} 位置: {targetMarker.transform.position}");
                        return (targetMarker.transform.position, respawnScene);
                    }
                }
                Logger?.LogWarning($"在当前场景中未找到椅子标记: {respawnMarkerName}");
                return (Vector3.zero, "");
            }
            else
            {
                // 椅子在其他场景，返回场景信息，坐标将在场景切换后获取
                LogInfo($"椅子在其他场景: {respawnScene}，需要切换场景后获取坐标");
                return (Vector3.one, respawnScene); // 使用 Vector3.one 作为占位符，表示需要场景切换后获取真实坐标
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"获取椅子位置时发生错误: {ex.Message}");
            return (Vector3.zero, "");
        }
    }

    // 场景切换传送的重载方法
    private IEnumerator TeleportWithSceneChange(string targetScene, Vector3 targetPosition)
    {
        yield return StartCoroutine(TeleportWithSceneChange(targetScene, targetPosition, null));
    }

    // 改进的场景切换传送方法，支持指定入口点
    private IEnumerator TeleportWithSceneChange(string targetScene, Vector3 targetPosition, string? entryPointName)
    {
        LogInfo($"开始场景切换到: {targetScene}");

        // 确定使用的入口点
        string? useEntryPoint = entryPointName;
        if (string.IsNullOrEmpty(useEntryPoint))
        {
            // 如果没有指定入口点，尝试智能选择
            useEntryPoint = GetBestEntryPointForScene(targetScene);
        }

        LogInfo($"使用入口点: {useEntryPoint}");

        // 使用 GameManager 的场景切换功能
        try
        {
            GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
            {
                SceneName = targetScene,
                EntryGateName = useEntryPoint,
                HeroLeaveDirection = GlobalEnums.GatePosition.unknown,
                EntryDelay = 0f,
                Visualization = GameManager.SceneLoadVisualizations.Default,
                AlwaysUnloadUnusedAssets = true
            });
        }
        catch (Exception ex)
        {
            Logger?.LogError($"开始场景切换时发生错误: {ex.Message}");
            yield break;
        }

        // 等待场景加载完成
        yield return new WaitWhile(() => GameManager.instance != null && GameManager.instance.IsInSceneTransition);

        // 等待额外一小段时间确保所有组件都初始化完成
        yield return new WaitForSeconds(0.5f);

        // 场景切换完成后，处理目标位置
        try
        {
            Vector3 finalPosition = targetPosition;

            // 如果是椅子传送且使用了占位符坐标，需要重新获取真实坐标
            if (targetPosition == Vector3.one)
            {
                LogInfo("获取椅子在新场景中的真实坐标");
                var benchInfo = GetBenchPositionAndScene();
                if (benchInfo.position != Vector3.zero && benchInfo.position != Vector3.one)
                {
                    finalPosition = benchInfo.position;
                    LogInfo($"找到椅子坐标: {finalPosition}");
                }
                else
                {
                    Logger?.LogError("场景切换后仍无法找到椅子坐标，使用入口点位置");
                    // 如果找不到椅子坐标，直接使用场景入口点，不再进行额外传送
                    yield break;
                }
            }

            // 如果目标位置是Vector3.zero，说明是Alt+6功能，使用入口点位置，无需额外传送
            if (targetPosition != Vector3.zero)
            {
                LogInfo($"场景切换完成，传送到位置: {finalPosition}");
                PerformSafeTeleport(finalPosition);
            }
            else
            {
                LogInfo("场景切换完成，已在安全入口点位置");
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"传送到目标位置时发生错误: {ex.Message}");
        }
    }

    // 智能选择最佳入口点
    private string GetBestEntryPointForScene(string sceneName)
    {
        try
        {
            // 常见的安全入口点名称列表（按优先级排序）
            string[] commonEntryPoints = { "door1", "door_entrance", "entrance", "left1", "right1", "top1", "bot1" };

            foreach (string entryPoint in commonEntryPoints)
            {
                // 这里可以根据需要添加更复杂的逻辑
                // 比如检查特定场景的已知入口点
                LogInfo($"尝试使用入口点: {entryPoint}");
                return entryPoint;
            }

            // 如果都没有找到，返回默认值，GameManager会fallback到第一个可用的
            return "door1";
        }
        catch (Exception ex)
        {
            Logger?.LogError($"选择最佳入口点时发生错误: {ex.Message}");
            return "door1";
        }
    }

    // 安全传送方法，包含位置验证和错误恢复
    private void PerformSafeTeleport(Vector3 targetPosition)
    {
        try
        {
            if (HeroController.instance == null)
            {
                Logger?.LogWarning("HeroController 未找到，无法执行传送");
                return;
            }

            // 执行传送
            PerformTeleport(targetPosition);

            // 等待一帧后检查是否卡在地里
            StartCoroutine(CheckTeleportSafety(targetPosition));
        }
        catch (Exception ex)
        {
            Logger?.LogError($"执行安全传送时发生错误: {ex.Message}");
        }
    }

    // 检查传送后的安全性
    private IEnumerator CheckTeleportSafety(Vector3 originalPosition)
    {
        yield return new WaitForSeconds(0.1f); // 等待物理系统稳定

        try
        {
            if (HeroController.instance == null) yield break;

            // 检查角色是否卡在固体碰撞器中
            var heroCollider = HeroController.instance.GetComponent<Collider2D>();
            if (heroCollider == null) yield break;

            // 检查是否与地形碰撞
            var groundLayerMask = LayerMask.GetMask("Terrain");
            var overlapping = Physics2D.OverlapBox(
                heroCollider.bounds.center,
                heroCollider.bounds.size,
                0f,
                groundLayerMask
            );

            if (overlapping != null)
            {
                Logger?.LogWarning("检测到传送后卡在地形中，尝试修复位置");

                // 尝试向上移动角色到安全位置
                Vector3 safePosition = FindSafePositionNearby(originalPosition);
                if (safePosition != Vector3.zero)
                {
                    PerformTeleport(safePosition);
                    LogInfo($"已修复到安全位置: {safePosition}");
                }
                else
                {
                    Logger?.LogWarning("无法找到安全位置，建议使用Alt+6重新进入场景");
                }
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"检查传送安全性时发生错误: {ex.Message}");
        }
    }

    // 在附近查找安全位置
    private Vector3 FindSafePositionNearby(Vector3 originalPosition)
    {
        try
        {
            var heroCollider = HeroController.instance?.GetComponent<Collider2D>();
            if (heroCollider == null) return Vector3.zero;

            var groundLayerMask = LayerMask.GetMask("Terrain");

            // 尝试向上、左、右偏移查找安全位置
            Vector3[] offsets = {
                new Vector3(0, 2f, 0),   // 向上
                new Vector3(0, 4f, 0),   // 向上更远
                new Vector3(-1f, 2f, 0), // 左上
                new Vector3(1f, 2f, 0),  // 右上
                new Vector3(-2f, 0, 0),  // 左侧
                new Vector3(2f, 0, 0),   // 右侧
            };

            foreach (var offset in offsets)
            {
                Vector3 testPosition = originalPosition + offset;

                var overlap = Physics2D.OverlapBox(
                    testPosition,
                    heroCollider.bounds.size,
                    0f,
                    groundLayerMask
                );

                if (overlap == null)
                {
                    LogInfo($"找到安全位置偏移: {offset}");
                    return testPosition;
                }
            }

            return Vector3.zero;
        }
        catch (Exception ex)
        {
            Logger?.LogError($"查找安全位置时发生错误: {ex.Message}");
            return Vector3.zero;
        }
    }

    // 原始传送方法，供内部使用
    private void PerformTeleport(Vector3 targetPosition)
    {
        try
        {
            if (HeroController.instance == null)
            {
                Logger?.LogWarning("HeroController 未找到，无法执行传送");
                return;
            }

            // 执行传送
            HeroController.instance.transform.position = targetPosition;

            // 重置物理速度，避免传送后继续移动
            var rb2d = HeroController.instance.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                rb2d.linearVelocity = Vector2.zero;
            }

            // 重置一些可能导致问题的状态
            if (HeroController.instance.cState != null)
            {
                HeroController.instance.cState.recoiling = false;
                HeroController.instance.cState.transitioning = false;
            }

            LogInfo($"传送完成: {targetPosition}");
        }
        catch (Exception ex)
        {
            Logger?.LogError($"执行传送时发生错误: {ex.Message}");
        }
    }
}
