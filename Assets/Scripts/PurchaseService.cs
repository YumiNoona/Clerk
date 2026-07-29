using UnityEngine;

public class PurchaseService : MonoBehaviour
{
    public static PurchaseService Instance { get; private set; }

    [Header("Services")]
    public StockDeliveryService StockDeliveryService;

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
            Debug.LogWarning("Purchase data is null.",this);
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

        bool deliverySucceeded = StockDeliveryService.Deliver(purchaseData);

        if (!deliverySucceeded)
        {
            WalletController.Instance.AddMoney(purchaseData.PurchasePrice);
            Debug.LogWarning("Purchase delivery failed. Money was refunded.",this);
            return false;
        }

        Debug.Log("Purchased " + purchaseData.DisplayName + " for $" + purchaseData.PurchasePrice.ToString("0.00") + ".",this);

        return true;
    }

    [ContextMenu("Test Purchase Lime Juice")]
    private void TestPurchase()
    {
        if (PurchaseCatalog == null)
        {
            Debug.LogWarning("Purchase Catalog is not assigned.",this);
            return;
        }

        if (PurchaseCatalog.StockPurchases == null || PurchaseCatalog.StockPurchases.Count == 0)
        {
            Debug.LogWarning("There are no stock purchases in the catalog.",this);
            return;
        }

        StockPurchaseData firstPurchase = PurchaseCatalog.StockPurchases[0];

        if (firstPurchase == null)
        {
            Debug.LogWarning("The first purchase entry in the catalog is empty.",this);
            return;
        }

        bool purchaseSucceeded = TryPurchaseStock(firstPurchase);

        if (purchaseSucceeded)
        {
            Debug.Log("Test purchase completed successfully.",this);
        }
        else
        {
            Debug.LogWarning("Test purchase failed.",this);
        }
    }
}