using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "New Stock Product",menuName = "Stock System/Stock Product")]
public class StockInfo : ScriptableObject
{
    [SerializeField,HideInInspector]
    private string productId;

    public string ProductName;
    public StockCategory Category;

    public float BasePrice;

    [FormerlySerializedAs("CurrentPrice")]
    [SerializeField]
    private float initialPrice;

    public StockObject StockPrefab;
    public BoxLayout DefaultBoxLayout;

    public string ProductId
    {
        get
        {
            return productId;
        }
    }

    public float InitialPrice => initialPrice;

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
        initialPrice =
            Mathf.Max(0f,initialPrice);
    }
#endif
}
