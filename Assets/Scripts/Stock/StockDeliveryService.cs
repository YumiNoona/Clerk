using UnityEngine;

public class StockDeliveryService : MonoBehaviour
{
    [Header("Delivery")]
    public Transform DeliverySpawnPoint;
    private bool hasConfiguredPose;
    private Vector3 configuredPosition;
    private Quaternion configuredRotation;

    public void ConfigureSpawnPose(Vector3 position,Quaternion rotation)
    {
        configuredPosition = position;
        configuredRotation = rotation;
        hasConfiguredPose = true;
        DeliverySpawnPoint = null;
    }

    public bool Deliver(StockPurchaseData purchaseData)
    {
        if (purchaseData == null)
        {
            return false;
        }

        if (purchaseData.Product == null || purchaseData.BoxPrefab == null)
        {
            Debug.LogWarning("Stock purchase data is missing its Product or Box Prefab.",purchaseData);
            return false;
        }

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;

        if (hasConfiguredPose)
        {
            spawnPosition = configuredPosition;
            spawnRotation = configuredRotation;
        }
        else if (DeliverySpawnPoint != null)
        {
            spawnPosition = DeliverySpawnPoint.position;
            spawnRotation = DeliverySpawnPoint.rotation;
        }

        StockBoxController deliveredBox = Instantiate(purchaseData.BoxPrefab,spawnPosition,spawnRotation);

        deliveredBox.InitializeDelivery(
            purchaseData.PurchaseId);

        deliveredBox.Product = purchaseData.Product;
        deliveredBox.Layout = purchaseData.Product.DefaultBoxLayout;

        int maximumQuantity = deliveredBox.MaximumQuantity;

        if (maximumQuantity > 0)
        {
            deliveredBox.Quantity = Mathf.Clamp(purchaseData.QuantityPerBox,0,maximumQuantity);
        }
        else
        {
            deliveredBox.Quantity = Mathf.Max(0,purchaseData.QuantityPerBox);
        }

        return true;
    }
}
