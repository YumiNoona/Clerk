using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[CustomEditor(typeof(StoreUIAuthoring))]
public sealed class StoreUIAuthoringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        StoreUIAuthoring authoring = (StoreUIAuthoring)target;

        EditorGUILayout.HelpBox(
            "This hierarchy is authored and editable. Runtime code only " +
            "binds actions and populates live data rows.",
            MessageType.Info);

        EditorGUILayout.HelpBox(
            "Desktop icons: import each PNG as Texture Type = Sprite (2D and UI), " +
            "then assign it under Desktop Application Icons below. The desktop " +
            "preview updates immediately in Edit Mode. Transparent square PNGs work best.",
            MessageType.None);

        if (GUILayout.Button("Rebuild Authored Store UI",GUILayout.Height(30f)) &&
            EditorUtility.DisplayDialog(
                "Rebuild Store UI",
                "Replace the current authored UI hierarchy?",
                "Rebuild",
                "Cancel"))
        {
            StoreUIHierarchyBuilder.Build(authoring);
        }

        if (DrawDefaultInspector())
        {
            StoreUIHierarchyBuilder.RefreshDesktopIcons(authoring);
        }
    }
}

[InitializeOnLoad]
public static class StoreUIHierarchyBuilder
{
    private static bool isBuilding;
    static StoreUIHierarchyBuilder()
    {
        EditorApplication.delayCall += BuildMissingUI;
    }

    private static void BuildMissingUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        StoreUIAuthoring authoring =
            Object.FindAnyObjectByType<StoreUIAuthoring>(
                FindObjectsInactive.Include);

        if (authoring != null && NeedsRebuild(authoring))
        {
            Build(authoring);
        }

        if (authoring != null)
        {
            RefreshDesktopIcons(authoring);
        }

        if (authoring != null && authoring.gameObject.scene.isDirty)
        {
            EditorSceneManager.SaveScene(authoring.gameObject.scene);
        }
    }

    private static bool NeedsRebuild(StoreUIAuthoring authoring)
    {
        int canvasCount = 0;
        int eventSystemCount = 0;

        for (int i = 0; i < authoring.transform.childCount; i++)
        {
            string childName = authoring.transform.GetChild(i).name;
            if (childName == "Store UI Canvas")
            {
                canvasCount++;
            }
            else if (childName == "UI Event System")
            {
                eventSystemCount++;
            }
        }

        return !authoring.IsComplete ||
            canvasCount != 1 ||
            eventSystemCount != 1 ||
            authoring.DeviceRoot == null ||
            authoring.DeviceRoot.name != "Device UI" ||
            authoring.MobileLayout != null;
    }

    public static void RefreshDesktopIcons(StoreUIAuthoring ui)
    {
        if (ui == null || EditorApplication.isPlayingOrWillChangePlaymode ||
            ui.ApplicationButtons == null || ui.ApplicationButtons.Length != 17)
        {
            return;
        }

        Sprite[] icons =
        {
            ui.OverviewIcon, ui.StoreIcon, ui.RegisterIcon, ui.BankIcon,
            ui.HiringIcon, ui.LinkedInIcon, ui.HistoryIcon, ui.SettingsIcon,
            ui.LoginIcon, ui.MailIcon, ui.AppMarketIcon, ui.MessagesIcon,
            ui.SecurityIcon, ui.TodoIcon, ui.WeatherIcon, ui.NotepadIcon,
            ui.CalculatorIcon
        };

        Undo.RegisterFullObjectHierarchyUndo(ui.gameObject,"Update Desktop Icons");
        for (int i = 0; i < ui.ApplicationButtons.Length; i++)
        {
            Button button = ui.ApplicationButtons[i];
            if (button == null)
            {
                continue;
            }

            Transform existing = button.transform.Find("Icon Image");
            TextMeshProUGUI glyph = button.GetComponentInChildren<TextMeshProUGUI>(true);
            Sprite icon = icons[i];

            if (icon == null)
            {
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }
                if (glyph != null)
                {
                    glyph.gameObject.SetActive(true);
                }
                continue;
            }

            Image image;
            if (existing == null)
            {
                RectTransform iconRect = UIFactory.Panel(
                    button.transform,"Icon Image",Color.white);
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f,0.5f);
                iconRect.sizeDelta = Vector2.one * ui.ApplicationIconSize;
                iconRect.anchoredPosition = Vector2.zero;
                image = iconRect.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            else
            {
                image = existing.GetComponent<Image>();
            }

            RectTransform imageRect = image.rectTransform;
            imageRect.anchorMin = imageRect.anchorMax = new Vector2(0.5f,0.5f);
            imageRect.sizeDelta = Vector2.one * ui.ApplicationIconSize;
            imageRect.anchoredPosition = Vector2.zero;

            image.sprite = icon;
            image.color = Color.white;
            if (glyph != null)
            {
                glyph.gameObject.SetActive(false);
            }
        }

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        SceneView.RepaintAll();
    }

    public static void Build(StoreUIAuthoring ui)
    {
        if (ui == null || isBuilding)
        {
            return;
        }

        isBuilding = true;

        try
        {
        Undo.RegisterFullObjectHierarchyUndo(ui.gameObject,"Build Store UI");

        for (int i = ui.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(ui.transform.GetChild(i).gameObject);
        }

        GameObject canvasObject = new GameObject(
            "Store UI Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(ui.transform,false);
        ui.Canvas = canvasObject.GetComponent<Canvas>();
        ui.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ui.Canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f,1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildHud(ui);
        BuildMainMenu(ui);
        BuildPauseMenu(ui);
        BuildNotifications(ui);
        BuildPriceEditor(ui);
        BuildDevice(ui);

        GameObject events = new GameObject(
            "UI Event System",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        events.transform.SetParent(ui.transform,false);

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveScene(ui.gameObject.scene);
        Selection.activeGameObject = ui.gameObject;
        Debug.Log("Authored Store UI hierarchy created. Save the scene.",ui);
        }
        finally
        {
            isBuilding = false;
        }
    }

    private static void BuildHud(StoreUIAuthoring ui)
    {
        ui.HudRoot = UIFactory.Panel(ui.Canvas.transform,"Gameplay HUD",Color.clear);
        RectTransform top = UIFactory.Panel(
            ui.HudRoot,"Top Bar",new Color(0.04f,0.05f,0.07f,0.92f));
        top.anchorMin = new Vector2(0.02f,0.92f);
        top.anchorMax = new Vector2(0.98f,0.985f);
        top.offsetMin = top.offsetMax = Vector2.zero;
        UIFactory.Horizontal(top,16f,18f);

        ui.MoneyText = UIFactory.Text(top,"Money","$1,000.00",25f,TextAlignmentOptions.Left);
        ui.MoneyText.color = UIFactory.Accent;
        UIFactory.Size(ui.MoneyText,300f,60f);
        ui.ClockText = UIFactory.Text(top,"Clock","DAY 1  08:00",22f,TextAlignmentOptions.Center);
        ui.StatusText = UIFactory.Text(top,"Status","STORE CLOSED",20f,TextAlignmentOptions.Right);
        UIFactory.Size(ui.StatusText,420f,60f);

        TextMeshProUGUI crosshair = UIFactory.Text(
            ui.HudRoot,"Crosshair","+",30f,TextAlignmentOptions.Center);
        crosshair.rectTransform.anchorMin = crosshair.rectTransform.anchorMax =
            new Vector2(0.5f,0.5f);
        crosshair.rectTransform.sizeDelta = new Vector2(32f,32f);
        crosshair.rectTransform.anchoredPosition = Vector2.zero;
    }

    private static void BuildMainMenu(StoreUIAuthoring ui)
    {
        ui.MainMenuRoot = UIFactory.Panel(
            ui.Canvas.transform,"Main Menu",UIFactory.Background);
        RectTransform card = UIFactory.Panel(ui.MainMenuRoot,"Main Menu Card",UIFactory.Surface);
        card.anchorMin = new Vector2(0.32f,0.16f);
        card.anchorMax = new Vector2(0.68f,0.84f);
        card.offsetMin = card.offsetMax = Vector2.zero;
        UIFactory.Vertical(card,16f,32f);
        TextMeshProUGUI title = UIFactory.Text(card,"Title","CLERK",72f,TextAlignmentOptions.Center);
        title.color = UIFactory.Accent;
        UIFactory.Size(title,0f,120f);
        TextMeshProUGUI subtitle = UIFactory.Text(
            card,"Subtitle","BUILD  /  STOCK  /  SERVE  /  GROW",18f,TextAlignmentOptions.Center);
        subtitle.color = UIFactory.Muted;
        UIFactory.Size(subtitle,0f,44f);
        ui.StartButton = MenuButton(card,"START STORE",UIFactory.Accent);
        ui.ContinueButton = MenuButton(card,"CONTINUE");
        ui.MainSettingsButton = MenuButton(card,"SETTINGS");
        ui.MainQuitButton = MenuButton(card,"QUIT",UIFactory.Danger);
        ui.MainMenuRoot.gameObject.SetActive(false);
    }

    private static void BuildPauseMenu(StoreUIAuthoring ui)
    {
        ui.PauseRoot = UIFactory.Panel(
            ui.Canvas.transform,"Pause Menu",new Color(0.02f,0.025f,0.04f,0.94f));
        RectTransform card = UIFactory.Panel(ui.PauseRoot,"Pause Card",UIFactory.Surface);
        card.anchorMin = new Vector2(0.37f,0.17f);
        card.anchorMax = new Vector2(0.63f,0.83f);
        card.offsetMin = card.offsetMax = Vector2.zero;
        UIFactory.Vertical(card,12f,26f);
        TextMeshProUGUI title = UIFactory.Text(card,"Title","PAUSED",44f,TextAlignmentOptions.Center);
        title.color = UIFactory.Accent;
        UIFactory.Size(title,0f,80f);
        ui.ResumeButton = MenuButton(card,"RESUME",UIFactory.Accent);
        ui.DesktopButton = MenuButton(card,"DESKTOP");
        ui.SaveButton = MenuButton(card,"SAVE GAME");
        ui.LoadButton = MenuButton(card,"LOAD GAME");
        ui.PauseQuitButton = MenuButton(card,"QUIT",UIFactory.Danger);
        ui.PauseRoot.gameObject.SetActive(false);
    }

    private static void BuildNotifications(StoreUIAuthoring ui)
    {
        ui.NotificationRoot = UIFactory.Panel(ui.Canvas.transform,"Notifications",Color.clear);
        ui.NotificationRoot.anchorMin = new Vector2(0.67f,0.7f);
        ui.NotificationRoot.anchorMax = new Vector2(0.98f,0.9f);
        ui.NotificationRoot.offsetMin = ui.NotificationRoot.offsetMax = Vector2.zero;
        ui.NotificationRoot.gameObject.SetActive(false);
    }

    private static void BuildPriceEditor(StoreUIAuthoring ui)
    {
        ui.PriceEditorRoot = UIFactory.Panel(
            ui.Canvas.transform,"Shelf Price Editor",new Color(0f,0f,0f,0.72f));
        RectTransform card = UIFactory.Panel(ui.PriceEditorRoot,"Price Card",UIFactory.Surface);
        card.anchorMin = new Vector2(0.37f,0.28f);
        card.anchorMax = new Vector2(0.63f,0.72f);
        card.offsetMin = card.offsetMax = Vector2.zero;
        UIFactory.Vertical(card,14f,28f);
        TextMeshProUGUI title = UIFactory.Text(card,"Title","SET SHELF PRICE",30f,TextAlignmentOptions.Center);
        title.color = UIFactory.Accent;
        UIFactory.Size(title,0f,58f);
        ui.PriceDetailsText = UIFactory.Text(card,"Price Details","Product price",20f,TextAlignmentOptions.Center);
        UIFactory.Size(ui.PriceDetailsText,0f,105f);
        RectTransform inputRoot = UIFactory.Panel(card,"Price Input",UIFactory.SurfaceRaised);
        UIFactory.Size(inputRoot,0f,58f);
        ui.PriceInput = inputRoot.gameObject.AddComponent<TMP_InputField>();
        TextMeshProUGUI inputText = UIFactory.Text(inputRoot,"Text","0.00",24f,TextAlignmentOptions.Center);
        ui.PriceInput.textComponent = inputText;
        ui.PriceInput.contentType = TMP_InputField.ContentType.DecimalNumber;
        ui.ApplyPriceButton = MenuButton(card,"APPLY PRICE",UIFactory.Accent,54f);
        ui.CancelPriceButton = MenuButton(card,"CANCEL",null,48f);
        ui.PriceEditorRoot.gameObject.SetActive(false);
    }

    private static void BuildDevice(StoreUIAuthoring ui)
    {
        ui.DeviceRoot = UIFactory.Panel(
            ui.Canvas.transform,"Device UI",new Color(0f,0f,0f,0.78f));

        BuildDesktopDevice(ui);
        ui.DeviceRoot.gameObject.SetActive(false);
    }

    private static void BuildDesktopDevice(StoreUIAuthoring ui)
    {
        Color desktopBackground = new Color32(13,20,35,255);
        Color taskbarColor = new Color32(18,23,34,250);
        ui.DeviceFrame = UIFactory.Panel(ui.DeviceRoot,"Device Frame",desktopBackground);
        ui.DeviceFrame.name = "Desktop Layout";
        ui.DeviceFrame.anchorMin = new Vector2(0.06f,0.06f);
        ui.DeviceFrame.anchorMax = new Vector2(0.94f,0.94f);
        ui.DeviceFrame.offsetMin = ui.DeviceFrame.offsetMax = Vector2.zero;
        Outline outline = ui.DeviceFrame.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f,0.28f,0.35f,1f);
        outline.effectDistance = new Vector2(3f,-3f);

        RectTransform header = UIFactory.Panel(ui.DeviceFrame,"Window Title Bar",UIFactory.Surface);
        header.anchorMin = new Vector2(0.20f,0.92f);
        header.anchorMax = Vector2.one;
        header.offsetMin = header.offsetMax = Vector2.zero;
        UIFactory.Horizontal(header,10f,14f);
        ui.DeviceBrand = UIFactory.Text(header,"Brand","CLERK DESKTOP",22f,TextAlignmentOptions.Left);
        ui.DeviceBrand.color = UIFactory.Accent;
        UIFactory.Size(ui.DeviceBrand,180f,0f);
        ui.DeviceTitle = UIFactory.Text(header,"Application Title","OVERVIEW",22f,TextAlignmentOptions.Center);
        ui.DeviceCloseButton = UIFactory.Button(header,"Close","X",null,UIFactory.Danger);
        UIFactory.Size(ui.DeviceCloseButton,54f,48f);

        RectTransform desktopHeader = UIFactory.Panel(
            ui.DeviceFrame,"Desktop Header",new Color32(18,27,45,255));
        desktopHeader.anchorMin = new Vector2(0f,0.92f);
        desktopHeader.anchorMax = new Vector2(0.20f,1f);
        desktopHeader.offsetMin = desktopHeader.offsetMax = Vector2.zero;
        TextMeshProUGUI desktopName = UIFactory.Text(
            desktopHeader,"Store Name","QUICK STOP\nMANAGEMENT",19f,
            TextAlignmentOptions.Center);
        desktopName.color = UIFactory.Accent;

        RectTransform taskbar = UIFactory.Panel(
            ui.DeviceFrame,"Taskbar",taskbarColor);
        taskbar.anchorMin = Vector2.zero;
        taskbar.anchorMax = new Vector2(1f,0.075f);
        taskbar.offsetMin = taskbar.offsetMax = Vector2.zero;
        ui.TaskbarStartButton = UIFactory.Button(
            taskbar,"Start","START",null,new Color32(34,126,210,255));
        RectTransform startRect = ui.TaskbarStartButton.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.01f,0.12f);
        startRect.anchorMax = new Vector2(0.13f,0.88f);
        startRect.offsetMin = startRect.offsetMax = Vector2.zero;
        ui.TaskbarClockText = UIFactory.Text(
            taskbar,"Clock And Calendar","08:00\nDAY 1",15f,
            TextAlignmentOptions.Right);
        ui.TaskbarClockText.rectTransform.anchorMin = new Vector2(0.76f,0f);
        ui.TaskbarClockText.rectTransform.anchorMax = new Vector2(0.985f,1f);
        ui.TaskbarClockText.rectTransform.offsetMin = ui.TaskbarClockText.rectTransform.offsetMax = Vector2.zero;
        ui.TaskbarClockText.color = new Color32(225,230,240,255);

        ui.DeviceNavigation = UIFactory.Panel(
            ui.DeviceFrame,"Desktop App Launcher",new Color32(16,25,42,255));
        ui.DeviceNavigation.anchorMin = new Vector2(0f,0.075f);
        ui.DeviceNavigation.anchorMax = new Vector2(0.20f,0.92f);
        ui.DeviceNavigation.offsetMin = ui.DeviceNavigation.offsetMax = Vector2.zero;
        GridLayoutGroup launcher =
            ui.DeviceNavigation.gameObject.AddComponent<GridLayoutGroup>();
        launcher.padding = new RectOffset(20,20,24,18);
        launcher.spacing = new Vector2(14f,18f);
        launcher.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        launcher.constraintCount = 3;
        launcher.cellSize = new Vector2(82f,82f);
        launcher.childAlignment = TextAnchor.UpperCenter;

        string[] apps =
        {
            "OVERVIEW", "STORE", "REGISTER", "BANK",
            "HIRING", "LINKEDIN", "HISTORY", "SETTINGS", "LOGIN", "MAIL",
            "APP MARKET", "MESSAGES", "SECURITY", "TODO", "WEATHER", "NOTEPAD", "CALCULATOR"
        };
        string[] glyphs = { "HQ", "SHOP", "POS", "$", "HR", "IN", "LOG", "SET", "ID", "MAIL", "GET", "MSG", "CAM", "DO", "SUN", "TXT", "123" };
        Color[] colors =
        {
            new Color32(76,201,240,255), new Color32(255,159,67,255),
            new Color32(167,126,245,255), new Color32(190,242,58,255),
            new Color32(255,103,132,255), new Color32(69,180,230,255),
            new Color32(137,145,168,255), new Color32(92,99,120,255),
            new Color32(53,199,140,255), new Color32(73,136,230,255),
            new Color32(80,215,180,255), new Color32(255,91,120,255),
            new Color32(82,88,107,255), new Color32(166,239,46,255),
            new Color32(69,180,230,255), new Color32(137,145,168,255),
            new Color32(255,159,67,255)
        };
        Sprite[] icons =
        {
            ui.OverviewIcon, ui.StoreIcon, ui.RegisterIcon, ui.BankIcon,
            ui.HiringIcon, ui.LinkedInIcon, ui.HistoryIcon, ui.SettingsIcon,
            ui.LoginIcon, ui.MailIcon, ui.AppMarketIcon, ui.MessagesIcon,
            ui.SecurityIcon, ui.TodoIcon, ui.WeatherIcon, ui.NotepadIcon,
            ui.CalculatorIcon
        };
        List<Button> buttons = new List<Button>();
        for (int i = 0; i < apps.Length; i++)
        {
            Button button = BuildDesktopAppShortcut(
                ui.DeviceNavigation,apps[i],glyphs[i],colors[i],icons[i],
                ui.ApplicationIconSize);
            buttons.Add(button);
        }
        ui.ApplicationButtons = buttons.ToArray();

        ui.DeviceBody = UIFactory.Panel(ui.DeviceFrame,"Workspace",UIFactory.Background);
        ui.DeviceBody.anchorMin = new Vector2(0.20f,0.075f);
        ui.DeviceBody.anchorMax = new Vector2(1f,0.92f);
        ui.DeviceBody.offsetMin = ui.DeviceBody.offsetMax = Vector2.zero;

        RectTransform pages = UIFactory.Panel(
            ui.DeviceBody,"Application Pages",Color.clear);
        UIFactory.Stretch(pages);
        ui.ApplicationPages = new RectTransform[apps.Length];
        ui.ApplicationContents = new RectTransform[apps.Length];

        for (int i = 0; i < apps.Length; i++)
        {
            RectTransform page = UIFactory.Panel(
                pages,apps[i] + " Software Window",desktopBackground);
            page.anchorMin = new Vector2(0.025f,0.03f);
            page.anchorMax = new Vector2(0.975f,0.97f);
            page.offsetMin = page.offsetMax = Vector2.zero;
            Outline windowOutline = page.gameObject.AddComponent<Outline>();
            windowOutline.effectColor = new Color32(55,65,84,255);
            windowOutline.effectDistance = new Vector2(2f,-2f);

            RectTransform pageHeader = UIFactory.Panel(
                page,"Page Header",UIFactory.Surface);
            pageHeader.anchorMin = new Vector2(0f,0.90f);
            pageHeader.anchorMax = Vector2.one;
            pageHeader.offsetMin = new Vector2(14f,6f);
            pageHeader.offsetMax = new Vector2(-14f,-8f);
            DesktopWindowDragHandle drag =
                pageHeader.gameObject.AddComponent<DesktopWindowDragHandle>();
            drag.Window = page;
            TextMeshProUGUI pageTitle = UIFactory.Text(
                pageHeader,"Title",apps[i],25f,
                TextAlignmentOptions.Left);
            RectTransform titleRect = pageTitle.rectTransform;
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(18f,0f);
            titleRect.offsetMax = new Vector2(-18f,0f);

            RectTransform liveArea = UIFactory.Panel(
                page,"Live Content Area",Color.clear);
            liveArea.anchorMin = Vector2.zero;
            liveArea.anchorMax = new Vector2(1f,0.90f);
            liveArea.offsetMin = new Vector2(14f,12f);
            liveArea.offsetMax = new Vector2(-14f,-4f);
            RectTransform content = UIFactory.ScrollContent(
                liveArea,"Scrollable Content");
            UIFactory.Vertical(content,10f,18f);

            ui.ApplicationPages[i] = page;
            ui.ApplicationContents[i] = content;
            page.gameObject.SetActive(false);
        }

        ui.DeviceContent = ui.ApplicationContents[0];
    }

    private static Button BuildDesktopAppShortcut(
        Transform parent,string appName,string glyph,Color tileColor,Sprite icon,
        float iconSize)
    {
        RectTransform shortcut = UIFactory.Panel(
            parent,appName + " Shortcut",Color.clear);
        Button button = UIFactory.Button(
            shortcut,"Open " + appName,glyph,null,tileColor);
        RectTransform tile = button.GetComponent<RectTransform>();
        tile.anchorMin = new Vector2(0.22f,0.30f);
        tile.anchorMax = new Vector2(0.78f,1f);
        tile.offsetMin = tile.offsetMax = Vector2.zero;
        TextMeshProUGUI glyphText =
            button.GetComponentInChildren<TextMeshProUGUI>();
        glyphText.fontSize = glyph.Length > 2 ? 15f : 23f;
        glyphText.fontStyle = FontStyles.Bold;
        glyphText.color = UIFactory.Background;
        if (icon != null)
        {
            glyphText.gameObject.SetActive(false);
            RectTransform iconRect = UIFactory.Panel(
                button.transform,"Icon Image",Color.white);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f,0.5f);
            iconRect.sizeDelta = Vector2.one * iconSize;
            iconRect.anchoredPosition = Vector2.zero;
            Image image = iconRect.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }
        TextMeshProUGUI label = UIFactory.Text(
            shortcut,"App Label",appName,13f,TextAlignmentOptions.Center);
        label.color = new Color32(205,213,232,255);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = new Vector2(1f,0.25f);
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static void BuildMobileDevice(StoreUIAuthoring ui)
    {
        Color phoneBlack = new Color32(6,8,13,255);
        Color phoneSurface = new Color32(22,25,33,255);
        Color[] iconColors =
        {
            new Color32(166,239,46,255),
            new Color32(255,157,67,255),
            new Color32(55,190,226,255),
            new Color32(107,220,190,255),
            new Color32(173,122,244,255),
            new Color32(255,91,120,255),
            new Color32(82,88,107,255)
        };
        string[] apps =
            { "HOME", "SUPPLY", "REGISTER", "BANK", "HISTORY", "TASKS", "SETTINGS" };
        string[] icons = { "H", "BOX", "POS", "$", "LOG", "OK", "SET" };

        ui.MobileLayout = UIFactory.Panel(
            ui.DeviceRoot,"Mobile Layout",Color.clear);
        UIFactory.Stretch(ui.MobileLayout);
        ui.MobileFrame = UIFactory.Panel(
            ui.MobileLayout,"Phone Frame",phoneBlack);
        Object.DestroyImmediate(ui.MobileFrame.GetComponent<Image>());
        RoundedRectGraphic rounded =
            ui.MobileFrame.gameObject.AddComponent<RoundedRectGraphic>();
        rounded.color = phoneBlack;
        rounded.Radius = 48f;
        rounded.CornerSegments = 10;
        Mask phoneMask = ui.MobileFrame.gameObject.AddComponent<Mask>();
        phoneMask.showMaskGraphic = true;
        ui.MobileFrame.anchorMin = new Vector2(0.31f,0.015f);
        ui.MobileFrame.anchorMax = new Vector2(0.69f,0.985f);
        ui.MobileFrame.offsetMin = ui.MobileFrame.offsetMax = Vector2.zero;
        Outline outline = ui.MobileFrame.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(61,68,84,255);
        outline.effectDistance = new Vector2(4f,-4f);

        RectTransform status = UIFactory.Panel(
            ui.MobileFrame,"Status Bar",phoneBlack);
        status.anchorMin = new Vector2(0f,0.935f);
        status.anchorMax = Vector2.one;
        status.offsetMin = status.offsetMax = Vector2.zero;
        ui.MobileClock = UIFactory.Text(
            status,"Time","08:00",20f,TextAlignmentOptions.Left);
        ui.MobileClock.rectTransform.anchorMin = new Vector2(0.06f,0f);
        ui.MobileClock.rectTransform.anchorMax = new Vector2(0.30f,1f);
        ui.MobileClock.rectTransform.offsetMin =
            ui.MobileClock.rectTransform.offsetMax = Vector2.zero;
        TextMeshProUGUI signal = UIFactory.Text(
            status,"Signal","WIFI  100%",15f,TextAlignmentOptions.Right);
        signal.color = UIFactory.Muted;
        signal.rectTransform.anchorMin = new Vector2(0.65f,0f);
        signal.rectTransform.anchorMax = new Vector2(0.94f,1f);
        signal.rectTransform.offsetMin = signal.rectTransform.offsetMax = Vector2.zero;
        RectTransform notch = UIFactory.Panel(
            status,"Camera Notch",Color.black);
        notch.anchorMin = new Vector2(0.36f,0.46f);
        notch.anchorMax = new Vector2(0.64f,1f);
        notch.offsetMin = notch.offsetMax = Vector2.zero;

        RectTransform appHeader = UIFactory.Panel(
            ui.MobileFrame,"App Header",phoneSurface);
        appHeader.anchorMin = new Vector2(0f,0.84f);
        appHeader.anchorMax = new Vector2(1f,0.935f);
        appHeader.offsetMin = appHeader.offsetMax = Vector2.zero;
        TextMeshProUGUI storeName = UIFactory.Text(
            appHeader,"Store Name","QUICK STOP  #04",27f,
            TextAlignmentOptions.Left);
        storeName.rectTransform.anchorMin = new Vector2(0.06f,0f);
        storeName.rectTransform.anchorMax = new Vector2(0.72f,1f);
        storeName.rectTransform.offsetMin = storeName.rectTransform.offsetMax = Vector2.zero;
        ui.MobileTitle = storeName;
        ui.MobileCloseButton = UIFactory.Button(
            appHeader,"Close","X",null,new Color32(52,57,71,255));
        RectTransform closeRect = ui.MobileCloseButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.82f,0.18f);
        closeRect.anchorMax = new Vector2(0.94f,0.82f);
        closeRect.offsetMin = closeRect.offsetMax = Vector2.zero;

        ui.MobileBody = UIFactory.Panel(
            ui.MobileFrame,"Application View",phoneBlack);
        ui.MobileBody.anchorMin = new Vector2(0f,0.22f);
        ui.MobileBody.anchorMax = new Vector2(1f,0.84f);
        ui.MobileBody.offsetMin = ui.MobileBody.offsetMax = Vector2.zero;
        RectTransform pages = UIFactory.Panel(
            ui.MobileBody,"Portrait Application Pages",Color.clear);
        UIFactory.Stretch(pages);
        ui.MobileApplicationPages = new RectTransform[apps.Length];
        ui.MobileApplicationContents = new RectTransform[apps.Length];
        for (int i = 0; i < apps.Length; i++)
        {
            RectTransform page = UIFactory.Panel(
                pages,apps[i] + " Mobile Page",Color.clear);
            UIFactory.Stretch(page);
            TextMeshProUGUI title = UIFactory.Text(
                page,"Page Title",apps[i],22f,TextAlignmentOptions.Left);
            title.color = UIFactory.Accent;
            title.rectTransform.anchorMin = new Vector2(0.06f,0.90f);
            title.rectTransform.anchorMax = new Vector2(0.94f,1f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
            RectTransform liveArea = UIFactory.Panel(
                page,"Live Content Area",Color.clear);
            liveArea.anchorMin = new Vector2(0.03f,0f);
            liveArea.anchorMax = new Vector2(0.97f,0.90f);
            liveArea.offsetMin = liveArea.offsetMax = Vector2.zero;
            RectTransform content = UIFactory.ScrollContent(
                liveArea,"Scrollable Content");
            UIFactory.Vertical(content,9f,12f);
            ui.MobileApplicationPages[i] = page;
            ui.MobileApplicationContents[i] = content;
            page.gameObject.SetActive(i == 0);
        }
        ui.MobileContent = ui.MobileApplicationContents[0];

        ui.MobileNavigation = UIFactory.Panel(
            ui.MobileFrame,"App Icon Dock",phoneSurface);
        ui.MobileNavigation.anchorMin = new Vector2(0.025f,0.025f);
        ui.MobileNavigation.anchorMax = new Vector2(0.975f,0.20f);
        ui.MobileNavigation.offsetMin = ui.MobileNavigation.offsetMax = Vector2.zero;
        GridLayoutGroup grid = ui.MobileNavigation.gameObject.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(18,18,14,10);
        grid.spacing = new Vector2(10f,8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.cellSize = new Vector2(105f,72f);
        grid.childAlignment = TextAnchor.MiddleCenter;
        ui.MobileApplicationButtons = new Button[apps.Length];
        for (int i = 0; i < apps.Length; i++)
        {
            ui.MobileApplicationButtons[i] = BuildMobileAppIcon(
                ui.MobileNavigation,apps[i],icons[i],iconColors[i],phoneBlack);
        }

        RectTransform homeIndicator = UIFactory.Panel(
            ui.MobileFrame,"Home Indicator",new Color32(125,130,142,255));
        homeIndicator.anchorMin = new Vector2(0.38f,0.008f);
        homeIndicator.anchorMax = new Vector2(0.62f,0.014f);
        homeIndicator.offsetMin = homeIndicator.offsetMax = Vector2.zero;
    }

    private static Button BuildMobileAppIcon(
        Transform parent,
        string appName,
        string glyph,
        Color tileColor,
        Color darkText)
    {
        RectTransform cell = UIFactory.Panel(
            parent,appName + " App",Color.clear);
        Button button = UIFactory.Button(
            cell,"Icon",glyph,null,tileColor);
        RectTransform iconRect = button.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.20f,0.28f);
        iconRect.anchorMax = new Vector2(0.80f,1f);
        iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
        TextMeshProUGUI glyphText =
            button.GetComponentInChildren<TextMeshProUGUI>();
        glyphText.fontSize = glyph.Length > 1 ? 13f : 24f;
        glyphText.fontStyle = FontStyles.Bold;
        glyphText.color = darkText;

        TextMeshProUGUI nameText = UIFactory.Text(
            cell,"App Name",appName,11f,TextAlignmentOptions.Center);
        nameText.color = new Color32(180,190,215,255);
        nameText.rectTransform.anchorMin = Vector2.zero;
        nameText.rectTransform.anchorMax = new Vector2(1f,0.25f);
        nameText.rectTransform.offsetMin =
            nameText.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static Button MenuButton(
        Transform parent,
        string label,
        Color? color = null,
        float height = 58f)
    {
        Button button = UIFactory.Button(parent,label,label,null,color);
        UIFactory.Size(button,0f,height);
        return button;
    }
}
