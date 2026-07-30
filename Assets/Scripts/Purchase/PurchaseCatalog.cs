using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Purchase Catalog",menuName = "Store System/Purchase Catalog")]
public class PurchaseCatalog : ScriptableObject
{
    [Header("Stock")]
    public List<StockPurchaseData> StockPurchases = new List<StockPurchaseData>();

    [Header("Furniture")]
    public List<FurniturePurchaseData> FurniturePurchases = new List<FurniturePurchaseData>();
}