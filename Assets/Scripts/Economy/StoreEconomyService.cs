using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class StoreEconomyService : MonoBehaviour
{
    private readonly List<LedgerEntry> entries =
        new List<LedgerEntry>();

    public IReadOnlyList<LedgerEntry> Entries =>
        entries;

    public int CurrentDay { get; private set; } = 1;

    public event Action<LedgerEntry> EntryRecorded;

    public bool CanAfford(Money amount)
    {
        return !amount.IsNegative &&
               WalletController.Instance != null &&
               WalletController.Instance.Balance >= amount;
    }

    public bool TrySpend(
        Money amount,
        LedgerEntryType type,
        string description,
        string relatedId = null)
    {
        if (amount.IsNegative ||
            WalletController.Instance == null ||
            !WalletController.Instance.TrySpend(amount))
        {
            return false;
        }

        Record(
            type,
            new Money(-amount.MinorUnits),
            description,
            relatedId);

        return true;
    }

    public void Refund(
        Money amount,
        string description,
        string relatedId = null)
    {
        if (amount.IsNegative ||
            amount.IsZero ||
            WalletController.Instance == null)
        {
            return;
        }

        WalletController.Instance.Add(amount);

        Record(
            LedgerEntryType.Refund,
            amount,
            description,
            relatedId);
    }

    public bool RecordSale(
        CustomerBasket basket,
        string checkoutId)
    {
        if (basket == null ||
            basket.ItemCount == 0 ||
            WalletController.Instance == null)
        {
            return false;
        }

        Money revenue =
            Money.FromFloat(basket.Total);

        WalletController.Instance.Add(revenue);

        Record(
            LedgerEntryType.Sale,
            revenue,
            "Customer sale",
            checkoutId,
            basket.ItemCount);

        return true;
    }

    public void RecordOperatingCost(
        Money amount,
        string description)
    {
        TrySpend(
            amount,
            LedgerEntryType.OperatingCost,
            description);
    }

    public void GrantFunds(
        Money amount,
        LedgerEntryType type,
        string description,
        string relatedId = null)
    {
        if (amount.IsNegative ||
            amount.IsZero ||
            WalletController.Instance == null)
        {
            return;
        }

        WalletController.Instance.Add(amount);

        Record(
            type,
            amount,
            description,
            relatedId);
    }

    public Money GetRevenueForDay(int day)
    {
        long cents = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            LedgerEntry entry = entries[i];

            if (entry.Day == day &&
                entry.Type == LedgerEntryType.Sale)
            {
                cents += entry.AmountCents;
            }
        }

        return new Money(cents);
    }

    public Money GetExpensesForDay(int day)
    {
        long cents = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            LedgerEntry entry = entries[i];

            if (entry.Day == day &&
                entry.AmountCents < 0)
            {
                cents += -entry.AmountCents;
            }
        }

        return new Money(cents);
    }

    public Money GetProfitForDay(int day)
    {
        return GetRevenueForDay(day) -
               GetExpensesForDay(day);
    }

    public void SetCurrentDay(int day)
    {
        CurrentDay = Mathf.Max(1,day);
    }

    public void RestoreEntries(
        IEnumerable<LedgerEntry> restored)
    {
        entries.Clear();

        if (restored != null)
        {
            entries.AddRange(restored);
        }
    }

    private void Record(
        LedgerEntryType type,
        Money amount,
        string description,
        string relatedId,
        int quantity = 0)
    {
        LedgerEntry entry = new LedgerEntry
        {
            EntryId = Guid.NewGuid().ToString("N"),
            Type = type,
            AmountCents = amount.MinorUnits,
            Day = CurrentDay,
            Description = description ?? string.Empty,
            RelatedId = relatedId ?? string.Empty,
            TimestampUtcTicks = DateTime.UtcNow.Ticks,
            Quantity = Mathf.Max(0,quantity)
        };

        entries.Add(entry);
        EntryRecorded?.Invoke(entry);
    }
}
