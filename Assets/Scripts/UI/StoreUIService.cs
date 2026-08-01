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

        if (GameBootstrap.Instance != null &&
            GameBootstrap.Instance.Input.WasPressedThisFrame(
                GameplayAction.Mobile))
        {
            ToggleMobile();
            return;
        }

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

        ShowApplication(StoreApplication.Overview);
        deviceRoot.gameObject.SetActive(true);

        GameBootstrap.Instance.GameplayModes
            .TrySetMode(GameplayMode.DeviceUI);
    }

    public void CloseDevice()
    {
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

            if (mobileModelInstance != null)
            {
                mobileModelInstance.SetActive(false);
            }

            return;
        }

        if (mobileModelInstance == null &&
            mobileModelPrefab != null &&
            Camera.main != null)
        {
            ResolveMobileHoldPoint();
            mobileModelInstance =
                Instantiate(
                    mobileModelPrefab,
                    mobileHoldPoint != null
                        ? mobileHoldPoint
                        : Camera.main.transform);

            mobileModelInstance.name =
                "Player Mobile Device";

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

        if (mobileModelInstance != null)
        {
            mobileModelInstance.SetActive(true);
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
            ResolveMobileHoldPoint();
            GameObject screen = new GameObject(
                "Mobile Screen UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));

            screen.transform.SetParent(
                mobileHoldPoint != null
                    ? mobileHoldPoint
                    : Camera.main.transform,
                false);
            RectTransform screenRect =
                screen.GetComponent<RectTransform>();
            screenRect.localPosition =
                mobilePresentation != null
                    ? mobilePresentation.MobileScreenLocalPosition
                    : new Vector3(0f,0f,-0.045f);
            screenRect.localRotation = Quaternion.Euler(
                mobilePresentation != null
                    ? mobilePresentation.MobileScreenLocalEulerAngles
                    : Vector3.zero);
            screenRect.localScale = Vector3.one *
                (mobilePresentation != null
                    ? mobilePresentation.MobileScreenScale
                    : 0.0003f);
            screenRect.sizeDelta = mobilePresentation != null
                ? mobilePresentation.MobileScreenSize
                : new Vector2(520f,900f);

            mobileScreenCanvas = screen.GetComponent<Canvas>();
            mobileScreenCanvas.renderMode = RenderMode.WorldSpace;
            mobileScreenCanvas.worldCamera = Camera.main;
            mobileScreenCanvas.sortingOrder = 110;
        }

        if (deviceScreenParent == null)
        {
            deviceScreenParent = deviceRoot.parent;
        }

        deviceRoot.SetParent(mobileScreenCanvas.transform,false);
        UIFactory.Stretch(deviceRoot);
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

    private void RestoreDeviceToScreenCanvas()
    {
        if (deviceRoot != null && deviceScreenParent != null)
        {
            deviceRoot.SetParent(deviceScreenParent,false);
            UIFactory.Stretch(deviceRoot);
        }

        if (mobileScreenCanvas != null)
        {
            mobileScreenCanvas.gameObject.SetActive(false);
        }
    }

    public void ShowApplication(
        StoreApplication application)
    {
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
                BuildStockCatalog(deviceContent);
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
                BuildTasks(deviceContent);
                break;
            case StoreApplication.Settings:
                BuildSettings(deviceContent);
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
            "PHONE",
            () =>
            {
                GameBootstrap.Instance
                    .GameplayModes.Resume();
                OpenDevice(StoreDeviceKind.Mobile);
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

    private void ToggleMobile()
    {
        if (showingMainMenu)
        {
            return;
        }

        if (deviceRoot != null &&
            deviceRoot.gameObject.activeSelf &&
            activeDevice == StoreDeviceKind.Mobile)
        {
            CloseDevice();
            return;
        }

        GameplayMode mode =
            GameBootstrap.Instance.GameplayModes.CurrentMode;

        if (mode != GameplayMode.Gameplay &&
            mode != GameplayMode.Paused)
        {
            return;
        }

        PlayerInteractionController player =
            FindAnyObjectByType<PlayerInteractionController>();

        if (player != null && player.IsHoldingAnything)
        {
            GameBootstrap.Instance.Notifications.Show(
                "Put down the held item before using the phone.",
                NotificationKind.Warning);
            return;
        }

        if (mode == GameplayMode.Paused)
        {
            GameBootstrap.Instance.GameplayModes.Resume();
        }

        OpenDevice(StoreDeviceKind.Mobile);
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
        BindButton(authoredUI.PhoneButton,() =>
        {
            GameBootstrap.Instance.GameplayModes.Resume();
            OpenDevice(StoreDeviceKind.Mobile);
        });
        BindButton(authoredUI.SaveButton,() => SaveSlot(0));
        BindButton(authoredUI.LoadButton,() => LoadSlot(0));
        BindButton(authoredUI.PauseQuitButton,QuitGame);
        BindButton(authoredUI.ApplyPriceButton,ApplyPriceEditor);
        BindButton(authoredUI.CancelPriceButton,ClosePriceEditor);
        BindButton(authoredUI.DeviceCloseButton,CloseDevice);

        StoreApplication[] applications = GetNavigationApplications();
        int count = Mathf.Min(
            applications.Length,
            authoredUI.ApplicationButtons.Length);

        for (int i = 0; i < count; i++)
        {
            StoreApplication application = applications[i];
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
            StoreApplication.History,
            StoreApplication.Tasks,
            StoreApplication.Settings
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
        UIFactory.Clear(deviceContent);
        RectTransform frame = authoredUI.DeviceFrame;

        if (kind == StoreDeviceKind.Desktop)
        {
            frame.anchorMin = new Vector2(0.06f,0.06f);
            frame.anchorMax = new Vector2(0.94f,0.94f);
            authoredUI.DeviceNavigation.anchorMin = Vector2.zero;
            authoredUI.DeviceNavigation.anchorMax = new Vector2(0.18f,0.91f);
            authoredUI.DeviceBody.anchorMin = new Vector2(0.18f,0f);
            authoredUI.DeviceBody.anchorMax = new Vector2(1f,0.91f);
            authoredUI.DeviceBrand.text = "CLERK OS";
        }
        else
        {
            frame.anchorMin = new Vector2(0.28f,0.04f);
            frame.anchorMax = new Vector2(0.72f,0.96f);
            authoredUI.DeviceNavigation.anchorMin = Vector2.zero;
            authoredUI.DeviceNavigation.anchorMax = new Vector2(0.25f,0.91f);
            authoredUI.DeviceBody.anchorMin = new Vector2(0.25f,0f);
            authoredUI.DeviceBody.anchorMax = new Vector2(1f,0.91f);
            authoredUI.DeviceBrand.text = "CLERK";
        }

        frame.offsetMin = Vector2.zero;
        frame.offsetMax = Vector2.zero;
        authoredUI.DeviceNavigation.offsetMin = Vector2.zero;
        authoredUI.DeviceNavigation.offsetMax = Vector2.zero;
        authoredUI.DeviceBody.offsetMin = Vector2.zero;
        authoredUI.DeviceBody.offsetMax = Vector2.zero;
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
        AddSectionTitle(parent,"SUPPLY CO.");

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

        AddActionButton(
            parent,
            "FURNITURE CATALOG",
            () => ShowApplication(
                StoreApplication.Furniture));
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
        AddSectionTitle(parent,"STAFF");

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

        AddMetric(
            parent,
            "CAMERAS ONLINE",
            cameras.Length.ToString());

        for (int i = 0; i < cameras.Length; i++)
        {
            AddMetric(
                parent,
                "CAM " + (i + 1),
                cameras[i].name);
        }
    }

    private void BuildMessages(Transform parent)
    {
        AddSectionTitle(parent,"MESSAGES");
        AddMessage(
            parent,
            "Supply Co.",
            "Deliveries are placed at your loading point.");
        AddMessage(
            parent,
            "Town Commerce",
            "Raise reputation by keeping products available and prices fair.");
        AddMessage(
            parent,
            "Clerk Tips",
            "Customers abandon queues when their patience runs out.");
    }

    private void BuildTasks(Transform parent)
    {
        AddSectionTitle(parent,"TASKS");
        var objectives =
            GameBootstrap.Instance.Objectives.Active;

        if (objectives.Count == 0)
        {
            AddEmptyState(
                parent,
                "No active objectives.");
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
            StoreApplication.Supply => "SUPPLY CO.",
            StoreApplication.Furniture => "FURNITURE",
            StoreApplication.Register => "REGISTER",
            StoreApplication.Bank => "BANK",
            StoreApplication.History => "HISTORY",
            StoreApplication.Staff => "STAFF",
            StoreApplication.Security => "SECURITY",
            StoreApplication.Messages => "MESSAGES",
            StoreApplication.Tasks => "TASKS",
            StoreApplication.Settings => "SETTINGS",
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
