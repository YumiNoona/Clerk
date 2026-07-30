using UnityEngine;

[CreateAssetMenu(menuName = "Store/Furniture Purchase")]
public class FurniturePurchaseData : PurchasableData
{
    [Header("Furniture")]
    public PlaceableFurniture FurniturePrefab;
}