using UnityEngine;

public abstract class PurchasableData : ScriptableObject
{
    [SerializeField,HideInInspector]
    private string purchaseId;

    [Header("Display")]
    public string DisplayName;
    public Sprite Icon;

    [Header("Purchase")]
    public float PurchasePrice;

    [Header("Unlock")]
    public bool UnlockedByDefault = true;
    public int RequiredStoreLevel = 1;

    public string PurchaseId
    {
        get
        {
            return purchaseId;
        }
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);

        if (!string.IsNullOrEmpty(assetPath))
        {
            string assetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);

            if (purchaseId != assetGuid)
            {
                purchaseId = assetGuid;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        PurchasePrice = Mathf.Max(0f,PurchasePrice);
        RequiredStoreLevel =
            Mathf.Max(1,RequiredStoreLevel);
    }
#endif
}
