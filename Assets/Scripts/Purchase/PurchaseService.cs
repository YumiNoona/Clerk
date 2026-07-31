using UnityEngine;

public class PurchaseService : MonoBehaviour
{
    public static PurchaseService Instance { get; private set; }

    [Header("Services")]
    public StockDeliveryService StockDeliveryService;
    public FurnitureDeliveryService FurnitureDeliveryService;

    [Header("Catalog")]
    public PurchaseCatalog PurchaseCatalog;

    [Header("Customers")]
    public CustomerDatabase CustomerDatabase;

    [Header("Objectives")]
    public ObjectiveDefinition[] StartingObjectives;

    [Header("Employees")]
    public EmployeeDefinition[] EmployeeCatalog;

    [Header("Player Devices")]
    public GameObject MobileModel;

    [Header("Starter Checkout")]
    public GameObject CheckoutModel;
    public bool CreateStarterCheckout = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Saves
                .Configure(PurchaseCatalog);

            GameBootstrap.Instance.Furniture
                .Configure(PurchaseCatalog);
        }

        EnsureCustomerSpawner();

        if (GameBootstrap.Instance != null &&
            StartingObjectives != null)
        {
            for (int i = 0;
                 i < StartingObjectives.Length;
                 i++)
            {
                GameBootstrap.Instance.Objectives
                    .AddObjective(
                        StartingObjectives[i]);
            }
        }

        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.UI
                .ConfigureMobileModel(MobileModel);
        }

        EnsureStarterCheckout();
    }

    private void EnsureStarterCheckout()
    {
        if (!CreateStarterCheckout ||
            CheckoutModel == null ||
            FindAnyObjectByType<CheckoutCounter>() != null)
        {
            return;
        }

        Transform spawn =
            FurnitureDeliveryService != null &&
            FurnitureDeliveryService
                .FurnitureSpawnPoint != null
                ? FurnitureDeliveryService
                    .FurnitureSpawnPoint
                : transform;

        GameObject checkout =
            Instantiate(
                CheckoutModel,
                spawn.position + spawn.right * 2f,
                spawn.rotation);

        checkout.name = "Starter Checkout";

        BoxCollider placementBounds =
            checkout.GetComponent<BoxCollider>();

        if (placementBounds == null)
        {
            placementBounds =
                checkout.AddComponent<BoxCollider>();

            Renderer renderer =
                checkout.GetComponentInChildren<
                    Renderer>();

            if (renderer != null)
            {
                Bounds world = renderer.bounds;
                placementBounds.center =
                    checkout.transform
                        .InverseTransformPoint(
                            world.center);

                Vector3 scale =
                    checkout.transform.lossyScale;

                placementBounds.size =
                    new Vector3(
                        world.size.x /
                        Mathf.Max(0.001f,
                            Mathf.Abs(scale.x)),
                        world.size.y /
                        Mathf.Max(0.001f,
                            Mathf.Abs(scale.y)),
                        world.size.z /
                        Mathf.Max(0.001f,
                            Mathf.Abs(scale.z)));
            }
        }

        PlaceableFurniture furniture =
            checkout.GetComponent<
                PlaceableFurniture>() ??
            checkout.AddComponent<
                PlaceableFurniture>();

        furniture.PlacementBounds =
            placementBounds;

        furniture.RestoreIdentity(
            "starter_checkout",
            string.Empty,
            false);

        if (checkout.GetComponent<
                CheckoutCounter>() == null)
        {
            checkout.AddComponent<CheckoutCounter>();
        }
    }

    private void EnsureCustomerSpawner()
    {
        if (CustomerDatabase == null)
        {
            return;
        }

        CustomerSpawner spawner =
            FindAnyObjectByType<CustomerSpawner>();

        if (spawner == null)
        {
            GameObject spawnerObject =
                new GameObject("Customer Spawner");

            spawner =
                spawnerObject.AddComponent<
                    CustomerSpawner>();
        }

        spawner.Configure(
            CustomerDatabase,
            PurchaseCatalog);
    }

    public bool TryPurchaseStock(StockPurchaseData purchaseData)
    {
        if (purchaseData == null)
        {
            Debug.LogWarning("Stock purchase data is null.",this);
            return false;
        }

        if (GameBootstrap.Instance == null)
        {
            Debug.LogWarning("Game runtime is missing.",this);
            return false;
        }

        if (!IsUnlocked(purchaseData))
        {
            Debug.LogWarning(
                purchaseData.DisplayName +
                " is not unlocked.",
                this);

            return false;
        }

        if (StockDeliveryService == null)
        {
            Debug.LogWarning("StockDeliveryService is not assigned.",this);
            return false;
        }

        Money price =
            Money.FromFloat(
                purchaseData.PurchasePrice);

        if (!GameBootstrap.Instance.Economy.TrySpend(
                price,
                LedgerEntryType.StockPurchase,
                "Purchased " +
                purchaseData.DisplayName,
                purchaseData.PurchaseId))
        {
            Debug.LogWarning("Not enough money to purchase " + purchaseData.DisplayName + ".",this);
            return false;
        }

        if (!StockDeliveryService.Deliver(purchaseData))
        {
            GameBootstrap.Instance.Economy.Refund(
                price,
                "Refunded failed stock delivery",
                purchaseData.PurchaseId);

            Debug.LogWarning("Stock delivery failed. Money was refunded.",this);
            return false;
        }

        Debug.Log("Purchased " + purchaseData.DisplayName + " for $" + purchaseData.PurchasePrice.ToString("0.00") + ".",this);
        return true;
    }

    public bool TryPurchaseFurniture(FurniturePurchaseData purchaseData)
    {
        if (purchaseData == null || purchaseData.FurniturePrefab == null)
        {
            Debug.LogWarning("Furniture purchase data or furniture prefab is missing.",this);
            return false;
        }

        if (GameBootstrap.Instance == null)
        {
            Debug.LogWarning("Game runtime is missing.",this);
            return false;
        }

        if (!IsUnlocked(purchaseData))
        {
            Debug.LogWarning(
                purchaseData.DisplayName +
                " is not unlocked.",
                this);

            return false;
        }

        if (FurnitureDeliveryService == null)
        {
            Debug.LogWarning("FurnitureDeliveryService is not assigned.",this);
            return false;
        }

        Money price =
            Money.FromFloat(
                purchaseData.PurchasePrice);

        if (!GameBootstrap.Instance.Economy.TrySpend(
                price,
                LedgerEntryType.FurniturePurchase,
                "Purchased " +
                purchaseData.DisplayName,
                purchaseData.PurchaseId))
        {
            Debug.LogWarning("Not enough money to purchase " + purchaseData.DisplayName + ".",this);
            return false;
        }

        if (!FurnitureDeliveryService.Deliver(purchaseData))
        {
            GameBootstrap.Instance.Economy.Refund(
                price,
                "Refunded failed furniture delivery",
                purchaseData.PurchaseId);

            Debug.LogWarning("Furniture delivery failed. Money was refunded.",this);
            return false;
        }

        Debug.Log("Purchased " + purchaseData.DisplayName + " for $" + purchaseData.PurchasePrice.ToString("0.00") + ".",this);
        return true;
    }

    [ContextMenu("Test Purchase First Stock")]
    private void TestPurchaseFirstStock()
    {
        if (PurchaseCatalog == null || PurchaseCatalog.StockPurchases == null || PurchaseCatalog.StockPurchases.Count == 0)
        {
            Debug.LogWarning("There are no stock purchases in the catalog.",this);
            return;
        }

        TryPurchaseStock(PurchaseCatalog.StockPurchases[0]);
    }

    [ContextMenu("Test Purchase First Furniture")]
    private void TestPurchaseFirstFurniture()
    {
        if (PurchaseCatalog == null || PurchaseCatalog.FurniturePurchases == null || PurchaseCatalog.FurniturePurchases.Count == 0)
        {
            Debug.LogWarning("There are no furniture purchases in the catalog.",this);
            return;
        }

        TryPurchaseFurniture(PurchaseCatalog.FurniturePurchases[0]);
    }

    public bool TryHireEmployee(
        EmployeeDefinition definition,
        out EmployeeContext employee)
    {
        employee = null;

        if (GameBootstrap.Instance == null)
        {
            return false;
        }

        Transform spawn =
            FurnitureDeliveryService != null &&
            FurnitureDeliveryService
                .FurnitureSpawnPoint != null
                ? FurnitureDeliveryService
                    .FurnitureSpawnPoint
                : transform;

        return GameBootstrap.Instance.Employees.TryHire(
                   definition,
                   spawn.position,
                   spawn.rotation,
                   out employee);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private static bool IsUnlocked(
        PurchasableData purchase)
    {
        if (purchase == null)
        {
            return false;
        }

        if (purchase.UnlockedByDefault &&
            purchase.RequiredStoreLevel <= 1)
        {
            return true;
        }

        if (GameBootstrap.Instance == null)
        {
            return false;
        }

        return
            GameBootstrap.Instance.Progression.StoreLevel >=
                purchase.RequiredStoreLevel &&
            (purchase.UnlockedByDefault ||
             GameBootstrap.Instance.Progression
                 .IsPurchaseUnlocked(
                     purchase.PurchaseId));
    }
}
