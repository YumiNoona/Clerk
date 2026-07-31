using System;

public sealed class CheckoutSession
{
    public CustomerContext Customer { get; }
    public int TotalItemCount =>
        Customer != null
            ? Customer.Basket.ItemCount
            : 0;

    public int ScannedItemCount { get; private set; }
    public bool AllItemsScanned =>
        ScannedItemCount >= TotalItemCount;

    public float Total =>
        Customer != null
            ? Customer.Basket.Total
            : 0f;

    public bool IsCompleted { get; private set; }

    public event Action<CheckoutSession> ItemScanned;
    public event Action<CheckoutSession> Completed;

    public CheckoutSession(CustomerContext customer)
    {
        Customer = customer;
    }

    public bool TryScanNext()
    {
        if (IsCompleted ||
            Customer == null ||
            AllItemsScanned)
        {
            return false;
        }

        ScannedItemCount++;
        ItemScanned?.Invoke(this);
        return true;
    }

    public bool TryComplete()
    {
        if (IsCompleted ||
            Customer == null ||
            !AllItemsScanned)
        {
            return false;
        }

        IsCompleted = true;
        Completed?.Invoke(this);
        return true;
    }
}
