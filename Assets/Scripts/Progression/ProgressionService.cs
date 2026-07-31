using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ProgressionData
{
    public int StoreLevel = 1;
    public int Experience;
    public float Reputation;
    public List<string> UnlockedProductIds =
        new List<string>();

    public List<string> UnlockedPurchaseIds =
        new List<string>();

    public List<string> UnlockedExpansionIds =
        new List<string>();
}

public sealed class ProgressionService : MonoBehaviour
{
    private readonly HashSet<string> unlockedProducts =
        new HashSet<string>();

    private readonly HashSet<string> unlockedPurchases =
        new HashSet<string>();

    private readonly HashSet<string> unlockedExpansions =
        new HashSet<string>();

    public int StoreLevel { get; private set; } = 1;
    public int Experience { get; private set; }
    public float Reputation { get; private set; }
    public int ExperienceForNextLevel =>
        GetRequiredExperience(StoreLevel + 1);

    public event Action ProgressionChanged;
    public event Action<int> StoreLevelChanged;

    public void AddExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Experience += amount;

        while (Experience >=
               GetRequiredExperience(StoreLevel + 1))
        {
            StoreLevel++;
            StoreLevelChanged?.Invoke(StoreLevel);
        }

        ProgressionChanged?.Invoke();
    }

    public void AddReputation(float amount)
    {
        if (Mathf.Approximately(amount,0f))
        {
            return;
        }

        Reputation = Mathf.Clamp(
            Reputation + amount,
            0f,
            100f);

        ProgressionChanged?.Invoke();
    }

    public bool UnlockProduct(string productId)
    {
        return Unlock(
            productId,
            unlockedProducts);
    }

    public bool UnlockPurchase(string purchaseId)
    {
        return Unlock(
            purchaseId,
            unlockedPurchases);
    }

    public bool UnlockExpansion(string expansionId)
    {
        return Unlock(
            expansionId,
            unlockedExpansions);
    }

    public bool IsProductUnlocked(string productId)
    {
        return IsUnlocked(
            productId,
            unlockedProducts);
    }

    public bool IsPurchaseUnlocked(string purchaseId)
    {
        return IsUnlocked(
            purchaseId,
            unlockedPurchases);
    }

    public bool IsExpansionUnlocked(string expansionId)
    {
        return IsUnlocked(
            expansionId,
            unlockedExpansions);
    }

    public ProgressionData Capture()
    {
        return new ProgressionData
        {
            StoreLevel = StoreLevel,
            Experience = Experience,
            Reputation = Reputation,
            UnlockedProductIds =
                new List<string>(unlockedProducts),
            UnlockedPurchaseIds =
                new List<string>(unlockedPurchases),
            UnlockedExpansionIds =
                new List<string>(unlockedExpansions)
        };
    }

    public void Restore(ProgressionData data)
    {
        data ??= new ProgressionData();

        StoreLevel = Mathf.Max(1,data.StoreLevel);
        Experience = Mathf.Max(0,data.Experience);
        Reputation =
            Mathf.Clamp(data.Reputation,0f,100f);

        RestoreSet(
            unlockedProducts,
            data.UnlockedProductIds);

        RestoreSet(
            unlockedPurchases,
            data.UnlockedPurchaseIds);

        RestoreSet(
            unlockedExpansions,
            data.UnlockedExpansionIds);

        ProgressionChanged?.Invoke();
        StoreLevelChanged?.Invoke(StoreLevel);
    }

    private bool Unlock(
        string id,
        HashSet<string> collection)
    {
        if (string.IsNullOrWhiteSpace(id) ||
            !collection.Add(id))
        {
            return false;
        }

        ProgressionChanged?.Invoke();
        return true;
    }

    private static bool IsUnlocked(
        string id,
        HashSet<string> collection)
    {
        return string.IsNullOrWhiteSpace(id) ||
               collection.Count == 0 ||
               collection.Contains(id);
    }

    private static int GetRequiredExperience(
        int level)
    {
        int safeLevel = Mathf.Max(1,level);
        return (safeLevel - 1) *
               (safeLevel - 1) *
               100;
    }

    private static void RestoreSet(
        HashSet<string> target,
        IEnumerable<string> values)
    {
        target.Clear();

        if (values == null)
        {
            return;
        }

        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                target.Add(value);
            }
        }
    }
}
