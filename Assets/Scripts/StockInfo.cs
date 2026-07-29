using UnityEngine;

[CreateAssetMenu(fileName = "New Stock Product",menuName = "Stock System/Stock Product")]
public class StockInfo : ScriptableObject
{
    [SerializeField,HideInInspector]
    private string productId;

    public string ProductName;
    public StockCategory Category;

    public float BasePrice;
    public float CurrentPrice;

    public StockObject StockPrefab;
    public BoxLayout DefaultBoxLayout;

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

        BasePrice = Mathf.Max(0f,BasePrice);
        CurrentPrice = Mathf.Max(0f,CurrentPrice);
    }
#endif
}