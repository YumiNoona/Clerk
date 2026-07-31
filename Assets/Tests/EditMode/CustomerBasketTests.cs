using NUnit.Framework;
using UnityEngine;

public sealed class CustomerBasketTests
{
    [Test]
    public void Add_MergesIdenticalProductsAndTotalsPrice()
    {
        StockInfo product =
            ScriptableObject.CreateInstance<StockInfo>();

        CustomerBasket basket =
            new CustomerBasket();

        basket.Add(product,2,1.25f);
        basket.Add(product,1,1.25f);

        Assert.That(basket.ItemCount,Is.EqualTo(3));
        Assert.That(basket.Lines.Count,Is.EqualTo(1));
        Assert.That(basket.Total,Is.EqualTo(3.75f).Within(0.001f));

        Object.DestroyImmediate(product);
    }

    [Test]
    public void Clear_RemovesAllLines()
    {
        StockInfo product =
            ScriptableObject.CreateInstance<StockInfo>();

        CustomerBasket basket =
            new CustomerBasket();

        basket.Add(product,1,2f);
        basket.Clear();

        Assert.That(basket.ItemCount,Is.Zero);
        Assert.That(basket.Total,Is.Zero);
        Assert.That(basket.Lines,Is.Empty);

        Object.DestroyImmediate(product);
    }
}
