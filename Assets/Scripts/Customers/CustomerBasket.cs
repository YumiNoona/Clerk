using System.Collections.Generic;

public sealed class CustomerBasketLine
{
    public StockInfo Product { get; }
    public int Quantity { get; private set; }
    public float UnitPrice { get; }
    public float Total => UnitPrice * Quantity;

    public CustomerBasketLine(
        StockInfo product,
        int quantity,
        float unitPrice)
    {
        Product = product;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    internal void Add(int quantity)
    {
        Quantity += quantity;
    }
}

public sealed class CustomerBasket
{
    private readonly List<CustomerBasketLine> lines =
        new List<CustomerBasketLine>();

    public IReadOnlyList<CustomerBasketLine> Lines =>
        lines;

    public int ItemCount { get; private set; }

    public float Total
    {
        get
        {
            float total = 0f;

            for (int i = 0; i < lines.Count; i++)
            {
                total += lines[i].Total;
            }

            return total;
        }
    }

    public void Add(
        StockInfo product,
        int quantity,
        float unitPrice)
    {
        if (product == null || quantity <= 0)
        {
            return;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            CustomerBasketLine line = lines[i];

            if (line.Product == product &&
                UnityEngine.Mathf.Approximately(
                    line.UnitPrice,
                    unitPrice))
            {
                line.Add(quantity);
                ItemCount += quantity;
                return;
            }
        }

        lines.Add(
            new CustomerBasketLine(
                product,
                quantity,
                unitPrice));

        ItemCount += quantity;
    }

    public void Clear()
    {
        lines.Clear();
        ItemCount = 0;
    }
}
