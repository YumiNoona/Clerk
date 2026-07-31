using System;

public sealed class ProductState
{
    public StockInfo Definition { get; }
    public string ProductId => Definition.ProductId;
    public float CurrentPrice { get; private set; }

    public event Action<ProductState> PriceChanged;

    public ProductState(
        StockInfo definition,
        float currentPrice)
    {
        Definition = definition ??
            throw new ArgumentNullException(
                nameof(definition));

        CurrentPrice = Math.Max(0f,currentPrice);
    }

    public bool SetPrice(float price)
    {
        float safePrice = Math.Max(0f,price);

        if (Math.Abs(CurrentPrice - safePrice) <
            0.0001f)
        {
            return false;
        }

        CurrentPrice = safePrice;
        PriceChanged?.Invoke(this);
        return true;
    }
}
