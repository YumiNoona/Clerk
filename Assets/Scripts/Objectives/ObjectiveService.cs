using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ObjectiveProgressData
{
    public string ObjectiveId;
    public int Progress;
    public bool Completed;
    public bool RewardClaimed;
}

public sealed class ObjectiveProgress
{
    public ObjectiveDefinition Definition { get; }
    public int Progress { get; private set; }
    public bool Completed { get; private set; }
    public bool RewardClaimed { get; private set; }

    public ObjectiveProgress(
        ObjectiveDefinition definition)
    {
        Definition = definition;
    }

    public bool AddProgress(int amount)
    {
        if (Completed || amount <= 0)
        {
            return false;
        }

        Progress = Mathf.Min(
            Definition.TargetAmount,
            Progress + amount);

        Completed =
            Progress >= Definition.TargetAmount;

        return true;
    }

    public void MarkRewardClaimed()
    {
        RewardClaimed = true;
    }

    public ObjectiveProgressData Capture()
    {
        return new ObjectiveProgressData
        {
            ObjectiveId = Definition.ObjectiveId,
            Progress = Progress,
            Completed = Completed,
            RewardClaimed = RewardClaimed
        };
    }

    public void Restore(ObjectiveProgressData data)
    {
        Progress = Mathf.Clamp(
            data.Progress,
            0,
            Definition.TargetAmount);

        Completed =
            data.Completed ||
            Progress >= Definition.TargetAmount;

        RewardClaimed = data.RewardClaimed;
    }
}

public sealed class ObjectiveService : MonoBehaviour
{
    private readonly List<ObjectiveProgress> active =
        new List<ObjectiveProgress>();

    public IReadOnlyList<ObjectiveProgress> Active =>
        active;

    public event Action<ObjectiveProgress>
        ObjectiveChanged;

    public event Action<ObjectiveProgress>
        ObjectiveCompleted;

    private void Start()
    {
        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Economy.EntryRecorded +=
                HandleLedgerEntry;

            GameBootstrap.Instance.Progression
                .StoreLevelChanged +=
                HandleStoreLevelChanged;

            GameBootstrap.Instance.Days.DayEnded +=
                HandleDayEnded;
        }
    }

    public bool AddObjective(
        ObjectiveDefinition definition)
    {
        if (definition == null ||
            Find(definition.ObjectiveId) != null)
        {
            return false;
        }

        ObjectiveProgress progress =
            new ObjectiveProgress(definition);

        active.Add(progress);
        ObjectiveChanged?.Invoke(progress);
        return true;
    }

    public void Report(
        ObjectiveType type,
        int amount = 1)
    {
        for (int i = 0; i < active.Count; i++)
        {
            ObjectiveProgress progress = active[i];

            if (progress.Definition.Type != type)
            {
                continue;
            }

            bool wasComplete = progress.Completed;

            if (!progress.AddProgress(amount))
            {
                continue;
            }

            ObjectiveChanged?.Invoke(progress);

            if (!wasComplete && progress.Completed)
            {
                ObjectiveCompleted?.Invoke(progress);
            }
        }
    }

    public bool ClaimReward(string objectiveId)
    {
        ObjectiveProgress progress = Find(objectiveId);

        if (progress == null ||
            !progress.Completed ||
            progress.RewardClaimed ||
            GameBootstrap.Instance == null)
        {
            return false;
        }

        progress.MarkRewardClaimed();

        if (progress.Definition.MoneyReward > 0f)
        {
            GameBootstrap.Instance.Economy.GrantFunds(
                Money.FromFloat(
                    progress.Definition.MoneyReward),
                LedgerEntryType.Adjustment,
                "Objective reward",
                objectiveId);
        }

        GameBootstrap.Instance.Progression
            .AddExperience(
                progress.Definition.ExperienceReward);

        ObjectiveChanged?.Invoke(progress);
        return true;
    }

    public List<ObjectiveProgressData> Capture()
    {
        List<ObjectiveProgressData> data =
            new List<ObjectiveProgressData>();

        for (int i = 0; i < active.Count; i++)
        {
            data.Add(active[i].Capture());
        }

        return data;
    }

    public void Restore(
        IEnumerable<ObjectiveProgressData> data)
    {
        if (data == null)
        {
            return;
        }

        foreach (ObjectiveProgressData saved in data)
        {
            ObjectiveProgress progress =
                Find(saved.ObjectiveId);

            if (progress != null)
            {
                progress.Restore(saved);
                ObjectiveChanged?.Invoke(progress);
            }
        }
    }

    private void HandleLedgerEntry(LedgerEntry entry)
    {
        switch (entry.Type)
        {
            case LedgerEntryType.StockPurchase:
                Report(ObjectiveType.PurchaseStock);
                break;

            case LedgerEntryType.FurniturePurchase:
                Report(ObjectiveType.PurchaseFurniture);
                break;

            case LedgerEntryType.Sale:
                Report(ObjectiveType.ServeCustomers);
                Report(
                    ObjectiveType.SellItems,
                    Mathf.Max(1,entry.Quantity));

                Report(
                    ObjectiveType.EarnRevenue,
                    Mathf.Max(
                        0,
                        Mathf.RoundToInt(
                            entry.Amount.AsFloat)));

                break;
        }
    }

    private void HandleStoreLevelChanged(int level)
    {
        Report(
            ObjectiveType.ReachStoreLevel,
            1);
    }

    private void HandleDayEnded(int day)
    {
        Money profit =
            GameBootstrap.Instance.Economy
                .GetProfitForDay(day);

        if (!profit.IsNegative)
        {
            Report(
                ObjectiveType.ReachProfit,
                Mathf.RoundToInt(
                    profit.AsFloat));
        }
    }

    private ObjectiveProgress Find(string objectiveId)
    {
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].Definition.ObjectiveId ==
                objectiveId)
            {
                return active[i];
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Economy.EntryRecorded -=
                HandleLedgerEntry;

            GameBootstrap.Instance.Progression
                .StoreLevelChanged -=
                HandleStoreLevelChanged;

            GameBootstrap.Instance.Days.DayEnded -=
                HandleDayEnded;
        }
    }
}
