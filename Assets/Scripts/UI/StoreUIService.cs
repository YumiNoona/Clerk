using System;
using System.Collections;
using System.Collections.Generic;
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
    private GameObject mobileModelPrefab;
    private GameObject mobileModelInstance;

    private void Awake()
    {
        // The runtime starts on the generated main menu. Pause before the
        // first rendered frame so the cursor never briefly enters locked
        // gameplay mode and consumes the player's first click.
        GameBootstrap.Instance?.GameplayModes.Pause();
    }

    private void Start()
    {
        BuildCanvas();
        BuildHud();
        BuildPauseMenu();
        BuildMainMenu();
        BuildNotificationLayer();

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

        if (deviceRoot != null &&
            deviceRoot.gameObject.activeSelf &&
            !isRebinding &&
            GameBootstrap.Instance.Input.WasPressedThisFrame(
                GameplayAction.Cancel))
        {
            CloseDevice();
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
            mobileModelInstance =
                Instantiate(
                    mobileModelPrefab,
                    Camera.main.transform);

            mobileModelInstance.name =
                "Player Mobile Device";

            mobileModelInstance.transform.localPosition =
                new Vector3(0.42f,-0.28f,0.75f);

            mobileModelInstance.transform.localRotation =
                Quaternion.Euler(8f,180f,0f);

            mobileModelInstance.transform.localScale =
                Vector3.one * 0.12f;
        }

        if (mobileModelInstance != null)
        {
            mobileModelInstance.SetActive(true);
        }
    }

    public void ShowApplication(
        StoreApplication application)
    {
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

    private void BuildDeviceShell(StoreDeviceKind kind)
    {
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
        {
            StoreApplication.Overview,
            StoreApplication.Supply,
            StoreApplication.Register,
            StoreApplication.Bank,
            StoreApplication.Tasks,
            StoreApplication.Settings
        };

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
        }

        IReadOnlyList<LedgerEntry> entries =
            GameBootstrap.Instance.Economy.Entries;

        int start = Mathf.Max(0,entries.Count - 12);

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
            StoreApplication.Overview => "⌂",
            StoreApplication.Supply => "▣",
            StoreApplication.Register => "▤",
            StoreApplication.Bank => "$",
            StoreApplication.Tasks => "✓",
            StoreApplication.Settings => "⚙",
            _ => "•"
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
