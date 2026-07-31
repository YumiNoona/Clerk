using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ShelfRegistry : MonoBehaviour
{
    private readonly List<ShelfSpaceController> shelves =
        new List<ShelfSpaceController>();

    public IReadOnlyList<ShelfSpaceController> Shelves =>
        shelves;

    public event Action<ShelfSpaceController> ShelfRegistered;
    public event Action<ShelfSpaceController> ShelfUnregistered;

    public void Register(ShelfSpaceController shelf)
    {
        if (shelf == null || shelves.Contains(shelf))
        {
            return;
        }

        shelves.Add(shelf);
        ShelfRegistered?.Invoke(shelf);
    }

    public void Unregister(ShelfSpaceController shelf)
    {
        if (shelf == null || !shelves.Remove(shelf))
        {
            return;
        }

        ShelfUnregistered?.Invoke(shelf);
    }

    public ShelfSpaceController FindNearestAvailableShelf(
        StockInfo product,
        Vector3 origin)
    {
        ShelfSpaceController bestShelf = null;
        float bestDistance = float.PositiveInfinity;

        for (int i = shelves.Count - 1;
             i >= 0;
             i--)
        {
            ShelfSpaceController shelf = shelves[i];

            if (shelf == null)
            {
                shelves.RemoveAt(i);
                continue;
            }

            if (!shelf.HasAvailableProduct(product))
            {
                continue;
            }

            float distance =
                (shelf.CustomerStandingPosition - origin)
                .sqrMagnitude;

            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestShelf = shelf;
        }

        return bestShelf;
    }

    public bool TryReserveNearest(
        string ownerId,
        StockInfo product,
        int quantity,
        Vector3 origin,
        out ShelfReservation reservation)
    {
        ShelfSpaceController shelf =
            FindNearestAvailableShelf(product,origin);

        if (shelf == null)
        {
            reservation = null;
            return false;
        }

        return shelf.TryReserve(
            ownerId,
            product,
            quantity,
            out reservation);
    }

    public int GetAvailableQuantity(StockInfo product)
    {
        if (product == null)
        {
            return 0;
        }

        int total = 0;

        for (int i = shelves.Count - 1;
             i >= 0;
             i--)
        {
            ShelfSpaceController shelf = shelves[i];

            if (shelf == null)
            {
                shelves.RemoveAt(i);
                continue;
            }

            if (shelf.Info == product)
            {
                total += shelf.AvailableStockCount;
            }
        }

        return total;
    }
}
