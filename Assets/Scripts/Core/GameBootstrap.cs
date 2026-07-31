using UnityEngine;

[DefaultExecutionOrder(-10000)]
public sealed class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }

    public GameplayModeController GameplayModes
    {
        get;
        private set;
    }

    public GameInputController Input
    {
        get;
        private set;
    }

    public ProductStateService Products
    {
        get;
        private set;
    }

    public ShelfRegistry Shelves
    {
        get;
        private set;
    }

    public CustomerRegistry Customers
    {
        get;
        private set;
    }

    public CheckoutRegistry Checkouts
    {
        get;
        private set;
    }

    public WalletController Wallet
    {
        get;
        private set;
    }

    public StoreEconomyService Economy
    {
        get;
        private set;
    }

    public ProductDemandService Demand
    {
        get;
        private set;
    }

    public StoreFinanceService Finance
    {
        get;
        private set;
    }

    public StoreDayController Days
    {
        get;
        private set;
    }

    public StoreStatisticsService Statistics
    {
        get;
        private set;
    }

    public ProgressionService Progression
    {
        get;
        private set;
    }

    public GameSettingsService Settings
    {
        get;
        private set;
    }

    public SaveGameService Saves
    {
        get;
        private set;
    }

    public FurnitureService Furniture
    {
        get;
        private set;
    }

    public ObjectiveService Objectives
    {
        get;
        private set;
    }

    public EmployeeService Employees
    {
        get;
        private set;
    }

    public NotificationService Notifications
    {
        get;
        private set;
    }

    public StoreUIService UI
    {
        get;
        private set;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureRuntimeExists()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject runtimeObject =
            new GameObject("[Clerk Runtime]");

        runtimeObject.AddComponent<GameBootstrap>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameplayModes =
            GetOrAdd<GameplayModeController>();

        Input =
            GetOrAdd<GameInputController>();

        Products =
            GetOrAdd<ProductStateService>();

        Shelves =
            GetOrAdd<ShelfRegistry>();

        Customers =
            GetOrAdd<CustomerRegistry>();

        Checkouts =
            GetOrAdd<CheckoutRegistry>();

        // The wallet is runtime state, just like the economy and finance
        // services. Keeping it on the persistent composition root makes
        // loans, purchases, sales, and saves work in every gameplay scene
        // without requiring an authored UI object.
        Wallet =
            GetOrAdd<WalletController>();

        Economy =
            GetOrAdd<StoreEconomyService>();

        Demand =
            GetOrAdd<ProductDemandService>();

        Finance =
            GetOrAdd<StoreFinanceService>();

        Days =
            GetOrAdd<StoreDayController>();

        Statistics =
            GetOrAdd<StoreStatisticsService>();

        Progression =
            GetOrAdd<ProgressionService>();

        Settings =
            GetOrAdd<GameSettingsService>();

        Saves =
            GetOrAdd<SaveGameService>();

        Furniture =
            GetOrAdd<FurnitureService>();

        Objectives =
            GetOrAdd<ObjectiveService>();

        Employees =
            GetOrAdd<EmployeeService>();

        Notifications =
            GetOrAdd<NotificationService>();

        UI =
            GetOrAdd<StoreUIService>();
    }

    private T GetOrAdd<T>()
        where T : Component
    {
        T component = GetComponent<T>();

        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private void Update()
    {
        if (Input == null ||
            GameplayModes == null ||
            !Input.WasPressedThisFrame(
                GameplayAction.Pause))
        {
            return;
        }

        if (GameplayModes.CurrentMode ==
            GameplayMode.Gameplay ||
            GameplayModes.CurrentMode ==
            GameplayMode.Paused)
        {
            GameplayModes.TogglePause();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
