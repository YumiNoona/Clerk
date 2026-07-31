using UnityEngine;

public class FurnitureDeliveryService : MonoBehaviour
{
    [Header("Delivery")]
    public Transform FurnitureSpawnPoint;

    public bool Deliver(FurniturePurchaseData purchaseData)
    {
        if (purchaseData == null || purchaseData.FurniturePrefab == null)
        {
            Debug.LogWarning("Furniture purchase data or furniture prefab is missing.",this);
            return false;
        }

        Vector3 spawnPosition = FurnitureSpawnPoint != null ? FurnitureSpawnPoint.position : transform.position;
        Quaternion spawnRotation = FurnitureSpawnPoint != null ? FurnitureSpawnPoint.rotation : Quaternion.identity;

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
