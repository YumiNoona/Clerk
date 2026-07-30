using UnityEngine;

public class PurchaseService : MonoBehaviour
{
    public static PurchaseService Instance { get; private set; }

    [Header("Services")]
    public StockDeliveryService StockDeliveryService;
    public FurnitureDeliveryService FurnitureDeliveryService;

    [Header("Catalog")]
    public PurchaseCatalog PurchaseCatalog;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool TryPurchaseStock(StockPurchaseData purchaseData)
    {
        if (purchaseData == null)
        {
            Debug.LogWarning("Stock purchase data is null.",this);
            return false;
        }

        if (WalletController.Instance == null)
        {
            Debug.LogWarning("WalletController is missing from the scene.",this);
            return false;
        }

        if (StockDeliveryService == null)
        {
            Debug.LogWarning("StockDeliveryService is not assigned.",this);
            return false;
        }

        if (!WalletController.Instance.TrySpendMoney(purchaseData.PurchasePrice))
        {
            Debug.LogWarning("Not enough money to purchase " + purchaseData.DisplayName + ".",this);
            return false;
        }

        if (!StockDeliveryService.Deliver(purchaseData))
        {
            WalletController.Instance.AddMoney(purchaseData.PurchasePrice);
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

        if (WalletController.Instance == null)
        {
            Debug.LogWarning("WalletController is missing from the scene.",this);
            return false;
        }

        if (FurnitureDeliveryService == null)
        {
            Debug.LogWarning("FurnitureDeliveryService is not assigned.",this);
            return false;
        }

        if (!WalletController.Instance.TrySpendMoney(purchaseData.PurchasePrice))
        {
            Debug.LogWarning("Not enough money to purchase " + purchaseData.DisplayName + ".",this);
            return false;
        }

        if (!FurnitureDeliveryService.Deliver(purchaseData))
        {
            WalletController.Instance.AddMoney(purchaseData.PurchasePrice);
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
}