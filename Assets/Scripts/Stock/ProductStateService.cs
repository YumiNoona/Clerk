using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ProductStateService : MonoBehaviour
{
    private readonly Dictionary<string,ProductState>
        statesById =
            new Dictionary<string,ProductState>();

    public IReadOnlyCollection<ProductState> States =>
        statesById.Values;

    public event Action<ProductState> ProductPriceChanged;

    public ProductState GetOrCreate(StockInfo product)
    {
        if (product == null ||
            string.IsNullOrWhiteSpace(product.ProductId))
        {
            return null;
        }

        if (statesById.TryGetValue(
                product.ProductId,
                out ProductState existing))
        {
            return existing;
        }

        ProductState created =
            new ProductState(
                product,
                product.InitialPrice);

        created.PriceChanged += HandlePriceChanged;

        statesById.Add(
            product.ProductId,
            created);

        return created;
    }

    public bool TryGet(
        string productId,
        out ProductState state)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            state = null;
            return false;
        }

        return statesById.TryGetValue(
            productId,
            out state);
    }

    public float GetPrice(StockInfo product)
    {
        ProductState state = GetOrCreate(product);

        return state != null
            ? state.CurrentPrice
            : 0f;
    }

    public bool TrySetPrice(
        StockInfo product,
        float price)
    {
        ProductState state = GetOrCreate(product);
        return state != null && state.SetPrice(price);
    }

    public void ClearRuntimeState()
    {
        foreach (ProductState state in
                 statesById.Values)
        {
            state.PriceChanged -= HandlePriceChanged;
        }

        statesById.Clear();
    }

    private void HandlePriceChanged(ProductState state)
    {
        ProductPriceChanged?.Invoke(state);
    }

    private void OnDestroy()
    {
        ClearRuntimeState();
    }
}
