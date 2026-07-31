using UnityEngine;

public sealed class StoreExpansionZone :
    MonoBehaviour
{
    [SerializeField]
    private string expansionId;

    [SerializeField]
    private string displayName =
        "Store Expansion";

    [Min(0f)]
    [SerializeField]
    private float purchasePrice = 1000f;

    [Min(1)]
    [SerializeField]
    private int requiredStoreLevel = 2;

    [SerializeField]
    private GameObject lockedBarrier;

    [SerializeField]
    private GameObject unlockedContent;

    public string ExpansionId => expansionId;
    public string DisplayName => displayName;
    public float PurchasePrice => purchasePrice;
    public bool IsUnlocked =>
        GameBootstrap.Instance != null &&
        GameBootstrap.Instance.Progression
            .IsExpansionUnlocked(expansionId);

    private void Start()
    {
        Refresh();
    }

    public bool TryPurchase()
    {
        if (IsUnlocked ||
            GameBootstrap.Instance == null ||
            GameBootstrap.Instance.Progression.StoreLevel <
                requiredStoreLevel ||
            !GameBootstrap.Instance.Economy.TrySpend(
                Money.FromFloat(purchasePrice),
                LedgerEntryType.FurniturePurchase,
                "Unlocked " + displayName,
                expansionId))
        {
            return false;
        }

        GameBootstrap.Instance.Progression
            .UnlockExpansion(expansionId);

        Refresh();
        return true;
    }

    public void Refresh()
    {
        bool unlocked = IsUnlocked;

        if (lockedBarrier != null)
        {
            lockedBarrier.SetActive(!unlocked);
        }

        if (unlockedContent != null)
        {
            unlockedContent.SetActive(unlocked);
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(expansionId))
        {
            expansionId = name
                .Trim()
                .ToLowerInvariant()
                .Replace(" ","_");
        }

        purchasePrice =
            Mathf.Max(0f,purchasePrice);

        requiredStoreLevel =
            Mathf.Max(1,requiredStoreLevel);
    }
}
