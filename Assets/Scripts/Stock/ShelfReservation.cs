using System;

public sealed class ShelfReservation
{
    public string ReservationId { get; }
    public string OwnerId { get; }
    public ShelfSpaceController Shelf { get; }
    public StockInfo Product { get; }
    public int RemainingQuantity { get; internal set; }
    public bool IsActive =>
        Shelf != null &&
        RemainingQuantity > 0;

    internal ShelfReservation(
        string ownerId,
        ShelfSpaceController shelf,
        StockInfo product,
        int quantity)
    {
        ReservationId =
            Guid.NewGuid().ToString("N");

        OwnerId = ownerId;
        Shelf = shelf;
        Product = product;
        RemainingQuantity = quantity;
    }
}
