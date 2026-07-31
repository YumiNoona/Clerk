using System;

public enum LedgerEntryType
{
    Sale,
    StockPurchase,
    FurniturePurchase,
    FurnitureSale,
    Refund,
    OperatingCost,
    Loan,
    Adjustment
}

[Serializable]
public sealed class LedgerEntry
{
    public string EntryId;
    public LedgerEntryType Type;
    public long AmountCents;
    public int Day;
    public string Description;
    public string RelatedId;
    public long TimestampUtcTicks;
    public int Quantity;

    public Money Amount => new Money(AmountCents);
}
