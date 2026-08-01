using UnityEngine;

public class FurnitureDeliveryService : MonoBehaviour
{
    [Header("Delivery")]
    public Transform FurnitureSpawnPoint;
    private bool hasConfiguredPose;
    private Vector3 configuredPosition;
    private Quaternion configuredRotation;

    public void ConfigureSpawnPose(Vector3 position,Quaternion rotation)
    {
        configuredPosition = position;
        configuredRotation = rotation;
        hasConfiguredPose = true;
        FurnitureSpawnPoint = null;
    }

    public bool Deliver(FurniturePurchaseData purchaseData)
    {
        if (purchaseData == null || purchaseData.FurniturePrefab == null)
        {
            Debug.LogWarning("Furniture purchase data or furniture prefab is missing.",this);
            return false;
        }

        Vector3 spawnPosition = hasConfiguredPose
            ? configuredPosition
            : FurnitureSpawnPoint != null
                ? FurnitureSpawnPoint.position
                : transform.position;
        Quaternion spawnRotation = hasConfiguredPose
            ? configuredRotation
            : FurnitureSpawnPoint != null
                ? FurnitureSpawnPoint.rotation
                : transform.rotation;

        PlaceableFurniture deliveredFurniture = Instantiate(purchaseData.FurniturePrefab,spawnPosition,spawnRotation);

        if (deliveredFurniture == null)
        {
            return false;
        }

        deliveredFurniture.InitializePurchase(
            purchaseData.PurchaseId);

        deliveredFurniture.transform.rotation = Quaternion.Euler(0f,spawnRotation.eulerAngles.y,0f);

        return true;
    }
}
