using NUnit.Framework;
using UnityEngine;

public sealed class CustomerShoppingPlanTests
{
    [Test]
    public void Request_CompletesAtDesiredQuantity()
    {
        StockInfo product =
            ScriptableObject.CreateInstance<StockInfo>();

        CustomerShoppingRequest request =
            new CustomerShoppingRequest(product,2);

        request.RecordCollectedItem();
        Assert.That(request.IsComplete,Is.False);
        Assert.That(request.MissingQuantity,Is.EqualTo(1));

        request.RecordCollectedItem();
        Assert.That(request.IsComplete,Is.True);
        Assert.That(request.MissingQuantity,Is.Zero);

        Object.DestroyImmediate(product);
    }
}
