using UnityEngine;

[CreateAssetMenu(fileName = "New Stock Product",menuName = "Stock System/Stock Product")]
public class StockInfo : ScriptableObject
{
    [SerializeField,HideInInspector]
    private string productId;

    [Header("Product Information")]
    public string ProductName;

    [Header("Category")]
    public StockCategory Category;

    [Header("Pricing")]
    public float BasePrice;
    public float CurrentPrice;

    public string ProductId
    {
        get
        {
            return productId;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);

        if (!string.IsNullOrEmpty(assetPath))
        {
            string assetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);

            if (productId != assetGuid)
            {
                productId = assetGuid;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        if (BasePrice < 0f)
        {
            BasePrice = 0f;
        }

        if (CurrentPrice < 0f)
        {
            CurrentPrice = 0f;
        }
    }
#endif
}