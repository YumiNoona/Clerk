using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Purchase Catalog",menuName = "Store System/Purchase Catalog")]
public class PurchaseCatalog : ScriptableObject
{
    public List<StockPurchaseData> StockPurchases = new List<StockPurchaseData>();
}