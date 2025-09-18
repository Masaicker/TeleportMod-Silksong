using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Logging;
using GlobalEnums;
using static TeleportMod;

public class TeleportUIManager : MonoBehaviour
{
    private static TeleportUIManager? _instance;
    public static TeleportUIManager? Instance => _instance;

    private static ManualLogSource? Logger;

    // UI组件引用
    private GameObject? uiCanvas;
    private GameObject? mainPanel;
    private ScrollRect? scrollView;
    private Transform? saveSlotContainer;
    private Button? closeButton;
    private Text? currentInfoText;

    // UI状态
    private bool isUIVisible = false;

    // 状态检测计时器
    private float lastStateCheckTime = 0f;
    private const float STATE_CHECK_INTERVAL = 0.5f; // 每0.5秒检测一次

    // 重复使用的确认对话框
    private GameObject? confirmDialog;
    private Text? confirmTitleText;
    private Text? confirmMessageText;
    private Button? confirmYesButton;
    private Button? confirmNoButton;

    // 回调事件
    public event Action<string>? OnTeleportToSlot;
    public event Action<string>? OnDeleteSlot;
    public event Action<string>? OnOverwriteSlot;
    public event Action? OnSaveCurrentPosition;
    public event Action? OnClearAllSlots;
    public event Action? OnSafeRespawn;
    public event Action? OnBenchTeleport;
    public event Action? OnHardcodedTeleport;


    public static void Initialize(ManualLogSource? logger)
    {
        Logger = logger;

        if (_instance == null)
        {
            var go = new GameObject("TeleportUIManager");
            _instance = go.AddComponent<TeleportUIManager>();
            DontDestroyOnLoad(go);
            _instance.CreateUI();
            LogInfo("TeleportUIManager 已初始化");
        }
    }

    private void CreateUI()
    {
        try
        {
            // 创建Canvas
            CreateCanvas();

            // 创建主面板
            CreateMainPanel();

            // 创建UI元素
            CreateUIElements();

            // 默认隐藏UI
            SetUIVisible(false);

            LogInfo("传送UI界面创建完成");
        }
        catch (Exception ex)
        {
            Logger?.LogError($"创建UI时发生错误: {ex.Message}");
        }
    }

    private void CreateCanvas()
    {
        uiCanvas = new GameObject("TeleportCanvas");
        DontDestroyOnLoad(uiCanvas);

        var canvas = uiCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 确保在最前面

        var canvasScaler = uiCanvas.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        uiCanvas.AddComponent<GraphicRaycaster>();

        // 智能EventSystem检测 - 确保UI交互可用
        EnsureEventSystemAvailable();
    }

    // 确保EventSystem可用 - 调用游戏原生激活方法
    private void EnsureEventSystemAvailable()
    {
        try
        {
            var gameManager = GameManager.instance;
            if (gameManager?.inputHandler != null)
            {
                gameManager.inputHandler.StartUIInput();
                LogInfo("已激活EventSystem，UI交互应该可用");
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"激活EventSystem时发生错误: {ex.Message}");
        }
    }

    private void CreateMainPanel()
    {
        mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(uiCanvas?.transform, false);

        var mainRect = mainPanel.AddComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0.15f, 0.1f);
        mainRect.anchorMax = new Vector2(0.85f, 0.9f);
        mainRect.offsetMin = Vector2.zero;
        mainRect.offsetMax = Vector2.zero;

        // 主面板背景 - 深灰色背景
        var mainImage = mainPanel.AddComponent<Image>();
        mainImage.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        mainImage.raycastTarget = true;
    }

    private void CreateUIElements()
    {
        // 标题区域
        CreateTitleArea();

        // 当前信息显示
        CreateCurrentInfoArea();

        // 按钮区域
        CreateButtonArea();

        // 滚动视图
        CreateScrollView();

        // 关闭按钮
        CreateCloseButton();

        // 重复使用的确认对话框
        CreateReusableConfirmDialog();
    }

    private void CreateTitleArea()
    {
        var titleAreaGO = new GameObject("TitleArea");
        titleAreaGO.transform.SetParent(mainPanel?.transform, false);

        var titleAreaRect = titleAreaGO.AddComponent<RectTransform>();
        titleAreaRect.anchorMin = new Vector2(0.05f, 0.95f);
        titleAreaRect.anchorMax = new Vector2(0.8f, 1f);
        titleAreaRect.offsetMin = Vector2.zero;
        titleAreaRect.offsetMax = Vector2.zero;

        // 标题背景 - 纯黑背景
        var titleBg = titleAreaGO.AddComponent<Image>();
        titleBg.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

        // 标题文本 - 纯白文字
        CreateStyledText(titleAreaGO.transform, "TitleText",
            "传送位置管理器 | Teleport Manager", 24, FontStyle.Bold,
            new Vector2(0.02f, 0), new Vector2(0.98f, 1),
            new Color(1f, 1f, 1f, 1f), TextAnchor.MiddleCenter);
    }

    private void CreateCurrentInfoArea()
    {
        var infoAreaGO = new GameObject("CurrentInfoArea");
        infoAreaGO.transform.SetParent(mainPanel?.transform, false);

        var infoAreaRect = infoAreaGO.AddComponent<RectTransform>();
        infoAreaRect.anchorMin = new Vector2(0.05f, 0.91f);  // 大胆缩小间距，几乎紧贴标题
        infoAreaRect.anchorMax = new Vector2(0.95f, 0.945f); // 缩小高度，更紧凑
        infoAreaRect.offsetMin = Vector2.zero;
        infoAreaRect.offsetMax = Vector2.zero;

        // 信息区域背景 - 深灰背景
        var infoBg = infoAreaGO.AddComponent<Image>();
        infoBg.color = new Color(0.18f, 0.18f, 0.18f, 0.9f);

        currentInfoText = CreateStyledText(infoAreaGO.transform, "CurrentInfoText",
            "当前位置: 获取中... | Current Position: Loading...", 16, FontStyle.Normal,
            new Vector2(0.02f, 0), new Vector2(0.98f, 1),
            new Color(0.95f, 0.95f, 0.95f, 1f), TextAnchor.MiddleCenter);
    }

    private void CreateButtonArea()
    {
        // 扩展按钮区域以容纳更多按钮
        var buttonAreaGO = new GameObject("ButtonArea");
        buttonAreaGO.transform.SetParent(mainPanel?.transform, false);

        var buttonAreaRect = buttonAreaGO.AddComponent<RectTransform>();
        buttonAreaRect.anchorMin = new Vector2(0.05f, 0.79f);  // 进一步向上调整，更靠近当前位置区域
        buttonAreaRect.anchorMax = new Vector2(0.95f, 0.935f); // 进一步向上调整，更靠近当前位置区域
        buttonAreaRect.offsetMin = Vector2.zero;
        buttonAreaRect.offsetMax = Vector2.zero;

        // 第一行：保存和清空按钮
        CreateStyledButton(buttonAreaGO.transform, "SaveCurrentButton",
            "保存当前位置 | Save Current", new Vector2(0f, 0.45f), new Vector2(0.48f, 0.78f),
            new Color(0.25f, 0.25f, 0.25f, 1f),      // 深灰色
            new Color(0.35f, 0.35f, 0.35f, 1f),      // 悬停时稍亮
            () => OnSaveCurrentPosition?.Invoke());

        CreateStyledButton(buttonAreaGO.transform, "ClearAllButton",
            "清空所有 | Clear All", new Vector2(0.52f, 0.45f), new Vector2(1f, 0.78f),
            new Color(0.45f, 0.15f, 0.15f, 1f),      // 红色突出危险操作
            new Color(0.55f, 0.25f, 0.25f, 1f),      // 悬停时稍亮
            () => ConfirmClearAll());

        // 第二行：特殊传送功能
        CreateStyledButton(buttonAreaGO.transform, "SafeRespawnButton",
            "安全重生 | Safe Respawn", new Vector2(0f, 0.05f), new Vector2(0.32f, 0.38f),
            new Color(0.28f, 0.28f, 0.28f, 1f),      // 深灰色
            new Color(0.38f, 0.38f, 0.38f, 1f),      // 悬停时稍亮
            () => OnSafeRespawn?.Invoke());

        CreateStyledButton(buttonAreaGO.transform, "BenchTeleportButton",
            "传送到椅子 | To Bench", new Vector2(0.34f, 0.05f), new Vector2(0.66f, 0.38f),
            new Color(0.32f, 0.32f, 0.32f, 1f),      // 中灰色
            new Color(0.42f, 0.42f, 0.42f, 1f),      // 悬停时稍亮
            () => OnBenchTeleport?.Invoke());

        CreateStyledButton(buttonAreaGO.transform, "HardcodedTeleportButton",
            "预设传送 | Preset", new Vector2(0.68f, 0.05f), new Vector2(1f, 0.38f),
            new Color(0.3f, 0.3f, 0.3f, 1f),         // 中深灰色
            new Color(0.4f, 0.4f, 0.4f, 1f),         // 悬停时稍亮
            () => OnHardcodedTeleport?.Invoke());
    }

    private void CreateScrollView()
    {
        var scrollViewGO = new GameObject("ScrollView");
        scrollViewGO.transform.SetParent(mainPanel?.transform, false);

        var scrollRect = scrollViewGO.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.05f, 0.05f);  // 进一步向下扩展，增加整体高度
        scrollRect.anchorMax = new Vector2(0.95f, 0.76f);  // 稍微向上调整，与按钮区域保持适当距离
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        // 滚动视图背景 - 深灰色背景
        var scrollBg = scrollViewGO.AddComponent<Image>();
        scrollBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        // 添加遮罩组件来约束内容
        var mask = scrollViewGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        scrollView = scrollViewGO.AddComponent<ScrollRect>();
        scrollView.horizontal = false;
        scrollView.vertical = true;
        scrollView.scrollSensitivity = 25f;
        scrollView.movementType = ScrollRect.MovementType.Clamped;

        // 创建内容容器
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollViewGO.transform, false);

        var contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        scrollView.content = contentRect;
        scrollView.viewport = scrollRect;

        // 添加内容大小适配器
        var contentSizeFitter = contentGO.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 添加垂直布局组
        var verticalLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        verticalLayout.spacing = 8f;
        verticalLayout.padding = new RectOffset(15, 15, 15, 15);
        verticalLayout.childControlHeight = false;
        verticalLayout.childControlWidth = true;
        verticalLayout.childForceExpandWidth = true;

        saveSlotContainer = contentGO.transform;

        // 创建滚动条
        CreateScrollbar(scrollViewGO);
    }

    private void CreateScrollbar(GameObject scrollViewParent)
    {
        var scrollbarGO = new GameObject("Scrollbar");
        scrollbarGO.transform.SetParent(scrollViewParent.transform, false);

        var scrollbarRect = scrollbarGO.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.offsetMin = new Vector2(-18, 0);  // 缩小滚动条宽度
        scrollbarRect.offsetMax = new Vector2(-6, 0);   // 缩小滚动条宽度

        var scrollbarImage = scrollbarGO.AddComponent<Image>();
        scrollbarImage.color = new Color(0.15f, 0.15f, 0.25f, 0.8f);

        var scrollbar = scrollbarGO.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        // 禁用滚动条的UI导航功能
        var scrollbarNavigation = scrollbar.navigation;
        scrollbarNavigation.mode = Navigation.Mode.None;
        scrollbar.navigation = scrollbarNavigation;

        // 滚动条滑块
        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(scrollbarGO.transform, false);

        var handleRect = handleGO.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        var handleImage = handleGO.AddComponent<Image>();
        handleImage.color = new Color(0.4f, 0.6f, 1f, 0.9f);

        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;

        if (scrollView != null)
        {
            scrollView.verticalScrollbar = scrollbar;
        }
    }

    private void CreateCloseButton()
    {
        closeButton = CreateStyledButton(mainPanel?.transform, "CloseButton",
            "✕", new Vector2(0.9f, 0.92f), new Vector2(0.98f, 1f),
            new Color(0.45f, 0.15f, 0.15f, 1f),      // 红色关闭按钮
            new Color(0.55f, 0.25f, 0.25f, 1f),      // 悬停时稍亮
            () => SetUIVisible(false));

        if (closeButton != null)
        {
            var text = closeButton.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 24;
                text.fontStyle = FontStyle.Bold;
            }
        }
    }

    private Button CreateStyledButton(Transform? parent, string name, string text,
                                    Vector2 anchorMin, Vector2 anchorMax,
                                    Color normalColor, Color hoverColor,
                                    Action? onClick)
    {
        var buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);

        var buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        // 添加边框
        var borderGO = new GameObject("Border");
        borderGO.transform.SetParent(buttonGO.transform, false);

        var borderRect = borderGO.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        var borderImage = borderGO.AddComponent<Image>();
        // 根据按钮功能设置边框颜色
        Color borderColor = GetBorderColorForButton(name);
        borderImage.color = borderColor;

        // 按钮背景（在边框之上）
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(buttonGO.transform, false);

        var bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(2, 2); // 边框宽度为2像素
        bgRect.offsetMax = new Vector2(-2, -2);

        var buttonImage = bgGO.AddComponent<Image>();
        buttonImage.color = normalColor;

        var button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => onClick?.Invoke());

        // 禁用UI导航功能 - 防止手柄和键盘方向键导航
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        // 按钮动画效果
        var colorBlock = button.colors;
        colorBlock.normalColor = normalColor;
        colorBlock.highlightedColor = hoverColor;
        colorBlock.pressedColor = normalColor * 0.8f;
        colorBlock.disabledColor = normalColor * 0.5f;
        colorBlock.colorMultiplier = 1f;
        colorBlock.fadeDuration = 0.15f;
        button.colors = colorBlock;

        // 按钮文本 - 确保在所有图层之上
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);  // 直接作为按钮的子级

        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4, 4);    // 添加小边距
        textRect.offsetMax = new Vector2(-4, -4);  // 添加小边距

        var textComponent = textGO.AddComponent<Text>();
        textComponent.text = text;
        textComponent.fontSize = 16;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.raycastTarget = false;  // 确保文字不阻挡按钮点击

        return button;
    }

    // 根据按钮功能获取边框颜色
    private Color GetBorderColorForButton(string buttonName)
    {
        if (buttonName.Contains("Teleport"))
            return new Color(0.6f, 0.8f, 1f, 1f);      // 蓝色 - 传送功能
        else if (buttonName.Contains("Overwrite"))
            return new Color(1f, 0.8f, 0.4f, 1f);      // 橙色 - 覆盖功能
        else if (buttonName.Contains("Delete") || buttonName.Contains("Clear"))
            return new Color(1f, 0.5f, 0.5f, 1f);      // 红色 - 危险操作
        else if (buttonName.Contains("Save"))
            return new Color(0.5f, 1f, 0.5f, 1f);      // 绿色 - 保存功能
        else
            return new Color(0.8f, 0.8f, 0.8f, 1f);    // 灰色 - 默认
    }

    private Text CreateStyledText(Transform parent, string name, string text,
                                  float fontSize, FontStyle fontStyle,
                                  Vector2 anchorMin, Vector2 anchorMax,
                                  Color color, TextAnchor alignment)
    {
        var textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);

        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = anchorMin;
        textRect.anchorMax = anchorMax;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var textComponent = textGO.AddComponent<Text>();
        textComponent.text = text;
        textComponent.fontSize = (int)fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.color = color;
        textComponent.alignment = alignment;
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.raycastTarget = false;  // 优化性能，文字不需要响应射线检测

        // 添加阴影效果以增强文字可见性
        var shadow = textGO.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);  // 黑色阴影
        shadow.effectDistance = new Vector2(1f, -1f);      // 轻微偏移

        return textComponent;
    }

    public void SetUIVisible(bool visible)
    {
        isUIVisible = visible;
        if (uiCanvas != null)
        {
            uiCanvas.SetActive(visible);
        }

        if (visible)
        {
            // 在显示UI前确保EventSystem可用
            EnsureEventSystemAvailable();

            UpdateCurrentInfo();
            RefreshSlotList();

            // 显示鼠标光标 - 使用Harmony补丁阻止游戏隐藏
            try
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                LogInfo("传送UI界面显示，已启用鼠标光标（Harmony补丁保护中）");
            }
            catch (Exception ex)
            {
                Logger?.LogError($"设置鼠标光标时发生错误: {ex.Message}");
            }
        }
        else
        {
            // 关闭UI时清理所有确认对话框
            CleanupConfirmDialogs();

            // 隐藏UI时恢复游戏鼠标状态
            try
            {
                // 根据游戏当前状态决定鼠标可见性
                var uiManager = UIManager.instance;
                if (uiManager != null && uiManager.uiState.Equals(UIState.PAUSED))
                {
                    // 如果游戏处于暂停状态，保持鼠标可见
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    LogInfo("游戏处于暂停状态，保持鼠标可见");
                }
                else
                {
                    // 游戏进行中，隐藏鼠标（游戏的默认状态）
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    LogInfo("游戏进行中，恢复默认鼠标状态（隐藏）");
                }

                LogInfo("传送UI界面隐藏，已恢复游戏鼠标状态");
            }
            catch (Exception ex)
            {
                Logger?.LogError($"恢复鼠标状态时发生错误: {ex.Message}");
            }
        }

        LogInfo($"传送UI界面 {(visible ? "显示" : "隐藏")}");
    }

    // 创建重复使用的确认对话框（初始化时调用一次）
    private void CreateReusableConfirmDialog()
    {
        // 创建对话框背景
        confirmDialog = new GameObject("ConfirmDialog");
        confirmDialog.transform.SetParent(uiCanvas?.transform, false);
        confirmDialog.SetActive(false); // 默认隐藏

        var dialogBgRect = confirmDialog.AddComponent<RectTransform>();
        dialogBgRect.anchorMin = Vector2.zero;
        dialogBgRect.anchorMax = Vector2.one;
        dialogBgRect.offsetMin = Vector2.zero;
        dialogBgRect.offsetMax = Vector2.zero;

        var dialogBgImage = confirmDialog.AddComponent<Image>();
        dialogBgImage.color = new Color(0f, 0f, 0f, 0.7f);

        // 创建对话框面板
        var dialogPanelGO = new GameObject("ConfirmDialogPanel");
        dialogPanelGO.transform.SetParent(confirmDialog.transform, false);

        var dialogPanelRect = dialogPanelGO.AddComponent<RectTransform>();
        dialogPanelRect.anchorMin = new Vector2(0.3f, 0.35f);
        dialogPanelRect.anchorMax = new Vector2(0.7f, 0.65f);
        dialogPanelRect.offsetMin = Vector2.zero;
        dialogPanelRect.offsetMax = Vector2.zero;

        var dialogPanelImage = dialogPanelGO.AddComponent<Image>();
        dialogPanelImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        // 标题文本
        confirmTitleText = CreateStyledText(dialogPanelGO.transform, "DialogTitle", "", 18, FontStyle.Bold,
            new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.95f),
            new Color(1f, 1f, 1f, 1f), TextAnchor.MiddleCenter);

        // 消息文本
        confirmMessageText = CreateStyledText(dialogPanelGO.transform, "DialogMessage", "", 14, FontStyle.Normal,
            new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.7f),
            new Color(0.9f, 0.9f, 0.9f, 1f), TextAnchor.MiddleCenter);

        // 确认按钮 - 红色突出危险操作
        confirmYesButton = CreateStyledButton(dialogPanelGO.transform, "ConfirmButton",
            "确认 | Confirm", new Vector2(0.05f, 0.05f), new Vector2(0.47f, 0.3f),
            new Color(0.5f, 0.15f, 0.15f, 1f),
            new Color(0.6f, 0.25f, 0.25f, 1f),
            null); // 回调稍后设置

        // 取消按钮 - 灰色调
        confirmNoButton = CreateStyledButton(dialogPanelGO.transform, "CancelButton",
            "取消 | Cancel", new Vector2(0.53f, 0.05f), new Vector2(0.95f, 0.3f),
            new Color(0.3f, 0.3f, 0.3f, 1f),
            new Color(0.4f, 0.4f, 0.4f, 1f),
            () => HideConfirmDialog()); // 取消总是隐藏对话框

        LogInfo("重复使用的确认对话框创建完成");
    }

    // 显示确认对话框
    private void ShowConfirmDialog(string title, string message, System.Action onConfirm)
    {
        if (confirmDialog == null || confirmTitleText == null || confirmMessageText == null || confirmYesButton == null)
        {
            Logger?.LogError("确认对话框未正确初始化");
            return;
        }

        // 设置文本内容
        confirmTitleText.text = title;
        confirmMessageText.text = message;

        // 设置确认按钮回调
        confirmYesButton.onClick.RemoveAllListeners();
        confirmYesButton.onClick.AddListener(() =>
        {
            HideConfirmDialog();
            onConfirm?.Invoke();
        });

        // 显示对话框
        confirmDialog.SetActive(true);
    }

    // 隐藏确认对话框
    private void HideConfirmDialog()
    {
        if (confirmDialog != null)
        {
            confirmDialog.SetActive(false);
        }
    }

    // 清理确认对话框（简化版）
    private void CleanupConfirmDialogs()
    {
        // 现在只需要隐藏对话框，不需要销毁
        HideConfirmDialog();
    }

    public void ToggleUI()
    {
        // 如果要打开UI，进行各种检查
        if (!isUIVisible)
        {
            // 统一的游戏状态检查
            if (!IsGameStateValidForUIOpen())
            {
                return; // 方法内部已包含具体的日志信息
            }
        }

        SetUIVisible(!isUIVisible);
    }

    // 统一的游戏状态检查 - 判断是否可以打开UI界面
    private bool IsGameStateValidForUIOpen()
    {
        // 检查是否在游戏世界中
        if (!IsInGameWorld())
        {
            LogInfo("不在游戏世界中，无法显示传送UI界面");
            return false;
        }

        // 检查是否正在传送中
        if (IsTeleportInProgress())
        {
            LogInfo("检测到传送正在进行中，禁止打开传送UI界面");
            return false;
        }

        // 检查传送操作条件
        if (!CanPerformTeleportOperations())
        {
            LogInfo("检测到玩家状态不允许传送操作，禁止打开传送UI界面");
            return false; // CanPerformTeleportOperations()内部已包含日志
        }

        // 检查ESC菜单状态
        try
        {
            var uiManager = UIManager.instance;
            if (uiManager != null && uiManager.uiState.Equals(UIState.PAUSED))
            {
                LogInfo("ESC菜单已打开，无法显示传送UI界面");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"检查ESC菜单状态时发生错误: {ex.Message}");
            return false; // 出错时为安全起见禁止打开
        }

        return true; // 所有检查都通过
    }

    // 检查基础游戏组件是否可用
    private bool AreGameComponentsAvailable()
    {
        return HeroController.instance != null && GameManager.instance != null;
    }

    // 检查是否在游戏世界中（而不是主菜单等场景）
    private bool IsInGameWorld()
    {
        try
        {
            // 使用统一的组件检查
            if (!AreGameComponentsAvailable())
            {
                return false;
            }

            // 可以在这里添加更多检查，比如特定的非游戏场景
            // 例如：if (GameManager.instance.sceneName == "Menu_Title") return false;

            return true;
        }
        catch (Exception ex)
        {
            Logger?.LogError($"检查游戏世界状态时发生错误: {ex.Message}");
            return false; // 出错时为安全起见禁止操作
        }
    }

    // 检查传送是否正在进行中
    private bool IsTeleportInProgress()
    {
        try
        {
            // 使用统一的组件检查，如果基础组件不可用，无法判断传送状态
            if (!AreGameComponentsAvailable())
            {
                return false; // 组件不可用时认为没有传送进行
            }

            var gameManager = GameManager.instance;
            var heroController = HeroController.instance;

            // 检查场景切换状态
            if (gameManager.IsInSceneTransition)
            {
                LogInfo("检测到场景切换中，传送进行中");
                return true;
            }

            // 检查重生状态
            if (gameManager.RespawningHero)
            {
                LogInfo("检测到角色重生中，传送进行中");
                return true;
            }

            // 检查角色传送状态
            if (heroController.cState != null && heroController.cState.transitioning)
            {
                LogInfo("检测到角色transitioning状态，传送进行中");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger?.LogError($"检查传送状态时发生错误: {ex.Message}");
            return true; // 出错时为安全起见假设传送进行中
        }
    }

    // 单个存档删除的二次确认
    private void ConfirmDeleteSlot(string slotId, string displayName)
    {
        // 使用重复利用的确认对话框
        ShowConfirmDialog(
            $"确认删除存档：{displayName}\nConfirm Delete Save: {displayName}",
            "此操作不可撤销！\nThis action cannot be undone!",
            () =>
            {
                OnDeleteSlot?.Invoke(slotId);
                LogInfo($"用户确认删除存档: {displayName} (ID: {slotId})");
            });
    }

    // 存档覆盖的二次确认
    private void ConfirmOverwriteSlot(string slotId, string displayName)
    {
        // 使用重复利用的确认对话框
        ShowConfirmDialog(
            $"覆盖存档：{displayName}\nOverwrite Save: {displayName}",
            "当前位置将覆盖此存档！\nCurrent position will overwrite this save!",
            () =>
            {
                OnOverwriteSlot?.Invoke(slotId);
                LogInfo($"用户确认覆盖存档: {displayName} (ID: {slotId})");
            });
    }

    // 清空所有存档的二次确认
    private void ConfirmClearAll()
    {
        // 使用重复利用的确认对话框
        ShowConfirmDialog(
            "确认清空所有存档？\nConfirm Clear All Saves?",
            "此操作不可撤销！\nThis action cannot be undone!",
            () =>
            {
                OnClearAllSlots?.Invoke();
                LogInfo("用户确认清空所有存档");
            });
    }



    public bool IsUIVisible => isUIVisible;

    public void UpdateCurrentInfo()
    {
        try
        {
            if (currentInfoText == null) return;

            if (IsInGameWorld())
            {
                var position = HeroController.instance!.transform.position;
                var scene = GameManager.instance!.sceneName;
                var displayName = GetSceneDisplayName(scene);

                currentInfoText.text = $"📍 当前位置 | Current: {displayName} ({position.x:F1}, {position.y:F1})";
            }
            else
            {
                currentInfoText.text = "📍 当前位置: 未在游戏中 | Current Position: Not In Game";
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"更新当前信息时发生错误: {ex.Message}");
        }
    }

    private string GetSceneDisplayName(string sceneName)
    {
        // 直接返回原始场景名，暂时不做复杂映射
        return sceneName ?? "Unknown";
    }

    public void RefreshSlotList()
    {
        if (saveSlotContainer == null) return;

        try
        {
            // 清除现有UI元素
            foreach (Transform child in saveSlotContainer)
            {
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }

            // 获取所有存档槽数据
            var allSlots = GetAllSaveSlots();
            if (allSlots == null || allSlots.Count == 0)
            {
                CreateEmptySlotMessage();
                return;
            }

            // 创建存档槽UI，按优先级排序（传统存档在前，新的手动存档在上）
            var sortedSlots = allSlots.OrderBy(x =>
            {
                // 传统存档排在前面
                if (x.Key.StartsWith("traditional_"))
                    return "0_" + x.Key;
                else
                    // 扩展存档按时间降序排列（新的在前面）
                    return "1_" + (DateTime.MaxValue.Ticks - x.Value.saveTime.Ticks).ToString("D19");
            }).ToList();

            foreach (var kvp in sortedSlots)
            {
                CreateSlotUI(kvp.Key, kvp.Value);
            }

            LogInfo($"刷新存档列表完成，共 {allSlots.Count} 个存档");
        }
        catch (Exception ex)
        {
            Logger?.LogError($"刷新存档列表时发生错误: {ex.Message}");
        }
    }

    private void CreateEmptySlotMessage()
    {
        var emptyGO = new GameObject("EmptyMessage");
        emptyGO.transform.SetParent(saveSlotContainer, false);

        var emptyRect = emptyGO.AddComponent<RectTransform>();
        emptyRect.sizeDelta = new Vector2(0, 120);

        var emptyBg = emptyGO.AddComponent<Image>();
        emptyBg.color = new Color(0.1f, 0.1f, 0.15f, 0.6f);

        CreateStyledText(emptyGO.transform, "EmptyText",
            "📝 暂无存档位置\n使用上方按钮保存当前位置\n或使用传统快捷键 Ctrl+1~5 保存\n\n📝 No Save Slots\nUse the button above to save current position\nOr use traditional shortcuts Ctrl+1~5",
            16, FontStyle.Normal,
            new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f),
            new Color(0.6f, 0.7f, 0.8f, 1f), TextAnchor.MiddleCenter);
    }

    private void CreateSlotUI(string slotId, ExtendedSaveSlot slotData)
    {
        if (saveSlotContainer == null) return;

        var slotGO = new GameObject($"Slot_{slotId}");
        slotGO.transform.SetParent(saveSlotContainer, false);

        var slotRect = slotGO.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(0, 120);  // 增加高度，给按钮更多空间

        // 存档槽背景 - 区分传统存档和扩展存档
        var slotBg = slotGO.AddComponent<Image>();
        if (slotId.StartsWith("traditional_"))
        {
            // 传统存档 - 蓝灰色调，突出快捷键存档
            slotBg.color = new Color(0.2f, 0.25f, 0.3f, 0.9f);
        }
        else
        {
            // 扩展存档 - 纯灰色调，UI手动存档
            slotBg.color = new Color(0.22f, 0.22f, 0.22f, 0.9f);
        }

        // 存档信息容器 - 占据左侧空间和上方空间
        var infoContainer = new GameObject("InfoContainer");
        infoContainer.transform.SetParent(slotGO.transform, false);

        var infoRect = infoContainer.AddComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0, 0);      // 占据全高
        infoRect.anchorMax = new Vector2(0.58f, 1);  // 左侧58%宽度，给按钮更多空间
        infoRect.offsetMin = new Vector2(10, 5);     // 统一边距
        infoRect.offsetMax = new Vector2(-5, -5);    // 右侧减少边距，为按钮让路

        // 存档名称 - 增强可见性，扩展存档添加序号
        string displayNameWithNumber = slotData.displayName;
        if (!slotId.StartsWith("traditional_") && slotData.serialNumber > 0)
        {
            displayNameWithNumber = $"{slotData.serialNumber}. {slotData.displayName}";
        }

        var nameText = CreateStyledText(infoContainer.transform, "NameText",
            displayNameWithNumber, 18, FontStyle.Bold,
            new Vector2(0.02f, 0.75f), new Vector2(0.98f, 1),
            new Color(1f, 1f, 1f, 1f), TextAnchor.UpperLeft);

        // 场景信息 - 增强可见性
        var sceneDisplayName = GetSceneDisplayName(slotData.scene);
        var sceneText = CreateStyledText(infoContainer.transform, "SceneText",
            $"场景 | Scene: {sceneDisplayName}", 14, FontStyle.Normal,
            new Vector2(0.02f, 0.5f), new Vector2(0.98f, 0.75f),
            new Color(0.9f, 0.9f, 0.9f, 1f), TextAnchor.UpperLeft);

        // 坐标信息 - 增强可见性
        var positionText = CreateStyledText(infoContainer.transform, "PositionText",
            $"坐标 | Pos: ({slotData.position.x:F1}, {slotData.position.y:F1})", 14, FontStyle.Normal,
            new Vector2(0.02f, 0.25f), new Vector2(0.98f, 0.5f),
            new Color(0.9f, 0.9f, 0.9f, 1f), TextAnchor.UpperLeft);

        // 时间信息 - 增强可见性
        var timeText = CreateStyledText(infoContainer.transform, "TimeText",
            $"时间 | Time: {slotData.saveTime:yyyy-MM-dd HH:mm}", 12, FontStyle.Normal,
            new Vector2(0.02f, 0), new Vector2(0.98f, 0.25f),
            new Color(0.8f, 0.8f, 0.8f, 1f), TextAnchor.UpperLeft);

        // 按钮容器 - 水平排列在右侧底部
        var buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(slotGO.transform, false);

        var buttonRect = buttonContainer.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.6f, 0.1f);  // 右侧40%宽度，底部留10%边距
        buttonRect.anchorMax = new Vector2(1, 0.9f);     // 高度占存档槽的80%，接近边框高度
        buttonRect.offsetMin = new Vector2(5, 0);        // 左侧留小间距，减少底部边距
        buttonRect.offsetMax = new Vector2(-10, 0);      // 右侧统一边距，减少顶部边距

        // 传送按钮 - 灰色调，左侧
        var teleportButton = CreateStyledButton(buttonContainer.transform, "TeleportButton",
            "传送\nTeleport", new Vector2(0, 0), new Vector2(0.3f, 1),
            new Color(0.35f, 0.35f, 0.35f, 1f),
            new Color(0.45f, 0.45f, 0.45f, 1f),
            () => OnTeleportToSlot?.Invoke(slotId));

        // 覆盖按钮 - 橙色调，中间
        var overwriteButton = CreateStyledButton(buttonContainer.transform, "OverwriteButton",
            "覆盖\nOverwrite", new Vector2(0.35f, 0), new Vector2(0.65f, 1),
            new Color(0.5f, 0.35f, 0.15f, 1f),
            new Color(0.6f, 0.45f, 0.25f, 1f),
            () => ConfirmOverwriteSlot(slotId, slotData.displayName));

        // 删除按钮 - 红色突出危险操作，右侧
        var deleteButton = CreateStyledButton(buttonContainer.transform, "DeleteButton",
            "删除\nDelete", new Vector2(0.7f, 0), new Vector2(1, 1),
            new Color(0.45f, 0.2f, 0.2f, 1f),
            new Color(0.55f, 0.3f, 0.3f, 1f),
            () => ConfirmDeleteSlot(slotId, slotData.displayName));

        // 调整按钮字体大小 - 更高的按钮可以使用稍大字体显示中英文
        var teleportText = teleportButton.GetComponentInChildren<Text>();
        if (teleportText != null) teleportText.fontSize = 12;

        var overwriteText = overwriteButton.GetComponentInChildren<Text>();
        if (overwriteText != null) overwriteText.fontSize = 12;

        var deleteText = deleteButton.GetComponentInChildren<Text>();
        if (deleteText != null) deleteText.fontSize = 12;

        // UI元素创建完成
    }

    private void Update()
    {
        // 检测快捷键 Ctrl+` (ToggleUI内部已包含完整检查)
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.BackQuote))
        {
            ToggleUI();
        }

        // 如果UI界面可见，阻止ESC键的默认行为（防止呼出ESC菜单）
        if (isUIVisible)
        {
            // 持续监控ESC菜单状态，如果检测到ESC菜单被打开（比如手柄呼出），立即关闭UI
            try
            {
                var uiManager = UIManager.instance;
                if (uiManager != null && uiManager.uiState.Equals(UIState.PAUSED))
                {
                    LogInfo("检测到ESC菜单被打开，自动关闭传送UI界面以避免冲突");
                    SetUIVisible(false);
                    return; // 立即返回，避免处理其他输入
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"监控ESC菜单状态时发生错误: {ex.Message}");
            }

            // 每0.5秒检测一次玩家状态，避免过于频繁的检查
            if (Time.time - lastStateCheckTime >= STATE_CHECK_INTERVAL)
            {
                lastStateCheckTime = Time.time;

                if (!CanPerformTeleportOperations())
                {
                    LogInfo("检测到玩家状态不允许传送操作，自动关闭传送UI界面");
                    SetUIVisible(false);
                    return; // 立即返回，避免处理其他输入
                }

                // 同时更新当前位置信息
                if (currentInfoText != null)
                {
                    UpdateCurrentInfo();
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // 直接关闭UI，不让ESC键传递给游戏
                SetUIVisible(false);

                // 消费ESC键事件，防止游戏处理ESC键
                try
                {
                    // 强制清除输入状态，防止ESC传递给游戏
                    Input.ResetInputAxes();
                }
                catch (Exception ex)
                {
                    Logger?.LogError($"重置输入状态时发生错误: {ex.Message}");
                }
            }
        }
    }


    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
