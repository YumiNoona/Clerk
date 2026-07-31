using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StoreStatisticsData
{
    public int CustomersSpawned;
    public int CustomersServed;
    public int ItemsSold;
    public int SalesCompleted;
    public long LifetimeRevenueCents;
    public long LifetimeExpensesCents;
}

public sealed class StoreStatisticsService : MonoBehaviour
{
    public StoreStatisticsData Data { get; private set; } =
        new StoreStatisticsData();

    public event Action StatisticsChanged;

    private void Start()
    {
        if (GameBootstrap.Instance == null)
        {
            return;
        }

        GameBootstrap.Instance.Economy.EntryRecorded +=
            HandleLedgerEntry;

        GameBootstrap.Instance.Customers.CustomerRegistered +=
            HandleCustomerRegistered;

        GameBootstrap.Instance.Checkouts.CounterRegistered +=
            HandleCounterRegistered;

        IReadOnlyList<CheckoutCounter> counters =
            GameBootstrap.Instance.Checkouts.Counters;

        for (int i = 0; i < counters.Count; i++)
        {
            HandleCounterRegistered(counters[i]);
        }
    }

    private void HandleLedgerEntry(LedgerEntry entry)
    {
        if (entry.AmountCents >= 0)
        {
            Data.LifetimeRevenueCents +=
                entry.AmountCents;
        }
        else
        {
            Data.LifetimeExpensesCents +=
                -entry.AmountCents;
        }

        if (entry.Type == LedgerEntryType.Sale)
        {
            Data.SalesCompleted++;
            Data.ItemsSold += entry.Quantity;
        }

        StatisticsChanged?.Invoke();
    }

    private void HandleCustomerRegistered(
        CustomerContext customer)
    {
        Data.CustomersSpawned++;
        StatisticsChanged?.Invoke();
    }

    private void HandleCounterRegistered(
        CheckoutCounter counter)
    {
        if (counter == null)
        {
            return;
        }

        counter.SessionCompleted -=
            HandleCheckoutCompleted;

        counter.SessionCompleted +=
            HandleCheckoutCompleted;
    }

    private void HandleCheckoutCompleted(
        CheckoutSession session)
    {
        Data.CustomersServed++;
        StatisticsChanged?.Invoke();
    }

    public void Restore(StoreStatisticsData data)
    {
        Data = data ?? new StoreStatisticsData();
        StatisticsChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (GameBootstrap.Instance == null)
        {
            return;
        }

        GameBootstrap.Instance.Economy.EntryRecorded -=
            HandleLedgerEntry;

        GameBootstrap.Instance.Customers.CustomerRegistered -=
            HandleCustomerRegistered;

        GameBootstrap.Instance.Checkouts.CounterRegistered -=
            HandleCounterRegistered;

        IReadOnlyList<CheckoutCounter> counters =
            GameBootstrap.Instance.Checkouts.Counters;

        for (int i = 0; i < counters.Count; i++)
        {
            if (counters[i] != null)
            {
                counters[i].SessionCompleted -=
                    HandleCheckoutCompleted;
            }
        }
    }
}
