using UnityEngine;

[CreateAssetMenu(fileName = "New Stock Purchase",menuName = "Store System/Purchases/Stock Purchase")]
public class StockPurchaseData : PurchasableData
{
    [Header("Stock")]
    public StockInfo Product;

    [Header("Delivery")]
    public StockBoxController BoxPrefab;
    public int QuantityPerBox = 12;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (QuantityPerBox < 1)
        {
            QuantityPerBox = 1;
        }
    }
#endif
}