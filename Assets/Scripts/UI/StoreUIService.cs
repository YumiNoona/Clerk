using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public enum StoreDeviceKind
{
    Desktop,
    Mobile
}

public enum StoreApplication
{
    Overview,
    Supply,
    Furniture,
    Register,
    Bank,
    History,
    Staff,
    Security,
    Messages,
    Tasks,
    Settings,
    Login,
    Mail,
    AppMarket,
    Todo,
    Weather,
    Notepad,
    Calculator,
    SaveLoad
}

public sealed class StoreUIService : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform hudRoot;
    private RectTransform deviceRoot;
    private RectTransform pauseRoot;
    private RectTransform mainMenuRoot;
    private RectTransform notificationRoot;
    private RectTransform priceEditorRoot;
    private RectTransform deviceContent;
    private TextMeshProUGUI moneyText;
    private TextMeshProUGUI clockText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI deviceTitle;
    private Coroutine notificationRoutine;
    private StoreDeviceKind activeDevice;
    private StoreApplication activeApplication;
    private int marketplaceTab;
    private int selectedMailIndex = -1;
    private int selectedConversationIndex = -1;
    private Camera selectedSecurityCamera;
    private RenderTexture securityPreviewTexture;
    private bool isRebinding;
    private bool showingMainMenu;
    private Button mainMenuStartButton;
    private ShelfSpaceController priceEditorShelf;
    private TMP_InputField priceInput;
    private TextMeshProUGUI priceDetailsText;
    private GameObject mobileModelPrefab;
    private GameObject mobileModelInstance;
    private StoreUIAuthoring authoredUI;
    private Canvas mobileScreenCanvas;
    private Transform deviceScreenParent;
    private Transform mobileHoldPoint;
    private PlayerInteractionController mobilePresentation;
    private Renderer mobileScreenRenderer;
    private RectTransform mobileRuntimeLayout;
    private RectTransform[] mobileRuntimePages;
    private RectTransform[] mobileRuntimeContents;
    private TextMeshProUGUI mobileRuntimeTitle;
    private bool mobileRuntimeBound;
    private RectTransform[] authoredApplicationPages;
    private RectTransform[] authoredApplicationContents;

    private void Awake()
    {
        // The runtime starts on the generated main menu. Pause before the
        // first rendered frame so the cursor never briefly enters locked
        // gameplay mode and consumes the player's first click.
        GameBootstrap.Instance?.GameplayModes.Pause();
    }

    private void Start()
    {
        if (!TryBindAuthoredUI())
        {
            Debug.LogWarning(
                "Authored Store UI was not found. Using the runtime fallback.",
                this);
            BuildCanvas();
            BuildHud();
            BuildPauseMenu();
            BuildMainMenu();
            BuildNotificationLayer();
            BuildPriceEditor();
        }

        GameBootstrap.Instance.GameplayModes.ModeChanged +=
            HandleModeChanged;

        GameBootstrap.Instance.Notifications
            .NotificationRaised +=
            HandleNotification;

        GameBootstrap.Instance.Days.DayEnded +=
            HandleDayEnded;

        HandleModeChanged(
            GameBootstrap.Instance.GameplayModes
                .CurrentMode);

        ShowMainMenu();
    }

    private void Update()
    {
        UpdateHud();


        if (priceEditorRoot != null &&
            priceEditorRoot.gameObject.activeSelf &&
            GameBootstrap.Instance.Input.WasPressedThisFrame(
                GameplayAction.Cancel))
        {
            ClosePriceEditor();
            return;
        }

        if (deviceRoot != null &&
            deviceRoot.gameObject.activeSelf &&
            !isRebinding &&
            GameBootstrap.Instance.Input.WasPressedThisFrame(
                GameplayAction.Cancel))
        {
            CloseDevice();
        }
    }

    public bool OpenPriceEditor(ShelfSpaceController shelf)
    {
        if (shelf == null || shelf.Info == null)
        {
            return false;
        }

        if (priceEditorRoot == null)
        {
            BuildPriceEditor();
        }

        if (priceEditorRoot == null ||
            !GameBootstrap.Instance.GameplayModes
                .TrySetMode(GameplayMode.PriceEditing))
        {
            return false;
        }

        priceEditorShelf = shelf;
        priceDetailsText.text =
            shelf.Info.ProductName +
            "\nBase price: $" +
            shelf.Info.BasePrice.ToString("0.00") +
            "\nCurrent price: $" +
            shelf.CurrentPrice.ToString("0.00");

        priceInput.text = shelf.CurrentPrice.ToString(
            "0.00",
            CultureInfo.InvariantCulture);
        priceEditorRoot.gameObject.SetActive(true);
        priceInput.Select();
        priceInput.ActivateInputField();
        return true;
    }

    private void ApplyPriceEditor()
    {
        if (priceEditorShelf == null || priceInput == null)
        {
            ClosePriceEditor();
            return;
        }

        if (!float.TryParse(
                priceInput.text.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float price) ||
            price < 0f)
        {
            GameBootstrap.Instance.Notifications.Show(
                "Enter a valid non-negative price, such as 2.99.",
                NotificationKind.Error);
            priceInput.Select();
            priceInput.ActivateInputField();
            return;
        }

        priceEditorShelf.SetCurrentPrice(price);
        GameBootstrap.Instance.Notifications.Show(
            "Shelf price updated to $" + price.ToString("0.00") + ".",
            NotificationKind.Success);
        ClosePriceEditor();
    }

    private void ClosePriceEditor()
    {
        if (priceEditorRoot != null)
        {
            priceEditorRoot.gameObject.SetActive(false);
        }

        priceEditorShelf = null;

        if (GameBootstrap.Instance != null &&
            GameBootstrap.Instance.GameplayModes.CurrentMode ==
                GameplayMode.PriceEditing)
        {
            GameBootstrap.Instance.GameplayModes
                .TrySetMode(GameplayMode.Gameplay);
        }
    }

    public void OpenDevice(StoreDeviceKind kind)
    {
        if (canvas == null)
        {
            BuildCanvas();
        }

        activeDevice = kind;
        SetMobileModelVisible(
            kind == StoreDeviceKind.Mobile);
        BuildDeviceShell(kind);

        if (kind == StoreDeviceKind.Mobile)
        {
            AttachDeviceToMobileScreen();
        }

        deviceRoot.gameObject.SetActive(
            kind == StoreDeviceKind.Desktop);

        if (kind == StoreDeviceKind.Desktop && authoredApplicationPages != null)
        {
            for (int i = 0; i < authoredApplicationPages.Length; i++)
            {
                authoredApplicationPages[i].gameObject.SetActive(false);
            }
            deviceTitle.text = "DESKTOP";
            deviceContent = null;
        }

        GameBootstrap.Instance.GameplayModes
            .TrySetMode(GameplayMode.DeviceUI);
    }

    public void CloseDevice()
    {
        ReleaseSecurityPreview();
        if (deviceRoot != null)
        {
            deviceRoot.gameObject.SetActive(false);
        }

        SetMobileModelVisible(false);

        if (GameBootstrap.Instance.GameplayModes
                .CurrentMode == GameplayMode.DeviceUI)
        {
            GameBootstrap.Instance.GameplayModes
                .TrySetMode(GameplayMode.Gameplay);
        }
    }

    public void ConfigureMobileModel(
        GameObject modelPrefab)
    {
        mobileModelPrefab = modelPrefab;
    }

    private void SetMobileModelVisible(bool visible)
    {
        if (!visible)
        {
            RestoreDeviceToScreenCanvas();

            if (mobileScreenRenderer != null)
            {
                mobileScreenRenderer.enabled = true;
            }

            if (mobileModelInstance != null)
            {
                mobileModelInstance.SetActive(false);
            }

            return;
        }

        ResolveMobileHoldPoint();

        if (mobileModelInstance == null && mobileHoldPoint != null)
        {
            Transform placedPhone = FindNamedChild(
                mobileHoldPoint,"Mobile");
            if (placedPhone != null)
            {
                mobileModelInstance = placedPhone.gameObject;
            }
        }

        if (mobileModelInstance == null &&
            mobileModelPrefab != null &&
            Camera.main != null)
        {
            mobileModelInstance =
                Instantiate(
                    mobileModelPrefab,
                    mobileHoldPoint != null
                        ? mobileHoldPoint
                        : Camera.main.transform);

            mobileModelInstance.name =
                "Player Mobile Device";

            if (mobileModelInstance.GetComponent<MobileDeviceView>() == null)
            {
                mobileModelInstance.transform.localPosition =
                    mobilePresentation != null
                        ? mobilePresentation.MobileModelLocalPosition
                        : Vector3.zero;
                mobileModelInstance.transform.localRotation =
                    Quaternion.Euler(
                        mobilePresentation != null
                            ? mobilePresentation.MobileModelLocalEulerAngles
                            : new Vector3(0f,180f,0f));
                mobileModelInstance.transform.localScale =
                    Vector3.one * (mobilePresentation != null
                        ? mobilePresentation.MobileModelScale
                        : 0.32f);
            }
        }

        if (mobileModelInstance != null)
        {
            mobileModelInstance.SetActive(true);
            MobileDeviceView prefabView =
                mobileModelInstance.GetComponent<MobileDeviceView>();
            if (prefabView != null && prefabView.ScreenCanvas != null)
            {
                mobileScreenCanvas = prefabView.ScreenCanvas;
                mobileRuntimeLayout = prefabView.MobileLayout;
                mobileScreenRenderer = prefabView.ScreenRenderer;
                mobileScreenCanvas.worldCamera = Camera.main;
            }
            Transform screen = FindNamedChild(
                mobileModelInstance.transform,"Screen");
            mobileScreenRenderer ??= screen != null
                ? screen.GetComponent<Renderer>()
                : null;
            if (mobileScreenRenderer != null)
            {
                mobileScreenRenderer.enabled = false;
            }
            AttachDeviceToMobileScreen();
        }
    }

    private void AttachDeviceToMobileScreen()
    {
        if (deviceRoot == null || Camera.main == null)
        {
            return;
        }

        if (mobileScreenCanvas == null)
        {
            Debug.LogError(
                "The Mobile prefab has no editable MobileDeviceView canvas. " +
                "Open Assets/Models/UI/Mobile.prefab or run Clerk > Setup > " +
                "Rebuild Editable Mobile UI Prefab.",this);
            return;
        }

        EnsureMobileRuntimeView();
        mobileScreenCanvas.gameObject.SetActive(true);
    }

    private void ResolveMobileHoldPoint()
    {
        if (mobileHoldPoint != null)
        {
            return;
        }

        PlayerInteractionController player =
            FindAnyObjectByType<PlayerInteractionController>();

        mobilePresentation = player;

        mobileHoldPoint = player != null
            ? player.MobileHoldPoint
            : Camera.main != null
                ? Camera.main.transform
                : null;
    }

    private static Transform FindNamedChild(
        Transform root,
        string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.Equals(
                    childName,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }

            Transform nested = FindNamedChild(child,childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private void RestoreDeviceToScreenCanvas()
    {
        if (mobileScreenCanvas != null)
        {
            mobileScreenCanvas.gameObject.SetActive(false);
        }
    }

    private void EnsureMobileRuntimeView()
    {
        if (mobileRuntimeLayout == null)
        {
            Debug.LogError(
                "Mobile.prefab is missing its authored Mobile Layout.",this);
            return;
        }

        mobileRuntimeLayout.gameObject.SetActive(true);

        if (mobileRuntimeBound)
        {
            return;
        }

        mobileRuntimeTitle = mobileRuntimeLayout.Find(
            "Phone Frame/App Header/Store Name")
            ?.GetComponent<TextMeshProUGUI>();
        BindButton(
            mobileRuntimeLayout.Find("Phone Frame/App Header/Close")
                ?.GetComponent<Button>(),
            CloseDevice);

        StoreApplication[] applications = GetNavigationApplications();
        mobileRuntimePages = new RectTransform[applications.Length];
        mobileRuntimeContents = new RectTransform[applications.Length];
        for (int i = 0; i < applications.Length; i++)
        {
            string label = i == 0
                ? "HOME"
                : GetApplicationTitle(applications[i]);
            Transform page = mobileRuntimeLayout.Find(
                "Phone Frame/Application View/Portrait Application Pages/" +
                label + " Mobile Page");
            mobileRuntimePages[i] = page as RectTransform;
            mobileRuntimeContents[i] = page?.Find(
                "Live Content Area/Scrollable Content/Viewport/Content")
                as RectTransform;
            Button button = mobileRuntimeLayout.Find(
                "Phone Frame/App Icon Dock/" + label + " App/Icon")
                ?.GetComponent<Button>();
            StoreApplication application = applications[i];
            BindButton(button,() => ShowApplication(application));
        }
        mobileRuntimeBound = true;
    }

    public void ShowApplication(
        StoreApplication application)
    {
        if (activeApplication == StoreApplication.Security &&
            application != StoreApplication.Security)
        {
            ReleaseSecurityPreview();
        }

        if (authoredUI != null)
        {
            int pageIndex = GetNavigationApplicationIndex(application);

            if (pageIndex >= 0 &&
                pageIndex < authoredApplicationPages.Length)
            {
                for (int i = 0; i < authoredApplicationPages.Length; i++)
                {
                    authoredApplicationPages[i].gameObject.SetActive(
                        i == pageIndex);
                }

                deviceContent = authoredApplicationContents[pageIndex];
            }
        }

        if (deviceContent == null)
        {
            return;
        }

        activeApplication = application;
        deviceTitle.text =
            GetApplicationTitle(application);

        UIFactory.Clear(deviceContent);

        switch (application)
        {
            case StoreApplication.Overview:
                BuildOverview(deviceContent);
                break;
            case StoreApplication.Supply:
                BuildStoreMarketplace(deviceContent);
                break;
            case StoreApplication.Furniture:
                BuildFurnitureCatalog(deviceContent);
                break;
            case StoreApplication.Register:
                BuildRegister(deviceContent);
                break;
            case StoreApplication.Bank:
                BuildBank(deviceContent);
                break;
            case StoreApplication.History:
                BuildTransactionHistory(deviceContent);
                break;
            case StoreApplication.Staff:
                BuildStaff(deviceContent);
                break;
            case StoreApplication.Security:
                BuildSecurity(deviceContent);
                break;
            case StoreApplication.Messages:
                BuildMessages(deviceContent);
                break;
            case StoreApplication.Tasks:
                BuildCareerBoard(deviceContent);
                break;
            case StoreApplication.Settings:
                BuildSettings(deviceContent);
                break;
            case StoreApplication.Login:
                BuildLogin(deviceContent);
                break;
            case StoreApplication.Mail:
                BuildMail(deviceContent);
                break;
            case StoreApplication.AppMarket:
                BuildAppMarket(deviceContent);
                break;
            case StoreApplication.Todo:
                BuildTodo(deviceContent);
                break;
            case StoreApplication.Weather:
                BuildWeather(deviceContent);
                break;
            case StoreApplication.Notepad:
                BuildNotepad(deviceContent);
                break;
            case StoreApplication.Calculator:
                BuildCalculator(deviceContent);
                break;
            case StoreApplication.SaveLoad:
                BuildSaveLoad(deviceContent);
                break;
        }
    }

    private void BuildCanvas()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject canvasObject =
            new GameObject(
                "Clerk UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

        canvasObject.transform.SetParent(
            transform,
            false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode
                .ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f,1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (EventSystem.current == null)
        {
            GameObject events =
                new GameObject(
                    "UI Event System",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));

            events.transform.SetParent(transform,false);
        }
    }

    private void BuildHud()
    {
        hudRoot =
            UIFactory.Panel(
                canvas.transform,
                "Gameplay HUD",
                Color.clear);

        RectTransform top =
            UIFactory.Panel(
                hudRoot,
                "Top Bar",
                new Color(0.04f,0.05f,0.07f,0.9f));

        top.anchorMin = new Vector2(0.02f,0.92f);
        top.anchorMax = new Vector2(0.98f,0.985f);
        top.offsetMin = Vector2.zero;
        top.offsetMax = Vector2.zero;

        UIFactory.Horizontal(top,16f,18f);

        moneyText =
            UIFactory.Text(
                top,
                "Money",
                "$0.00",
                25f,
                TextAlignmentOptions.Left);

        moneyText.color = UIFactory.Accent;
        UIFactory.Size(moneyText,300f,60f);

        clockText =
            UIFactory.Text(
                top,
                "Clock",
                "DAY 1  08:00",
                22f,
                TextAlignmentOptions.Center);

        statusText =
            UIFactory.Text(
                top,
                "Status",
                "STORE CLOSED",
                20f,
                TextAlignmentOptions.Right);

        UIFactory.Size(statusText,420f,60f);

        TextMeshProUGUI crosshair =
            UIFactory.Text(
                hudRoot,
                "Crosshair",
                "+",
                30f,
                TextAlignmentOptions.Center);

        crosshair.color = new Color(1f,1f,1f,0.9f);
        RectTransform crosshairRect = crosshair.rectTransform;
        crosshairRect.anchorMin = new Vector2(0.5f,0.5f);
        crosshairRect.anchorMax = new Vector2(0.5f,0.5f);
        crosshairRect.pivot = new Vector2(0.5f,0.5f);
        crosshairRect.anchoredPosition = Vector2.zero;
        crosshairRect.sizeDelta = new Vector2(32f,32f);
    }

    private void BuildPauseMenu()
    {
        pauseRoot =
            UIFactory.Panel(
                canvas.transform,
                "Pause Menu",
                new Color(0.02f,0.025f,0.04f,0.94f));

        RectTransform menu =
            UIFactory.Panel(
                pauseRoot,
                "Pause Card",
                UIFactory.Surface);

        menu.anchorMin = new Vector2(0.37f,0.2f);
        menu.anchorMax = new Vector2(0.63f,0.8f);
        menu.offsetMin = Vector2.zero;
        menu.offsetMax = Vector2.zero;
        UIFactory.Vertical(menu,14f,26f);

        TextMeshProUGUI title =
            UIFactory.Text(
                menu,
                "Title",
                "CLERK",
                48f,
                TextAlignmentOptions.Center);

        title.color = UIFactory.Accent;
        UIFactory.Size(title,0f,90f);

        AddMenuButton(
            menu,
            "RESUME",
            () => GameBootstrap.Instance
                .GameplayModes.Resume());

        AddMenuButton(
            menu,
            "DESKTOP",
            () =>
            {
                GameBootstrap.Instance
                    .GameplayModes.Resume();
                OpenDevice(StoreDeviceKind.Desktop);
            });

        AddMenuButton(
            menu,
            "SAVE GAME",
            () => SaveSlot(0));

        AddMenuButton(
            menu,
            "LOAD GAME",
            () => LoadSlot(0));

        AddMenuButton(
            menu,
            "QUIT",
            QuitGame,
            UIFactory.Danger);
    }

    private void BuildMainMenu()
    {
        mainMenuRoot =
            UIFactory.Panel(
                canvas.transform,
                "Main Menu",
                UIFactory.Background);

        RectTransform menu =
            UIFactory.Panel(
                mainMenuRoot,
                "Main Menu Card",
                UIFactory.Surface);

        menu.anchorMin = new Vector2(0.32f,0.16f);
        menu.anchorMax = new Vector2(0.68f,0.84f);
        menu.offsetMin = Vector2.zero;
        menu.offsetMax = Vector2.zero;
        UIFactory.Vertical(menu,16f,32f);

        TextMeshProUGUI title =
            UIFactory.Text(
                menu,
                "Title",
                "CLERK",
                72f,
                TextAlignmentOptions.Center);

        title.color = UIFactory.Accent;
        UIFactory.Size(title,0f,120f);

        TextMeshProUGUI subtitle =
            UIFactory.Text(
                menu,
                "Subtitle",
                "BUILD · STOCK · SERVE · GROW",
                18f,
                TextAlignmentOptions.Center);

        subtitle.color = UIFactory.Muted;
        UIFactory.Size(subtitle,0f,44f);

        mainMenuStartButton =
            UIFactory.Button(
                menu,
                "START STORE",
                "START STORE",
                StartStore,
                UIFactory.Accent);

        UIFactory.Size(
            mainMenuStartButton,
            0f,
            58f);

        if (GameBootstrap.Instance.Saves.SaveExists(0))
        {
            AddMenuButton(
                menu,
                "CONTINUE",
                () =>
                {
                    LoadSlot(0);
                    StartStore();
                });
        }

        AddMenuButton(
            menu,
            "SETTINGS",
            () =>
            {
                showingMainMenu = false;
                mainMenuRoot.gameObject
                    .SetActive(false);
                GameBootstrap.Instance
                    .GameplayModes.Resume();
                OpenDevice(
                    StoreDeviceKind.Desktop);
                ShowApplication(
                    StoreApplication.Settings);
            });

        AddMenuButton(
            menu,
            "QUIT",
            QuitGame,
            UIFactory.Danger);

        mainMenuRoot.gameObject.SetActive(false);
    }

    private void ShowMainMenu()
    {
        showingMainMenu = true;
        mainMenuRoot.gameObject.SetActive(true);
        pauseRoot.gameObject.SetActive(false);
        hudRoot.gameObject.SetActive(false);
        GameBootstrap.Instance.GameplayModes.Pause();
        GameBootstrap.Instance.GameplayModes
            .RefreshPresentation();

        Canvas.ForceUpdateCanvases();

        if (EventSystem.current != null &&
            mainMenuStartButton != null)
        {
            EventSystem.current.SetSelectedGameObject(
                mainMenuStartButton.gameObject);
        }
    }

    private void StartStore()
    {
        showingMainMenu = false;
        mainMenuRoot.gameObject.SetActive(false);
        GameBootstrap.Instance.GameplayModes.Resume();

        if (!GameBootstrap.Instance.Days.IsDayRunning)
        {
            GameBootstrap.Instance.Days.StartDay();
        }
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void AddMenuButton(
        Transform parent,
        string label,
        UnityEngine.Events.UnityAction action,
        Color? color = null)
    {
        Button button =
            UIFactory.Button(
                parent,
                label,
                label,
                action,
                color);

        UIFactory.Size(button,0f,58f);
    }

    private void BuildNotificationLayer()
    {
        notificationRoot =
            UIFactory.Panel(
                canvas.transform,
                "Notifications",
                Color.clear);

        notificationRoot.anchorMin =
            new Vector2(0.67f,0.7f);
        notificationRoot.anchorMax =
            new Vector2(0.98f,0.9f);
        notificationRoot.offsetMin = Vector2.zero;
        notificationRoot.offsetMax = Vector2.zero;
        notificationRoot.gameObject.SetActive(false);
    }

    private bool TryBindAuthoredUI()
    {
        authoredUI = FindAnyObjectByType<StoreUIAuthoring>(
            FindObjectsInactive.Include);

        if (authoredUI == null || !authoredUI.IsComplete)
        {
            authoredUI = null;
            return false;
        }

        canvas = authoredUI.Canvas;
        hudRoot = authoredUI.HudRoot;
        pauseRoot = authoredUI.PauseRoot;
        mainMenuRoot = authoredUI.MainMenuRoot;
        notificationRoot = authoredUI.NotificationRoot;
        priceEditorRoot = authoredUI.PriceEditorRoot;
        deviceRoot = authoredUI.DeviceRoot;
        deviceContent = authoredUI.DeviceContent;
        authoredApplicationPages = authoredUI.ApplicationPages;
        authoredApplicationContents = authoredUI.ApplicationContents;
        moneyText = authoredUI.MoneyText;
        clockText = authoredUI.ClockText;
        statusText = authoredUI.StatusText;
        deviceTitle = authoredUI.DeviceTitle;
        mainMenuStartButton = authoredUI.StartButton;
        priceInput = authoredUI.PriceInput;
        priceDetailsText = authoredUI.PriceDetailsText;

        BindButton(authoredUI.StartButton,StartStore);
        BindButton(authoredUI.ContinueButton,() =>
        {
            LoadSlot(0);
            StartStore();
        });
        authoredUI.ContinueButton.gameObject.SetActive(
            GameBootstrap.Instance.Saves.SaveExists(0));
        BindButton(authoredUI.MainSettingsButton,() =>
        {
            showingMainMenu = false;
            mainMenuRoot.gameObject.SetActive(false);
            GameBootstrap.Instance.GameplayModes.Resume();
            OpenDevice(StoreDeviceKind.Desktop);
            ShowApplication(StoreApplication.Settings);
        });
        BindButton(authoredUI.MainQuitButton,QuitGame);

        BindButton(authoredUI.ResumeButton,() =>
            GameBootstrap.Instance.GameplayModes.Resume());
        BindButton(authoredUI.DesktopButton,() =>
        {
            GameBootstrap.Instance.GameplayModes.Resume();
            OpenDevice(StoreDeviceKind.Desktop);
        });
        BindButton(authoredUI.SaveButton,() => SaveSlot(0));
        BindButton(authoredUI.LoadButton,() => LoadSlot(0));
        BindButton(authoredUI.PauseQuitButton,QuitGame);
        BindButton(authoredUI.ApplyPriceButton,ApplyPriceEditor);
        BindButton(authoredUI.CancelPriceButton,ClosePriceEditor);
        BindButton(authoredUI.DeviceCloseButton,CloseDevice);
        BindButton(authoredUI.TaskbarStartButton,() =>
        {
            if (GameBootstrap.Instance.Days.IsDayRunning)
            {
                GameBootstrap.Instance.Days.EndDay();
            }
            else
            {
                GameBootstrap.Instance.Days.StartDay();
            }
            UpdateHud();
        });

        StoreApplication[] applications = GetNavigationApplications();
        int count = Mathf.Min(
            applications.Length,
            authoredUI.ApplicationButtons.Length);

        for (int i = 0; i < count; i++)
        {
            StoreApplication application = applications[i];
            authoredUI.ApplicationButtons[i].transform.parent.gameObject.SetActive(
                IsCoreApplication(application) || IsApplicationInstalled(application));
            BindButton(
                authoredUI.ApplicationButtons[i],
                () => ShowApplication(application));
        }

        priceInput.onSubmit.RemoveAllListeners();
        priceInput.onSubmit.AddListener(_ => ApplyPriceEditor());
        pauseRoot.gameObject.SetActive(false);
        mainMenuRoot.gameObject.SetActive(false);
        priceEditorRoot.gameObject.SetActive(false);
        notificationRoot.gameObject.SetActive(false);
        deviceRoot.gameObject.SetActive(false);
        return true;
    }

    private static void BindButton(
        Button button,
        UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static StoreApplication[] GetNavigationApplications()
    {
        return new[]
        {
            StoreApplication.Overview,
            StoreApplication.Supply,
            StoreApplication.Register,
            StoreApplication.Bank,
            StoreApplication.Staff,
            StoreApplication.Tasks,
            StoreApplication.History,
            StoreApplication.Settings,
            StoreApplication.Login,
            StoreApplication.Mail,
            StoreApplication.AppMarket,
            StoreApplication.Messages,
            StoreApplication.Security,
            StoreApplication.Todo,
            StoreApplication.Weather,
            StoreApplication.Notepad,
            StoreApplication.Calculator
        };
    }

    private static int GetNavigationApplicationIndex(
        StoreApplication application)
    {
        StoreApplication[] applications =
            GetNavigationApplications();

        for (int i = 0; i < applications.Length; i++)
        {
            if (applications[i] == application)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsCoreApplication(StoreApplication application)
    {
        return application == StoreApplication.Overview ||
            application == StoreApplication.Supply ||
            application == StoreApplication.Register ||
            application == StoreApplication.Bank ||
            application == StoreApplication.Staff ||
            application == StoreApplication.Tasks ||
            application == StoreApplication.History ||
            application == StoreApplication.Settings ||
            application == StoreApplication.Login ||
            application == StoreApplication.Mail ||
            application == StoreApplication.AppMarket;
    }

    private static string GetInstallKey(StoreApplication application) =>
        "Clerk.DesktopApp." + application;

    private static bool IsApplicationInstalled(StoreApplication application) =>
        IsCoreApplication(application) || PlayerPrefs.GetInt(GetInstallKey(application),0) == 1;

    private void InstallApplication(StoreApplication application)
    {
        PlayerPrefs.SetInt(GetInstallKey(application),1);
        PlayerPrefs.Save();
        int index = GetNavigationApplicationIndex(application);
        if (authoredUI != null && index >= 0 && index < authoredUI.ApplicationButtons.Length)
        {
            authoredUI.ApplicationButtons[index].transform.parent.gameObject.SetActive(true);
        }
        GameBootstrap.Instance.Notifications.Show(
            GetApplicationTitle(application) + " installed.",NotificationKind.Success);
        ShowApplication(StoreApplication.AppMarket);
    }

    private void BuildPriceEditor()
    {
        if (canvas == null || priceEditorRoot != null)
        {
            return;
        }

        priceEditorRoot = UIFactory.Panel(
            canvas.transform,
            "Shelf Price Editor",
            new Color(0f,0f,0f,0.72f));

        RectTransform card = UIFactory.Panel(
            priceEditorRoot,
            "Price Card",
            UIFactory.Surface);
        card.anchorMin = new Vector2(0.37f,0.28f);
        card.anchorMax = new Vector2(0.63f,0.72f);
        card.offsetMin = Vector2.zero;
        card.offsetMax = Vector2.zero;
        UIFactory.Vertical(card,14f,28f);

        TextMeshProUGUI title = UIFactory.Text(
            card,
            "Title",
            "SET SHELF PRICE",
            30f,
            TextAlignmentOptions.Center);
        title.color = UIFactory.Accent;
        UIFactory.Size(title,0f,58f);

        priceDetailsText = UIFactory.Text(
            card,
            "Price Details",
            string.Empty,
            20f,
            TextAlignmentOptions.Center);
        UIFactory.Size(priceDetailsText,0f,105f);

        RectTransform inputRoot = UIFactory.Panel(
            card,
            "Price Input",
            UIFactory.SurfaceRaised);
        UIFactory.Size(inputRoot,0f,58f);

        priceInput = inputRoot.gameObject.AddComponent<TMP_InputField>();
        TextMeshProUGUI inputText = UIFactory.Text(
            inputRoot,
            "Text",
            string.Empty,
            24f,
            TextAlignmentOptions.Center);
        inputText.raycastTarget = false;
        priceInput.textComponent = inputText;
        priceInput.contentType = TMP_InputField.ContentType.DecimalNumber;
        priceInput.lineType = TMP_InputField.LineType.SingleLine;
        priceInput.onSubmit.AddListener(_ => ApplyPriceEditor());

        Button apply = UIFactory.Button(
            card,
            "Apply Price",
            "APPLY PRICE",
            ApplyPriceEditor,
            UIFactory.Accent);
        UIFactory.Size(apply,0f,54f);

        Button cancel = UIFactory.Button(
            card,
            "Cancel",
            "CANCEL",
            ClosePriceEditor);
        UIFactory.Size(cancel,0f,48f);

        priceEditorRoot.gameObject.SetActive(false);
    }

    private void BuildDeviceShell(StoreDeviceKind kind)
    {
        if (authoredUI != null)
        {
            ConfigureAuthoredDevice(kind);
            return;
        }

        if (deviceRoot != null)
        {
            Destroy(deviceRoot.gameObject);
        }

        deviceRoot =
            UIFactory.Panel(
                canvas.transform,
                kind + " UI",
                new Color(0f,0f,0f,0.78f));

        RectTransform frame =
            UIFactory.Panel(
                deviceRoot,
                "Device Frame",
                UIFactory.Background);

        if (kind == StoreDeviceKind.Desktop)
        {
            frame.anchorMin =
                new Vector2(0.06f,0.06f);
            frame.anchorMax =
                new Vector2(0.94f,0.94f);
        }
        else
        {
            frame.anchorMin =
                new Vector2(0.36f,0.04f);
            frame.anchorMax =
                new Vector2(0.64f,0.96f);
        }

        frame.offsetMin = Vector2.zero;
        frame.offsetMax = Vector2.zero;

        Outline outline =
            frame.gameObject.AddComponent<Outline>();
        outline.effectColor =
            new Color(0.25f,0.28f,0.35f,1f);
        outline.effectDistance =
            new Vector2(3f,-3f);

        RectTransform header =
            UIFactory.Panel(
                frame,
                "Header",
                UIFactory.Surface);

        header.anchorMin = new Vector2(0f,0.91f);
        header.anchorMax = Vector2.one;
        header.offsetMin = Vector2.zero;
        header.offsetMax = Vector2.zero;
        UIFactory.Horizontal(header,10f,14f);

        TextMeshProUGUI brand =
            UIFactory.Text(
                header,
                "Brand",
                kind == StoreDeviceKind.Desktop
                    ? "CLERK OS"
                    : "CLERK",
                25f,
                TextAlignmentOptions.Left);

        brand.color = UIFactory.Accent;
        UIFactory.Size(brand,180f,0f);

        deviceTitle =
            UIFactory.Text(
                header,
                "Application Title",
                "OVERVIEW",
                22f,
                TextAlignmentOptions.Center);

        Button close =
            UIFactory.Button(
                header,
                "Close",
                "×",
                CloseDevice,
                UIFactory.Danger);

        UIFactory.Size(close,54f,48f);

        RectTransform navigation =
            UIFactory.Panel(
                frame,
                "Applications",
                UIFactory.Surface);

        RectTransform body =
            UIFactory.Panel(
                frame,
                "Workspace",
                UIFactory.Background);

        if (kind == StoreDeviceKind.Desktop)
        {
            navigation.anchorMin =
                new Vector2(0f,0f);
            navigation.anchorMax =
                new Vector2(0.18f,0.91f);

            body.anchorMin =
                new Vector2(0.18f,0f);
            body.anchorMax =
                new Vector2(1f,0.91f);
        }
        else
        {
            navigation.anchorMin =
                new Vector2(0f,0f);
            navigation.anchorMax =
                new Vector2(1f,0.12f);

            body.anchorMin =
                new Vector2(0f,0.12f);
            body.anchorMax =
                new Vector2(1f,0.91f);
        }

        navigation.offsetMin = Vector2.zero;
        navigation.offsetMax = Vector2.zero;
        body.offsetMin = Vector2.zero;
        body.offsetMax = Vector2.zero;

        if (kind == StoreDeviceKind.Desktop)
        {
            UIFactory.Vertical(navigation,7f,12f);
        }
        else
        {
            UIFactory.Horizontal(navigation,5f,7f);
        }

        StoreApplication[] apps =
            GetNavigationApplications();

        for (int i = 0; i < apps.Length; i++)
        {
            StoreApplication app = apps[i];
            string label =
                kind == StoreDeviceKind.Mobile
                    ? GetMobileIcon(app)
                    : GetApplicationTitle(app);

            Button appButton =
                UIFactory.Button(
                    navigation,
                    app.ToString(),
                    label,
                    () => ShowApplication(app));

            UIFactory.Size(
                appButton,
                kind == StoreDeviceKind.Mobile
                    ? 50f
                    : 0f,
                kind == StoreDeviceKind.Mobile
                    ? 58f
                    : 48f);
        }

        deviceContent =
            UIFactory.ScrollContent(
                body,
                "Scrollable Content");

        UIFactory.Vertical(deviceContent,10f,18f);
    }

    private void ConfigureAuthoredDevice(StoreDeviceKind kind)
    {
        if (kind == StoreDeviceKind.Desktop)
        {
            authoredUI.DeviceFrame.gameObject.SetActive(true);
            deviceContent = authoredUI.DeviceContent;
            deviceTitle = authoredUI.DeviceTitle;
            authoredApplicationPages = authoredUI.ApplicationPages;
            authoredApplicationContents = authoredUI.ApplicationContents;
        }
        else
        {
            authoredUI.DeviceFrame.gameObject.SetActive(false);
            authoredUI.MobileLayout.gameObject.SetActive(false);
            EnsureMobileRuntimeView();
            if (mobileRuntimeContents == null ||
                mobileRuntimeContents.Length == 0)
            {
                return;
            }
            deviceContent = mobileRuntimeContents[0];
            deviceTitle = mobileRuntimeTitle;
            authoredApplicationPages = mobileRuntimePages;
            authoredApplicationContents = mobileRuntimeContents;
        }

        if (deviceContent != null)
        {
            UIFactory.Clear(deviceContent);
        }
    }

    private void BuildOverview(Transform parent)
    {
        AddSectionTitle(parent,"STORE PULSE");

        int day =
            GameBootstrap.Instance.Days.CurrentDay;

        AddMetric(
            parent,
            "TODAY'S REVENUE",
            FormatMoney(
                GameBootstrap.Instance.Economy
                    .GetRevenueForDay(day)));

        AddMetric(
            parent,
            "TODAY'S PROFIT",
            FormatMoney(
                GameBootstrap.Instance.Economy
                    .GetProfitForDay(day)));

        StoreStatisticsData stats =
            GameBootstrap.Instance.Statistics.Data;

        AddMetric(
            parent,
            "CUSTOMERS SERVED",
            stats.CustomersServed.ToString());

        AddMetric(
            parent,
            "ITEMS SOLD",
            stats.ItemsSold.ToString());

        AddMetric(
            parent,
            "STORE LEVEL",
            GameBootstrap.Instance.Progression
                .StoreLevel.ToString());

        AddActionButton(
            parent,
            "OPEN / CLOSE STORE",
            () =>
            {
                if (GameBootstrap.Instance.Days
                        .IsDayRunning)
                {
                    GameBootstrap.Instance.Days.EndDay();
                }
                else
                {
                    GameBootstrap.Instance.Days.StartDay();
                }

                ShowApplication(activeApplication);
            });
    }

    private void BuildStockCatalog(Transform parent)
    {
        AddSectionTitle(parent,"PRODUCT STOCK");

        PurchaseCatalog catalog =
            PurchaseService.Instance != null
                ? PurchaseService.Instance.PurchaseCatalog
                : null;

        if (catalog == null ||
            catalog.StockPurchases.Count == 0)
        {
            AddEmptyState(
                parent,
                "No stock products are configured.");
            return;
        }

        for (int i = 0;
             i < catalog.StockPurchases.Count;
             i++)
        {
            StockPurchaseData purchase =
                catalog.StockPurchases[i];

            if (purchase == null)
            {
                continue;
            }

            AddPurchaseRow(
                parent,
                purchase.DisplayName,
                purchase.QuantityPerBox +
                " items · $" +
                purchase.PurchasePrice.ToString("0.00"),
                () =>
                {
                    bool success =
                        PurchaseService.Instance
                            .TryPurchaseStock(purchase);

                    NotifyPurchase(
                        success,
                        purchase.DisplayName);
                });
        }

    }

    private void BuildStoreMarketplace(Transform parent)
    {
        AddSectionTitle(parent,"CLERK MARKETPLACE");
        AddMetric(parent,"DEPARTMENT",
            marketplaceTab == 0 ? "PRODUCT STOCK" : "STORE FURNITURE");
        AddActionButton(parent,"PRODUCT STOCK",() =>
        {
            marketplaceTab = 0;
            ShowApplication(StoreApplication.Supply);
        });
        AddActionButton(parent,"FURNITURE",() =>
        {
            marketplaceTab = 1;
            ShowApplication(StoreApplication.Supply);
        });

        if (marketplaceTab == 0)
        {
            BuildStockCatalog(parent);
        }
        else
        {
            BuildFurnitureCatalog(parent);
        }
    }

    private void BuildFurnitureCatalog(Transform parent)
    {
        AddSectionTitle(parent,"FURNITURE");

        PurchaseCatalog catalog =
            PurchaseService.Instance != null
                ? PurchaseService.Instance.PurchaseCatalog
                : null;

        if (catalog == null ||
            catalog.FurniturePurchases.Count == 0)
        {
            AddEmptyState(
                parent,
                "No furniture is configured.");
            return;
        }

        for (int i = 0;
             i < catalog.FurniturePurchases.Count;
             i++)
        {
            FurniturePurchaseData purchase =
                catalog.FurniturePurchases[i];

            if (purchase == null)
            {
                continue;
            }

            AddPurchaseRow(
                parent,
                purchase.DisplayName,
                "$" +
                purchase.PurchasePrice.ToString("0.00") +
                " · level " +
                purchase.RequiredStoreLevel,
                () =>
                {
                    bool success =
                        PurchaseService.Instance
                            .TryPurchaseFurniture(purchase);

                    NotifyPurchase(
                        success,
                        purchase.DisplayName);
                });
        }
    }

    private void BuildRegister(Transform parent)
    {
        AddSectionTitle(parent,"REGISTER");
        var counters =
            GameBootstrap.Instance.Checkouts.Counters;

        if (counters.Count == 0)
        {
            AddEmptyState(
                parent,
                "Place a checkout counter to begin serving customers.");
            return;
        }

        for (int i = 0; i < counters.Count; i++)
        {
            CheckoutCounter counter = counters[i];

            if (counter == null)
            {
                continue;
            }

            string session =
                counter.ActiveSession == null
                    ? "No active transaction"
                    : counter.ActiveSession.ScannedItemCount +
                      "/" +
                      counter.ActiveSession.TotalItemCount +
                      " · $" +
                      counter.ActiveSession.Total
                          .ToString("0.00");

            AddMetric(
                parent,
                "COUNTER " + (i + 1) +
                " · QUEUE " +
                counter.QueueCount,
                session);
        }
    }

    private void BuildBank(Transform parent)
    {
        AddSectionTitle(parent,"CLERK BANK");

        Money balance =
            WalletController.Instance != null
                ? WalletController.Instance.Balance
                : Money.Zero;

        AddMetric(parent,"AVAILABLE BALANCE",FormatMoney(balance));

        AddMetric(
            parent,
            "OUTSTANDING LOAN",
            FormatMoney(
                GameBootstrap.Instance.Finance
                    .OutstandingLoan));

        AddMetric(
            parent,
            "AVAILABLE CREDIT",
            FormatMoney(
                GameBootstrap.Instance.Finance
                    .AvailableCredit));

        AddActionButton(
            parent,
            "BORROW $500",
            () =>
            {
                bool success =
                    GameBootstrap.Instance.Finance
                        .Borrow(
                            Money.FromFloat(500f));

                GameBootstrap.Instance.Notifications
                    .Show(
                        success
                            ? "$500 added to your account."
                            : "Credit limit reached.",
                        success
                            ? NotificationKind.Success
                            : NotificationKind.Error);

                ShowApplication(
                    StoreApplication.Bank);
            });

        if (!GameBootstrap.Instance.Finance
                .OutstandingLoan.IsZero)
        {
            AddActionButton(
                parent,
                "REPAY $100",
                () =>
                {
                    bool success =
                        GameBootstrap.Instance.Finance
                            .Repay(
                                Money.FromFloat(100f));

                    GameBootstrap.Instance.Notifications
                        .Show(
                            success
                                ? "Loan payment recorded."
                                : "Loan payment failed.",
                            success
                                ? NotificationKind.Success
                                : NotificationKind.Error);

                    ShowApplication(
                        StoreApplication.Bank);
                });

            AddActionButton(
                parent,
                "REPAY FULL BALANCE",
                () =>
                {
                    Money outstanding =
                        GameBootstrap.Instance.Finance
                            .OutstandingLoan;

                    bool success =
                        GameBootstrap.Instance.Finance
                            .Repay(outstanding);

                    GameBootstrap.Instance.Notifications
                        .Show(
                            success
                                ? "Loan paid in full."
                                : "Not enough available balance.",
                            success
                                ? NotificationKind.Success
                                : NotificationKind.Error);

                    ShowApplication(
                        StoreApplication.Bank);
                });
        }
    }

    private void BuildTransactionHistory(Transform parent)
    {
        AddSectionTitle(parent,"FINANCIAL HISTORY");

        IReadOnlyList<LedgerEntry> entries =
            GameBootstrap.Instance.Economy.Entries;

        long incomeCents = 0;
        long expenseCents = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].AmountCents >= 0)
            {
                incomeCents += entries[i].AmountCents;
            }
            else
            {
                expenseCents += -entries[i].AmountCents;
            }
        }

        AddMetric(
            parent,
            "TOTAL MONEY IN",
            FormatMoney(new Money(incomeCents)));

        AddMetric(
            parent,
            "TOTAL MONEY OUT",
            FormatMoney(new Money(expenseCents)));

        int start = Mathf.Max(0,entries.Count - 50);

        for (int i = entries.Count - 1;
             i >= start;
             i--)
        {
            LedgerEntry entry = entries[i];

            AddMetric(
                parent,
                "DAY " + entry.Day +
                " · " + entry.Description,
                (entry.AmountCents >= 0 ? "+" : "") +
                FormatMoney(entry.Amount));
        }

        if (entries.Count == 0)
        {
            AddEmptyState(parent,"No transactions yet.");
        }
    }

    private void BuildStaff(Transform parent)
    {
        AddSectionTitle(parent,"EMPLOYEE MANAGEMENT");

        var employees =
            GameBootstrap.Instance.Employees.Employees;

        AddMetric(
            parent,
            "ACTIVE EMPLOYEES",
            employees.Count.ToString());

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeContext employee = employees[i];

            if (employee == null)
            {
                continue;
            }

            AddMetric(
                parent,
                employee.Definition != null
                    ? employee.Definition.DisplayName
                    : "Employee",
                employee.Definition != null
                    ? employee.Definition.Role.ToString()
                    : "Unassigned");

            if (employee.Definition != null)
            {
                EmployeeDefinition stats = employee.Definition;
                AddMetric(parent,"SPECIALTY",stats.Role.ToString());
                AddMetric(parent,"DAILY SALARY","$" + stats.DailyWage.ToString("0.00"));
                AddMetric(parent,"MOVEMENT",stats.MovementSpeed.ToString("0.0"));
                AddMetric(parent,"WORK SPEED",stats.WorkInterval.ToString("0.00") + " sec/task");
            }

            EmployeeContext capturedEmployee =
                employee;

            AddActionButton(
                parent,
                "FIRE " +
                (employee.Definition != null
                    ? employee.Definition.DisplayName
                    : "EMPLOYEE"),
                () =>
                {
                    GameBootstrap.Instance.Employees
                        .Fire(capturedEmployee);
                    ShowApplication(
                        StoreApplication.Staff);
                });
        }

        EmployeeDefinition[] catalog =
            PurchaseService.Instance != null
                ? PurchaseService.Instance
                    .EmployeeCatalog
                : null;

        if (catalog == null || catalog.Length == 0)
        {
            AddEmptyState(
                parent,
                "No employee definitions are configured.");
            return;
        }

        AddSectionTitle(parent,"HIRE");

        for (int i = 0; i < catalog.Length; i++)
        {
            EmployeeDefinition definition =
                catalog[i];

            if (definition == null)
            {
                continue;
            }

            AddPurchaseRow(
                parent,
                definition.DisplayName,
                "$" +
                definition.HiringCost.ToString("0.00") +
                " hire · $" +
                definition.DailyWage.ToString("0.00") +
                "/day",
                () =>
                {
                    bool success =
                        PurchaseService.Instance
                            .TryHireEmployee(
                                definition,
                                out _);

                    GameBootstrap.Instance
                        .Notifications.Show(
                            success
                                ? definition.DisplayName +
                                  " hired."
                                : "Could not hire " +
                                  definition.DisplayName +
                                  ".",
                            success
                                ? NotificationKind.Success
                                : NotificationKind.Error);

                    ShowApplication(
                        StoreApplication.Staff);
                });
        }
    }

    private void BuildSecurity(Transform parent)
    {
        AddSectionTitle(parent,"SECURITY CAMERAS");
        Camera[] cameras =
            FindObjectsByType<Camera>(
                FindObjectsInactive.Exclude);
        List<Camera> feeds = new List<Camera>();
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != Camera.main)
            {
                feeds.Add(cameras[i]);
            }
        }
        AddMetric(parent,"CAMERAS ONLINE",feeds.Count.ToString());
        if (feeds.Count == 0)
        {
            AddEmptyState(parent,
                "No security cameras are installed. Add a Camera to the scene; the player Main Camera is excluded.");
            return;
        }
        for (int i = 0; i < feeds.Count; i++)
        {
            Camera feed = feeds[i];
            AddActionButton(parent,"VIEW · " + feed.name,() =>
            {
                OpenSecurityPreview(feed);
                ShowApplication(StoreApplication.Security);
            });
        }
        if (selectedSecurityCamera != null && securityPreviewTexture != null)
        {
            AddSectionTitle(parent,"LIVE · " + selectedSecurityCamera.name);
            RectTransform preview = UIFactory.Panel(parent,"Live Camera Feed",Color.black);
            UIFactory.Size(preview,0f,360f);
            UnityEngine.Object.Destroy(preview.GetComponent<Image>());
            RawImage image = preview.gameObject.AddComponent<RawImage>();
            image.texture = securityPreviewTexture;
            image.raycastTarget = false;
            AddActionButton(parent,"CLOSE CAMERA FEED",() =>
            {
                ReleaseSecurityPreview();
                ShowApplication(StoreApplication.Security);
            });
        }
    }

    private void OpenSecurityPreview(Camera camera)
    {
        ReleaseSecurityPreview();
        selectedSecurityCamera = camera;
        securityPreviewTexture = new RenderTexture(640,360,16,RenderTextureFormat.ARGB32);
        securityPreviewTexture.name = "Clerk Security Preview";
        selectedSecurityCamera.targetTexture = securityPreviewTexture;
    }

    private void ReleaseSecurityPreview()
    {
        if (selectedSecurityCamera != null &&
            selectedSecurityCamera.targetTexture == securityPreviewTexture)
        {
            selectedSecurityCamera.targetTexture = null;
        }
        selectedSecurityCamera = null;
        if (securityPreviewTexture != null)
        {
            securityPreviewTexture.Release();
            Destroy(securityPreviewTexture);
            securityPreviewTexture = null;
        }
    }

    private void BuildMessages(Transform parent)
    {
        string[] contacts = { "District Manager", "SupplyCo Bot", "Staff Group", "Landlord" };
        string[] previews =
        {
            "Remember to review today's assignments.",
            "Deliveries are routed to your loading point.",
            "Keep checkout queues moving during busy periods.",
            "Store facilities report: no action required."
        };
        AddSectionTitle(parent,"MESSAGES");
        for (int i = 0; i < contacts.Length; i++)
        {
            int captured = i;
            AddActionButton(parent,contacts[i] + " - " + previews[i],() =>
            {
                selectedConversationIndex = captured;
                ShowApplication(StoreApplication.Messages);
            });
        }

        if (selectedConversationIndex >= 0 && selectedConversationIndex < contacts.Length)
        {
            int selected = selectedConversationIndex;
            AddSectionTitle(parent,contacts[selected]);
            AddMessage(parent,contacts[selected],previews[selected]);
            string replyKey = "Clerk.Messages.Replied." + selected;
            if (PlayerPrefs.GetInt(replyKey,0) == 0)
            {
                AddActionButton(parent,"REPLY: GOT IT",() =>
                {
                    PlayerPrefs.SetInt(replyKey,1);
                    PlayerPrefs.Save();
                    ShowApplication(StoreApplication.Messages);
                });
            }
            else
            {
                AddMessage(parent,"YOU","Got it. I'll take care of it.");
            }
            AddActionButton(parent,"BACK TO CONVERSATIONS",() =>
            {
                selectedConversationIndex = -1;
                ShowApplication(StoreApplication.Messages);
            });
        }
    }

    private void BuildCareerBoard(Transform parent)
    {
        AddSectionTitle(parent,"LINKEDIN JOBS");
        AddMetric(parent,"PROFILE","STORE CLERK · LEVEL " +
            GameBootstrap.Instance.Progression.StoreLevel);
        AddSectionTitle(parent,"AVAILABLE CONTRACTS");
        var objectives =
            GameBootstrap.Instance.Objectives.Active;

        if (objectives.Count == 0)
        {
            AddEmptyState(
                parent,
                "No job opportunities are available right now.");
            return;
        }

        for (int i = 0; i < objectives.Count; i++)
        {
            ObjectiveProgress objective =
                objectives[i];

            string value =
                objective.Progress +
                " / " +
                objective.Definition.TargetAmount;

            AddMetric(
                parent,
                objective.Definition.Title,
                objective.Completed
                    ? "COMPLETE · " + value
                    : value);

            if (objective.Completed &&
                !objective.RewardClaimed)
            {
                string id =
                    objective.Definition.ObjectiveId;

                AddActionButton(
                    parent,
                    "CLAIM REWARD",
                    () =>
                    {
                        GameBootstrap.Instance.Objectives
                            .ClaimReward(id);
                        ShowApplication(
                            StoreApplication.Tasks);
                    });
            }
        }
    }

    private void BuildLogin(Transform parent)
    {
        int day = GameBootstrap.Instance.Days.CurrentDay;
        bool paid = HasReceivedShiftPay(day);
        AddSectionTitle(parent,"EMPLOYEE LOGIN");
        AddMetric(parent,"EMPLOYEE","STORE CLERK");
        AddMetric(parent,"TODAY","DAY " + day);
        AddMetric(parent,"ATTENDANCE",paid ? "CLOCKED IN · PAID" : "NOT CLOCKED IN");
        AddMetric(parent,"DAILY WAGE",FormatMoney(
            Money.FromFloat(authoredUI != null ? authoredUI.PlayerDailyWage : 80f)));

        if (!paid)
        {
            AddActionButton(parent,"CLOCK IN FOR TODAY",() =>
            {
                Money wage = Money.FromFloat(
                    authoredUI != null ? authoredUI.PlayerDailyWage : 80f);
                GameBootstrap.Instance.Economy.GrantFunds(
                    wage,LedgerEntryType.Adjustment,"Clerk daily wage","player-shift-pay");
                GameBootstrap.Instance.Notifications.Show(
                    "Attendance recorded. Today's wage was paid.",NotificationKind.Success);
                ShowApplication(StoreApplication.Login);
            });
        }
        else
        {
            AddEmptyState(parent,"Your manager has received today's attendance record.");
        }
    }

    private bool HasReceivedShiftPay(int day)
    {
        IReadOnlyList<LedgerEntry> entries = GameBootstrap.Instance.Economy.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Day == day && entries[i].RelatedId == "player-shift-pay")
            {
                return true;
            }
        }
        return false;
    }

    private void BuildMail(Transform parent)
    {
        int day = GameBootstrap.Instance.Days.CurrentDay;
        int level = GameBootstrap.Instance.Progression.StoreLevel;
        List<string> senders = new List<string>
        {
            "STORE MANAGER",
            "HUMAN RESOURCES"
        };
        List<string> subjects = new List<string>
        {
            "Attendance · Day " + day,
            "Your current performance review"
        };
        List<string> bodies = new List<string>
        {
            HasReceivedShiftPay(day)
                ? "Attendance confirmed. Your wage has been processed and recorded in History."
                : "You have not clocked in today. Open the Login app before beginning your shift.",
            "Current rank: Store Clerk · Performance level " + level +
            ". Keep shelves stocked, serve customers quickly, and maintain fair prices to earn promotion opportunities."
        };
        var objectives = GameBootstrap.Instance.Objectives.Active;
        for (int i = 0; i < objectives.Count; i++)
        {
            ObjectiveProgress objective = objectives[i];
            senders.Add("DISTRICT MANAGER");
            subjects.Add("Assignment: " + objective.Definition.Title);
            bodies.Add("Progress: " + objective.Progress + "/" +
                objective.Definition.TargetAmount +
                (objective.Completed
                    ? ". Assignment complete—open LinkedIn Jobs to claim its reward."
                    : ". Complete this during your shift and report back."));
        }
        if (level > 1)
        {
            senders.Add("MANAGEMENT");
            subjects.Add("Congratulations on your promotion");
            bodies.Add("You reached store level " + level +
                ". New responsibilities and career opportunities may now be available.");
        }

        AddSectionTitle(parent,"MAIL · INBOX");
        for (int i = 0; i < subjects.Count; i++)
        {
            int captured = i;
            bool read = PlayerPrefs.GetInt("Clerk.Mail.Read." + day + "." + i,0) == 1;
            AddActionButton(parent,(read ? "" : "NEW · ") + senders[i] + " — " + subjects[i],() =>
            {
                selectedMailIndex = captured;
                PlayerPrefs.SetInt("Clerk.Mail.Read." + day + "." + captured,1);
                PlayerPrefs.Save();
                ShowApplication(StoreApplication.Mail);
            });
        }

        if (selectedMailIndex >= 0 && selectedMailIndex < bodies.Count)
        {
            AddSectionTitle(parent,subjects[selectedMailIndex]);
            AddMessage(parent,"FROM · " + senders[selectedMailIndex],bodies[selectedMailIndex]);
            AddActionButton(parent,"CLOSE MESSAGE",() =>
            {
                selectedMailIndex = -1;
                ShowApplication(StoreApplication.Mail);
            });
        }
    }

    private void BuildAppMarket(Transform parent)
    {
        AddSectionTitle(parent,"CLERK APP MARKET");
        AddMessage(parent,"EDITOR'S PICK",
            "Install useful software for your store desktop. Installed apps appear as shortcuts immediately.");
        AddMarketApp(parent,StoreApplication.Messages,"Staff Chat","Business","14 MB","4.2");
        AddMarketApp(parent,StoreApplication.Security,"SecuCam","Business","18 MB","4.6");
        AddMarketApp(parent,StoreApplication.Todo,"Shift Buddy","Business","8 MB","4.6");
        AddMarketApp(parent,StoreApplication.Weather,"Store Weather","Tools","6 MB","4.5");
        AddMarketApp(parent,StoreApplication.Notepad,"Clerk Notes","Tools","2 MB","4.1");
        AddMarketApp(parent,StoreApplication.Calculator,"Cash Counter+","Tools","5 MB","4.4");
    }

    private void AddMarketApp(Transform parent,StoreApplication application,
        string productName,string category,string size,string rating)
    {
        bool installed = IsApplicationInstalled(application);
        AddMetric(parent,productName,
            "RATING " + rating + " | " + size + " | " + category +
            (installed ? " | INSTALLED" : string.Empty));
        AddActionButton(parent,installed ? "OPEN " + productName.ToUpperInvariant()
            : "INSTALL " + productName.ToUpperInvariant(),() =>
        {
            if (installed)
            {
                ShowApplication(application);
            }
            else
            {
                InstallApplication(application);
            }
        });
    }

    private void BuildTodo(Transform parent)
    {
        AddSectionTitle(parent,"SHIFT BUDDY · TO DO");
        var objectives = GameBootstrap.Instance.Objectives.Active;
        if (objectives.Count == 0)
        {
            AddEmptyState(parent,"Your shift list is clear.");
            return;
        }
        for (int i = 0; i < objectives.Count; i++)
        {
            ObjectiveProgress item = objectives[i];
            AddMetric(parent,(item.Completed ? "✓ " : "□ ") + item.Definition.Title,
                item.Progress + " / " + item.Definition.TargetAmount);
            if (item.Completed && !item.RewardClaimed)
            {
                string objectiveId = item.Definition.ObjectiveId;
                AddActionButton(parent,"CLAIM REWARD · " + item.Definition.Title,() =>
                {
                    GameBootstrap.Instance.Objectives.ClaimReward(objectiveId);
                    ShowApplication(StoreApplication.Todo);
                });
            }
        }
    }

    private void BuildWeather(Transform parent)
    {
        int day = GameBootstrap.Instance.Days.CurrentDay;
        string[] conditions = { "Clear", "Light rain", "Cloudy", "Warm and sunny", "Windy" };
        int[] temperatures = { 72, 66, 69, 77, 64 };
        int index = Mathf.Abs(day - 1) % conditions.Length;
        AddSectionTitle(parent,"WEATHER");
        AddMetric(parent,temperatures[index] + "°F",conditions[index] + " · Quick Stop district");
        AddMessage(parent,"SHIFT ADVISORY",index == 1
            ? "Expect wet foot traffic. Deliveries may arrive slightly later than usual."
            : index == 4
                ? "Secure outdoor deliveries and expect cooler evening traffic."
                : "Conditions are favorable for normal deliveries and customer traffic.");
        AddMessage(parent,"FORECAST","Tomorrow · " +
            conditions[(index + 1) % conditions.Length] + " · " +
            temperatures[(index + 1) % temperatures.Length] + "°F");
    }

    private void BuildNotepad(Transform parent)
    {
        AddSectionTitle(parent,"CLERK NOTES");
        TMP_InputField notes = AddDesktopInput(parent,"Notes",true);
        notes.text = PlayerPrefs.GetString("Clerk.Desktop.Notepad",string.Empty);
        AddActionButton(parent,"SAVE NOTE",() =>
        {
            PlayerPrefs.SetString("Clerk.Desktop.Notepad",notes.text);
            PlayerPrefs.Save();
            GameBootstrap.Instance.Notifications.Show("Note saved.",NotificationKind.Success);
        });
    }

    private void BuildCalculator(Transform parent)
    {
        AddSectionTitle(parent,"CASH COUNTER+");
        TMP_InputField first = AddDesktopInput(parent,"First number",false);
        TMP_InputField second = AddDesktopInput(parent,"Second number",false);
        TextMeshProUGUI result = UIFactory.Text(parent,"Result","RESULT: 0",24f,
            TextAlignmentOptions.Left);
        UIFactory.Size(result,0f,55f);
        void Calculate(char operation)
        {
            double.TryParse(first.text,NumberStyles.Float,CultureInfo.InvariantCulture,out double a);
            double.TryParse(second.text,NumberStyles.Float,CultureInfo.InvariantCulture,out double b);
            double value = operation switch
            {
                '+' => a + b,
                '-' => a - b,
                '*' => a * b,
                '/' => Math.Abs(b) < 0.000001d ? double.NaN : a / b,
                _ => 0d
            };
            result.text = "RESULT: " + (double.IsNaN(value) ? "CANNOT DIVIDE BY ZERO" : value.ToString("0.##"));
        }
        AddActionButton(parent,"ADD",() => Calculate('+'));
        AddActionButton(parent,"SUBTRACT",() => Calculate('-'));
        AddActionButton(parent,"MULTIPLY",() => Calculate('*'));
        AddActionButton(parent,"DIVIDE",() => Calculate('/'));
    }

    private static TMP_InputField AddDesktopInput(Transform parent,string placeholder,bool multiline)
    {
        RectTransform root = UIFactory.Panel(parent,placeholder,UIFactory.SurfaceRaised);
        UIFactory.Size(root,0f,multiline ? 260f : 58f);
        root.GetComponent<Image>().raycastTarget = true;
        TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();
        TextMeshProUGUI text = UIFactory.Text(root,"Text",string.Empty,19f,
            TextAlignmentOptions.TopLeft);
        text.rectTransform.offsetMin = new Vector2(14f,10f);
        text.rectTransform.offsetMax = new Vector2(-14f,-10f);
        input.textComponent = text;
        input.textViewport = root;
        TextMeshProUGUI hint = UIFactory.Text(root,"Placeholder",placeholder,19f,
            TextAlignmentOptions.TopLeft);
        hint.color = UIFactory.Muted;
        hint.fontStyle = FontStyles.Italic;
        hint.rectTransform.offsetMin = new Vector2(14f,10f);
        hint.rectTransform.offsetMax = new Vector2(-14f,-10f);
        input.placeholder = hint;
        input.contentType = multiline ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.DecimalNumber;
        input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline
            : TMP_InputField.LineType.SingleLine;
        return input;
    }

    private void BuildSettings(Transform parent)
    {
        AddSectionTitle(parent,"SETTINGS");
        GameSettingsService settings =
            GameBootstrap.Instance.Settings;

        AddMetric(
            parent,
            "MASTER VOLUME",
            Mathf.RoundToInt(
                settings.Settings.MasterVolume * 100f) +
            "%");

        AddActionButton(
            parent,
            "VOLUME -",
            () =>
            {
                settings.SetMasterVolume(
                    settings.Settings.MasterVolume -
                    0.1f);
                ShowApplication(activeApplication);
            });

        AddActionButton(
            parent,
            "VOLUME +",
            () =>
            {
                settings.SetMasterVolume(
                    settings.Settings.MasterVolume +
                    0.1f);
                ShowApplication(activeApplication);
            });

        AddMetric(
            parent,
            "LOOK SENSITIVITY",
            settings.Settings.LookSensitivity
                .ToString("0"));

        AddActionButton(
            parent,
            "SENSITIVITY -",
            () =>
            {
                settings.SetLookSensitivity(
                    settings.Settings.LookSensitivity -
                    10f);
                ShowApplication(activeApplication);
            });

        AddActionButton(
            parent,
            "SENSITIVITY +",
            () =>
            {
                settings.SetLookSensitivity(
                    settings.Settings.LookSensitivity +
                    10f);
                ShowApplication(activeApplication);
            });

        AddActionButton(
            parent,
            settings.Settings.Fullscreen
                ? "WINDOWED MODE"
                : "FULLSCREEN MODE",
            () =>
            {
                settings.SetFullscreen(
                    !settings.Settings.Fullscreen);
                ShowApplication(activeApplication);
            });

        AddActionButton(
            parent,
            "REBIND INTERACT · " +
            GameBootstrap.Instance.Input
                .GetBindingDisplay(
                    GameplayAction.Use),
            () => BeginRebind(
                GameplayAction.Use,
                0));

        AddActionButton(
            parent,
            "REBIND PICK UP · " +
            GameBootstrap.Instance.Input
                .GetBindingDisplay(
                    GameplayAction.Primary),
            () => BeginRebind(
                GameplayAction.Primary,
                0));

        AddActionButton(
            parent,
            "RESET INPUT BINDINGS",
            () =>
            {
                GameBootstrap.Instance.Input
                    .ResetBindingOverrides();
                GameBootstrap.Instance.Notifications
                    .Show(
                        "Input bindings reset.",
                        NotificationKind.Success);
            });

        AddActionButton(
            parent,
            "SAVE / LOAD",
            () => ShowApplication(
                StoreApplication.SaveLoad));
    }

    private void BuildSaveLoad(Transform parent)
    {
        AddSectionTitle(parent,"SAVE SLOTS");

        for (int slot = 0; slot < 3; slot++)
        {
            int capturedSlot = slot;
            bool exists =
                GameBootstrap.Instance.Saves
                    .SaveExists(slot);

            AddMetric(
                parent,
                "SLOT " + (slot + 1),
                exists ? "SAVED GAME" : "EMPTY");

            AddActionButton(
                parent,
                "SAVE TO SLOT " + (slot + 1),
                () => SaveSlot(capturedSlot));

            if (exists)
            {
                AddActionButton(
                    parent,
                    "LOAD SLOT " + (slot + 1),
                    () => LoadSlot(capturedSlot));
            }
        }
    }

    private static void AddSectionTitle(
        Transform parent,
        string title)
    {
        TextMeshProUGUI text =
            UIFactory.Text(
                parent,
                "Section Title",
                title,
                32f,
                TextAlignmentOptions.Left);

        text.color = UIFactory.Accent;
        UIFactory.Size(text,0f,60f);
    }

    private static void AddMetric(
        Transform parent,
        string label,
        string value)
    {
        RectTransform row =
            UIFactory.Panel(
                parent,
                "Metric",
                UIFactory.Surface);

        UIFactory.Size(row,0f,72f);
        UIFactory.Horizontal(row,14f,16f);

        TextMeshProUGUI labelText =
            UIFactory.Text(
                row,
                "Label",
                label,
                17f,
                TextAlignmentOptions.Left);

        labelText.color = UIFactory.Muted;

        TextMeshProUGUI valueText =
            UIFactory.Text(
                row,
                "Value",
                value,
                20f,
                TextAlignmentOptions.Right);

        valueText.color = Color.white;
    }

    private static void AddMessage(
        Transform parent,
        string sender,
        string message)
    {
        AddMetric(parent,sender,message);
    }

    private static void AddEmptyState(
        Transform parent,
        string message)
    {
        TextMeshProUGUI text =
            UIFactory.Text(
                parent,
                "Empty State",
                message,
                19f,
                TextAlignmentOptions.Center);

        text.color = UIFactory.Muted;
        UIFactory.Size(text,0f,90f);
    }

    private static void AddActionButton(
        Transform parent,
        string label,
        UnityEngine.Events.UnityAction action)
    {
        Button button =
            UIFactory.Button(
                parent,
                label,
                label,
                action,
                UIFactory.Accent);

        UIFactory.Size(button,0f,54f);
    }

    private static void AddPurchaseRow(
        Transform parent,
        string title,
        string details,
        UnityEngine.Events.UnityAction purchase)
    {
        RectTransform row =
            UIFactory.Panel(
                parent,
                title,
                UIFactory.Surface);

        UIFactory.Size(row,0f,86f);
        UIFactory.Horizontal(row,12f,14f);

        TextMeshProUGUI description =
            UIFactory.Text(
                row,
                "Description",
                title + "\n" + details,
                18f,
                TextAlignmentOptions.Left);

        description.color = Color.white;

        Button button =
            UIFactory.Button(
                row,
                "Buy",
                "BUY",
                purchase,
                UIFactory.Accent);

        UIFactory.Size(button,110f,56f);
    }

    private void NotifyPurchase(
        bool success,
        string item)
    {
        GameBootstrap.Instance.Notifications.Show(
            success
                ? item + " purchased and sent to delivery."
                : "Could not purchase " + item + ".",
            success
                ? NotificationKind.Success
                : NotificationKind.Error);

        ShowApplication(activeApplication);
    }

    private void SaveSlot(int slot)
    {
        bool saved =
            GameBootstrap.Instance.Saves.Save(slot);

        GameBootstrap.Instance.Notifications.Show(
            saved
                ? "Game saved to slot " + (slot + 1) + "."
                : "The game could not be saved.",
            saved
                ? NotificationKind.Success
                : NotificationKind.Error);
    }

    private void BeginRebind(
        GameplayAction action,
        int bindingIndex)
    {
        isRebinding = true;

        GameBootstrap.Instance.Notifications.Show(
            "Press a new key, or Escape to cancel.",
            NotificationKind.Information,
            5f);

        GameBootstrap.Instance.Input
            .BeginInteractiveRebind(
                action,
                bindingIndex,
                success =>
                {
                    isRebinding = false;

                    GameBootstrap.Instance
                        .Notifications.Show(
                            success
                                ? action +
                                  " binding updated."
                                : "Rebinding cancelled.",
                            success
                                ? NotificationKind.Success
                                : NotificationKind.Warning);

                    if (deviceContent != null)
                    {
                        ShowApplication(
                            StoreApplication.Settings);
                    }
                });
    }

    private void LoadSlot(int slot)
    {
        bool loaded =
            GameBootstrap.Instance.Saves.Load(slot);

        GameBootstrap.Instance.Notifications.Show(
            loaded
                ? "Loaded slot " + (slot + 1) + "."
                : "That save slot could not be loaded.",
            loaded
                ? NotificationKind.Success
                : NotificationKind.Error);

        if (loaded && deviceContent != null)
        {
            ShowApplication(activeApplication);
        }
    }

    private void UpdateHud()
    {
        if (moneyText == null ||
            GameBootstrap.Instance == null)
        {
            return;
        }

        Money balance =
            WalletController.Instance != null
                ? WalletController.Instance.Balance
                : Money.Zero;

        moneyText.text = FormatMoney(balance);

        StoreDayController day =
            GameBootstrap.Instance.Days;

        clockText.text =
            "DAY " + day.CurrentDay +
            "  " + day.FormattedTime;

        if (authoredUI != null && authoredUI.TaskbarClockText != null)
        {
            authoredUI.TaskbarClockText.text =
                day.FormattedTime + "\nDAY " + day.CurrentDay;
        }

        statusText.text =
            day.IsDayRunning
                ? "OPEN · " +
                  GameBootstrap.Instance.Customers
                      .Count +
                  " CUSTOMERS"
                : "STORE CLOSED";
    }

    private void HandleModeChanged(GameplayMode mode)
    {
        if (pauseRoot != null)
        {
            pauseRoot.gameObject.SetActive(
                mode == GameplayMode.Paused &&
                !showingMainMenu);
        }

        if (hudRoot != null)
        {
            hudRoot.gameObject.SetActive(
                !showingMainMenu &&
                (mode == GameplayMode.Gameplay ||
                 mode == GameplayMode
                     .FurniturePlacement));
        }
    }

    private void HandleNotification(
        StoreNotification notification)
    {
        if (notificationRoutine != null)
        {
            StopCoroutine(notificationRoutine);
        }

        notificationRoutine =
            StartCoroutine(
                ShowNotification(notification));
    }

    private void HandleDayEnded(int day)
    {
        Money revenue =
            GameBootstrap.Instance.Economy
                .GetRevenueForDay(day);

        Money profit =
            GameBootstrap.Instance.Economy
                .GetProfitForDay(day);

        GameBootstrap.Instance.Notifications.Show(
            "Day " + day +
            " complete · Revenue " +
            FormatMoney(revenue) +
            " · Profit " +
            FormatMoney(profit),
            profit.IsNegative
                ? NotificationKind.Warning
                : NotificationKind.Success,
            6f);
    }

    private IEnumerator ShowNotification(
        StoreNotification notification)
    {
        notificationRoot.gameObject.SetActive(true);
        UIFactory.Clear(notificationRoot);

        Color color = notification.Kind switch
        {
            NotificationKind.Success =>
                UIFactory.Accent,
            NotificationKind.Warning =>
                new Color32(255,184,77,255),
            NotificationKind.Error =>
                UIFactory.Danger,
            _ => new Color32(71,156,255,255)
        };

        RectTransform card =
            UIFactory.Panel(
                notificationRoot,
                "Toast",
                UIFactory.SurfaceRaised);

        Outline outline =
            card.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance =
            new Vector2(3f,-3f);

        TextMeshProUGUI text =
            UIFactory.Text(
                card,
                "Message",
                notification.Message,
                20f,
                TextAlignmentOptions.Center);

        text.color = Color.white;

        yield return new WaitForSecondsRealtime(
            notification.Duration);

        notificationRoot.gameObject.SetActive(false);
        notificationRoutine = null;
    }

    private static string FormatMoney(Money money)
    {
        return "$" + money.AsFloat.ToString("0.00");
    }

    private static string GetApplicationTitle(
        StoreApplication application)
    {
        return application switch
        {
            StoreApplication.Overview => "OVERVIEW",
            StoreApplication.Supply => "STORE",
            StoreApplication.Furniture => "FURNITURE",
            StoreApplication.Register => "REGISTER",
            StoreApplication.Bank => "BANK",
            StoreApplication.History => "HISTORY",
            StoreApplication.Staff => "HIRING & HR",
            StoreApplication.Security => "SECURITY",
            StoreApplication.Messages => "MESSAGES",
            StoreApplication.Tasks => "LINKEDIN JOBS",
            StoreApplication.Settings => "SETTINGS",
            StoreApplication.Login => "EMPLOYEE LOGIN",
            StoreApplication.Mail => "MAIL",
            StoreApplication.AppMarket => "APP MARKET",
            StoreApplication.Todo => "SHIFT BUDDY",
            StoreApplication.Weather => "WEATHER",
            StoreApplication.Notepad => "NOTEPAD",
            StoreApplication.Calculator => "CALCULATOR",
            StoreApplication.SaveLoad => "SAVE / LOAD",
            _ => application.ToString().ToUpperInvariant()
        };
    }

    private static string GetMobileIcon(
        StoreApplication application)
    {
        return application switch
        {
            StoreApplication.Overview => "H",
            StoreApplication.Supply => "S",
            StoreApplication.Register => "R",
            StoreApplication.Bank => "$",
            StoreApplication.History => "L",
            StoreApplication.Tasks => "T",
            StoreApplication.Settings => "O",
            _ => "."
        };
    }

    private void OnDestroy()
    {
        if (GameBootstrap.Instance == null)
        {
            return;
        }

        GameBootstrap.Instance.GameplayModes.ModeChanged -=
            HandleModeChanged;

        GameBootstrap.Instance.Notifications
            .NotificationRaised -=
            HandleNotification;

        GameBootstrap.Instance.Days.DayEnded -=
            HandleDayEnded;
    }
}
