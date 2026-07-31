using System.Collections.Generic;
using UnityEngine;

public sealed class CustomerShoppingRequest
{
    public StockInfo Product { get; }
    public int DesiredQuantity { get; }
    public int CollectedQuantity { get; private set; }
    public int MissingQuantity =>
        Mathf.Max(
            0,
            DesiredQuantity - CollectedQuantity);

    public bool IsComplete =>
        CollectedQuantity >= DesiredQuantity;

    public CustomerShoppingRequest(
        StockInfo product,
        int desiredQuantity)
    {
        Product = product;
        DesiredQuantity =
            Mathf.Max(1,desiredQuantity);
    }

    public void RecordCollectedItem()
    {
        CollectedQuantity =
            Mathf.Min(
                DesiredQuantity,
                CollectedQuantity + 1);
    }
}

public sealed class CustomerShoppingPlan
{
    private readonly List<CustomerShoppingRequest>
        requests =
            new List<CustomerShoppingRequest>();

    public IReadOnlyList<CustomerShoppingRequest>
        Requests => requests;

    public bool IsComplete
    {
        get
        {
            for (int i = 0;
                 i < requests.Count;
                 i++)
            {
                if (!requests[i].IsComplete)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public void Add(
        StockInfo product,
        int quantity)
    {
        if (product == null || quantity <= 0)
        {
            return;
        }

        requests.Add(
            new CustomerShoppingRequest(
                product,
                quantity));
    }
}

public static class CustomerShoppingPlanner
{
    public static CustomerShoppingPlan Create(
        IReadOnlyList<StockInfo> availableProducts,
        CustomerDefinition definition)
    {
        CustomerShoppingPlan plan =
            new CustomerShoppingPlan();

        if (availableProducts == null ||
            availableProducts.Count == 0 ||
            definition == null)
        {
            return plan;
        }

        List<StockInfo> candidates =
            new List<StockInfo>();

        for (int i = 0;
             i < availableProducts.Count;
             i++)
        {
            StockInfo product = availableProducts[i];

            if (product != null &&
                !candidates.Contains(product))
            {
                candidates.Add(product);
            }
        }

        int requestCount = Mathf.Min(
            definition.GetRandomShoppingLineCount(),
            candidates.Count);

        for (int i = 0;
             i < requestCount;
             i++)
        {
            int selectedIndex =
                SelectWeightedIndex(candidates,i);

            StockInfo selected =
                candidates[selectedIndex];

            candidates[selectedIndex] =
                candidates[i];

            candidates[i] = selected;

            plan.Add(
                selected,
                definition.GetRandomDesiredQuantity());
        }

        return plan;
    }

    private static int SelectWeightedIndex(
        List<StockInfo> candidates,
        int startIndex)
    {
        ProductDemandService demand =
            GameBootstrap.Instance != null
                ? GameBootstrap.Instance.Demand
                : null;

        if (demand == null)
        {
            return Random.Range(
                startIndex,
                candidates.Count);
        }

        float total = 0f;

        for (int i = startIndex;
             i < candidates.Count;
             i++)
        {
            total += Mathf.Max(
                0.01f,
                demand.GetDemandScore(candidates[i]));
        }

        float roll = Random.value * total;

        for (int i = startIndex;
             i < candidates.Count;
             i++)
        {
            roll -= Mathf.Max(
                0.01f,
                demand.GetDemandScore(candidates[i]));

            if (roll <= 0f)
            {
                return i;
            }
        }

        return candidates.Count - 1;
    }
}
