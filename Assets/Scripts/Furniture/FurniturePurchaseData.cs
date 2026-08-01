using UnityEngine;

[CreateAssetMenu(fileName = "New Furniture Purchase",menuName = "Clerk/Furniture/Purchase Entry")]
public class FurniturePurchaseData : PurchasableData
{
    [Header("Furniture")]
    public PlaceableFurniture FurniturePrefab;
}
