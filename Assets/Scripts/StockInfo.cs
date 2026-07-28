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
    public float Price;

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

        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        string assetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);

        if (productId != assetGuid)
        {
            productId = assetGuid;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}