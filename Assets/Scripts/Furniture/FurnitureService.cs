using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class FurnitureService : MonoBehaviour
{
    private readonly List<PlaceableFurniture> furniture =
        new List<PlaceableFurniture>();

    private PurchaseCatalog purchaseCatalog;

    public IReadOnlyList<PlaceableFurniture> Furniture =>
        furniture;

    public event Action<PlaceableFurniture> FurnitureRegistered;
    public event Action<PlaceableFurniture> FurnitureRemoved;

    public void Configure(PurchaseCatalog catalog)
    {
        purchaseCatalog = catalog;
    }

    public void Register(PlaceableFurniture item)
    {
        if (item == null || furniture.Contains(item))
        {
            return;
        }

        furniture.Add(item);
        FurnitureRegistered?.Invoke(item);
    }

    public void Unregister(PlaceableFurniture item)
    {
        if (item == null || !furniture.Remove(item))
        {
            return;
        }

        FurnitureRemoved?.Invoke(item);
    }

    public bool TrySell(
        PlaceableFurniture item,
        float refundMultiplier = 0.5f)
    {
        if (item == null ||
            item.IsBeingPlaced ||
            string.IsNullOrWhiteSpace(item.PurchaseId) ||
            GameBootstrap.Instance == null)
        {
            return false;
        }

        FurniturePurchaseData purchase =
            FindPurchase(item.PurchaseId);

        if (purchase == null)
        {
            return false;
        }

        Money refund = Money.FromFloat(
            purchase.PurchasePrice *
            Mathf.Clamp01(refundMultiplier));

        GameBootstrap.Instance.Economy.GrantFunds(
            refund,
            LedgerEntryType.FurnitureSale,
            "Sold " + purchase.DisplayName,
            purchase.PurchaseId);

        Unregister(item);
        Destroy(item.gameObject);
        return true;
    }

    private FurniturePurchaseData FindPurchase(
        string purchaseId)
    {
        if (purchaseCatalog == null ||
            purchaseCatalog.FurniturePurchases == null)
        {
            return null;
        }

        for (int i = 0;
             i < purchaseCatalog.FurniturePurchases.Count;
             i++)
        {
            FurniturePurchaseData purchase =
                purchaseCatalog.FurniturePurchases[i];

            if (purchase != null &&
                purchase.PurchaseId == purchaseId)
            {
                return purchase;
            }
        }

        return null;
    }
}
