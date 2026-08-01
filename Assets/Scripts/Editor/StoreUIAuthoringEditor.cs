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

        if (GUILayout.Button("Rebuild Authored Store UI",GUILayout.Height(30f)) &&
            EditorUtility.DisplayDialog(
                "Rebuild Store UI",
                "Replace the current authored UI hierarchy?",
                "Rebuild",
                "Cancel"))
        {
            StoreUIHierarchyBuilder.Build(authoring);
        }

        DrawDefaultInspector();
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
        ui.DeviceFrame = UIFactory.Panel(ui.DeviceRoot,"Device Frame",UIFactory.Background);
        ui.DeviceFrame.name = "Desktop Layout";
        ui.DeviceFrame.anchorMin = new Vector2(0.06f,0.06f);
        ui.DeviceFrame.anchorMax = new Vector2(0.94f,0.94f);
        ui.DeviceFrame.offsetMin = ui.DeviceFrame.offsetMax = Vector2.zero;
        Outline outline = ui.DeviceFrame.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f,0.28f,0.35f,1f);
        outline.effectDistance = new Vector2(3f,-3f);

        RectTransform header = UIFactory.Panel(ui.DeviceFrame,"Header",UIFactory.Surface);
        header.anchorMin = new Vector2(0f,0.91f);
        header.anchorMax = Vector2.one;
        header.offsetMin = header.offsetMax = Vector2.zero;
        UIFactory.Horizontal(header,10f,14f);
        ui.DeviceBrand = UIFactory.Text(header,"Brand","CLERK OS",25f,TextAlignmentOptions.Left);
        ui.DeviceBrand.color = UIFactory.Accent;
        UIFactory.Size(ui.DeviceBrand,180f,0f);
        ui.DeviceTitle = UIFactory.Text(header,"Application Title","OVERVIEW",22f,TextAlignmentOptions.Center);
        ui.DeviceCloseButton = UIFactory.Button(header,"Close","X",null,UIFactory.Danger);
        UIFactory.Size(ui.DeviceCloseButton,54f,48f);

        ui.DeviceNavigation = UIFactory.Panel(ui.DeviceFrame,"Applications",UIFactory.Surface);
        ui.DeviceNavigation.anchorMin = Vector2.zero;
        ui.DeviceNavigation.anchorMax = new Vector2(0.18f,0.91f);
        ui.DeviceNavigation.offsetMin = ui.DeviceNavigation.offsetMax = Vector2.zero;
        UIFactory.Vertical(ui.DeviceNavigation,7f,12f);

        string[] apps = { "OVERVIEW", "SUPPLY", "REGISTER", "BANK", "HISTORY", "TASKS", "SETTINGS" };
        List<Button> buttons = new List<Button>();
        for (int i = 0; i < apps.Length; i++)
        {
            Button button = UIFactory.Button(ui.DeviceNavigation,apps[i],apps[i],null);
            UIFactory.Size(button,0f,48f);
            buttons.Add(button);
        }
        ui.ApplicationButtons = buttons.ToArray();

        ui.DeviceBody = UIFactory.Panel(ui.DeviceFrame,"Workspace",UIFactory.Background);
        ui.DeviceBody.anchorMin = new Vector2(0.18f,0f);
        ui.DeviceBody.anchorMax = new Vector2(1f,0.91f);
        ui.DeviceBody.offsetMin = ui.DeviceBody.offsetMax = Vector2.zero;

        RectTransform pages = UIFactory.Panel(
            ui.DeviceBody,"Application Pages",Color.clear);
        UIFactory.Stretch(pages);
        ui.ApplicationPages = new RectTransform[apps.Length];
        ui.ApplicationContents = new RectTransform[apps.Length];

        for (int i = 0; i < apps.Length; i++)
        {
            RectTransform page = UIFactory.Panel(
                pages,apps[i] + " Page",Color.clear);
            UIFactory.Stretch(page);

            RectTransform pageHeader = UIFactory.Panel(
                page,"Page Header",UIFactory.Surface);
            pageHeader.anchorMin = new Vector2(0f,0.90f);
            pageHeader.anchorMax = Vector2.one;
            pageHeader.offsetMin = new Vector2(14f,6f);
            pageHeader.offsetMax = new Vector2(-14f,-8f);
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
            page.gameObject.SetActive(i == 0);
        }

        ui.DeviceContent = ui.ApplicationContents[0];
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
