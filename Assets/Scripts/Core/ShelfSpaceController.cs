using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;

[System.Serializable]
public class PlacementPointGroup
{
    public StockCategory Category;

    public List<Transform> PlacementPoints =
        new List<Transform>();
}

public class ShelfSpaceController :
    InteractableBehaviour
{
    public enum PlacementMode
    {
        SmartPlacement,
        PlacementPoints
    }

    [Header("Shelf Information")]
    [SerializeField,HideInInspector]
    private string shelfId;

    public StockInfo Info;

    [Tooltip(
        "Keep the product assigned when the last item is taken, " +
        "allowing customers and UI to report an out-of-stock shelf.")]
    public bool KeepProductAssignmentWhenEmpty = true;

    [Header("Customer Access")]
    [Tooltip(
        "Optional position where customers stand while browsing this shelf.")]
    public Transform CustomerStandingPoint;

    public float CustomerStoppingDistance = 0.35f;

    [Header("Shelf Label")]
    public TMP_Text ShelfLabel;
    public string CurrencySymbol = "$";

    [Header("Objects")]
    public List<StockObject> ObjectsOnShelf =
        new List<StockObject>();

    [Header("Placement Mode")]
    public PlacementMode CurrentPlacementMode =
        PlacementMode.SmartPlacement;

    [Header("Smart Placement")]
    public Vector3 FirstObjectLocalPosition =
        new Vector3(-0.525f,0f,0.4f);

    public float ObjectSpacingX = 0.175f;
    public int ObjectsPerRow = 7;
    public int NumberOfRows = 2;

    public Vector3 RowSpacing =
        new Vector3(0f,0f,-0.215f);

    public Vector3 ObjectLocalRotation =
        Vector3.zero;

    [Header("Placement Point Groups")]
    public List<PlacementPointGroup>
        PlacementGroups =
            new List<PlacementPointGroup>();

    public int StockCount
    {
        get
        {
            RemoveMissingObjects();
            return ObjectsOnShelf.Count;
        }
    }

    public int ReservedStockCount
    {
        get
        {
            ReconcileReservations();
            return reservedStockCount;
        }
    }

    public int AvailableStockCount =>
        Mathf.Max(
            0,
            StockCount - ReservedStockCount);

    public bool HasStock =>
        StockCount > 0 &&
        Info != null;

    public bool HasAvailableStock =>
        AvailableStockCount > 0 &&
        Info != null;

    public bool IsOutOfStock =>
        Info != null &&
        StockCount == 0;

    public Vector3 CustomerStandingPosition =>
        CustomerStandingPoint != null
            ? CustomerStandingPoint.position
            : transform.position -
              transform.forward;

    public Quaternion CustomerStandingRotation =>
        CustomerStandingPoint != null
            ? CustomerStandingPoint.rotation
            : Quaternion.LookRotation(
                transform.position -
                CustomerStandingPosition,
                Vector3.up);

    public event Action<ShelfSpaceController>
        InventoryChanged;

    public event Action<ShelfSpaceController>
        OutOfStock;

    private readonly Dictionary<string,ShelfReservation>
        reservations =
            new Dictionary<string,ShelfReservation>();

    private int reservedStockCount;

    public string ShelfId => shelfId;

    public float CurrentPrice
    {
        get
        {
            if (Info == null)
            {
                return 0f;
            }

            return GameBootstrap.Instance != null
                ? GameBootstrap.Instance.Products
                    .GetPrice(Info)
                : Info.InitialPrice;
        }
    }

    private void OnEnable()
    {
        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Products
                .ProductPriceChanged +=
                    HandleProductPriceChanged;

            GameBootstrap.Instance.Shelves
                .Register(this);
        }
    }

    private void OnDisable()
    {
        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Products
                .ProductPriceChanged -=
                    HandleProductPriceChanged;

            GameBootstrap.Instance.Shelves
                .Unregister(this);
        }

        ReleaseAllReservations();
    }

    private void Awake()
    {
        EnsureId();

        if (ObjectsOnShelf == null)
        {
            ObjectsOnShelf =
                new List<StockObject>();
        }

        if (PlacementGroups == null)
        {
            PlacementGroups =
                new List<PlacementPointGroup>();
        }

        RemoveMissingObjects();

        if (ObjectsOnShelf.Count > 0 &&
            ObjectsOnShelf[0] != null)
        {
            Info = ObjectsOnShelf[0].Info;
        }

        UpdateObjectPositions();
        UpdateShelfLabel();
    }

    protected override int
        GetDefaultInteractionPriority()
    {
        return 40;
    }

    protected override bool SupportsInteraction(
        InteractionType interactionType)
    {
        return interactionType ==
                   InteractionType.Secondary ||
               interactionType ==
                   InteractionType.Use;
    }

    protected override bool CanInteractInternal(
        InteractionContext context)
    {
        RemoveMissingObjects();

        if (context.Type ==
            InteractionType.Secondary)
        {
            return AvailableStockCount > 0 &&
                   !context.Player
                       .IsHoldingAnything;
        }

        if (context.Type ==
            InteractionType.Use)
        {
            return Info != null;
        }

        return false;
    }

    protected override void OnInteract(
        InteractionContext context)
    {
        if (context.Type ==
            InteractionType.Secondary)
        {
            TakeStock(context.Player);
            return;
        }

        if (context.Type ==
            InteractionType.Use)
        {
            StartPriceUpdate();
        }
    }

    protected override string
        GetDefaultInteractionPrompt(
            InteractionType interactionType)
    {
        if (interactionType ==
            InteractionType.Secondary)
        {
            return GameBootstrap.Instance != null
                ? GameBootstrap.Instance.Input
                    .FormatPrompt(
                        GameplayAction.Secondary,
                        "Take Stock")
                : "[Right Click] Take Stock";
        }

        if (interactionType ==
            InteractionType.Use)
        {
            return GameBootstrap.Instance != null
                ? GameBootstrap.Instance.Input
                    .FormatPrompt(
                        GameplayAction.Use,
                        "Change Price")
                : "[E] Change Price";
        }

        return string.Empty;
    }

    private void TakeStock(
        PlayerInteractionController player)
    {
        StockObject stockObject = GetStock();

        if (stockObject == null)
        {
            return;
        }

        stockObject.PrepareForShelfPickup();

        if (player.TryHold(stockObject))
        {
            return;
        }

        PlaceStock(stockObject);
    }

    public bool CanAcceptStock(
        StockObject stockObject)
    {
        return stockObject != null &&
               CanAcceptProduct(
                   stockObject.Info);
    }

    public bool CanAcceptProduct(
        StockInfo product)
    {
        if (product == null ||
            product.Category == null)
        {
            return false;
        }

        RemoveMissingObjects();

        if (ObjectsOnShelf.Count > 0 &&
            Info != product)
        {
            return false;
        }

        int maximumObjects =
            GetMaximumObjects(
                product.Category);

        return maximumObjects > 0 &&
               ObjectsOnShelf.Count <
               maximumObjects;
    }

    public bool PlaceStock(
        StockObject objectToPlace)
    {
        if (objectToPlace == null)
        {
            return false;
        }

        if (objectToPlace.Info == null)
        {
            Debug.LogWarning(
                objectToPlace.name +
                " does not have a StockInfo asset assigned.",
                objectToPlace);

            return false;
        }

        if (objectToPlace.Info.Category == null)
        {
            Debug.LogWarning(
                objectToPlace.Info.name +
                " does not have a StockCategory assigned.",
                objectToPlace.Info);

            return false;
        }

        if (!CanAcceptStock(objectToPlace))
        {
            return false;
        }

        int objectIndex =
            ObjectsOnShelf.Count;

        if (!TryGetPlacement(
                objectToPlace.Info.Category,
                objectIndex,
                out Vector3 targetPosition,
                out Quaternion targetRotation))
        {
            return false;
        }

        if (ObjectsOnShelf.Count == 0)
        {
            Info = objectToPlace.Info;
        }

        objectToPlace.transform.SetParent(
            transform,
            true);

        ObjectsOnShelf.Add(objectToPlace);

        objectToPlace.MakePlaced(
            targetPosition,
            targetRotation);

        UpdateShelfLabel();
        NotifyInventoryChanged();

        return true;
    }

    public StockObject GetStock()
    {
        RemoveMissingObjects();

        if (AvailableStockCount == 0)
        {
            UpdateShelfLabel();
            return null;
        }

        int lastIndex =
            ObjectsOnShelf.Count - 1;

        StockObject objectToReturn =
            ObjectsOnShelf[lastIndex];

        ObjectsOnShelf.RemoveAt(lastIndex);

        if (ObjectsOnShelf.Count == 0 &&
            !KeepProductAssignmentWhenEmpty)
        {
            Info = null;
        }

        UpdateShelfLabel();
        NotifyInventoryChanged();

        return objectToReturn;
    }

    public bool HasAvailableProduct(StockInfo product)
    {
        return product != null &&
               Info == product &&
               AvailableStockCount > 0;
    }

    public bool TryReserve(
        string ownerId,
        StockInfo product,
        int quantity,
        out ShelfReservation reservation)
    {
        reservation = null;

        if (string.IsNullOrWhiteSpace(ownerId) ||
            product == null ||
            Info != product ||
            quantity <= 0 ||
            AvailableStockCount < quantity)
        {
            return false;
        }

        reservation =
            new ShelfReservation(
                ownerId,
                this,
                product,
                quantity);

        reservations.Add(
            reservation.ReservationId,
            reservation);

        reservedStockCount += quantity;
        InventoryChanged?.Invoke(this);
        return true;
    }

    public bool ReleaseReservation(
        ShelfReservation reservation)
    {
        if (!OwnsReservation(reservation))
        {
            return false;
        }

        reservations.Remove(
            reservation.ReservationId);

        reservedStockCount =
            Mathf.Max(
                0,
                reservedStockCount -
                reservation.RemainingQuantity);

        reservation.RemainingQuantity = 0;
        InventoryChanged?.Invoke(this);
        return true;
    }

    public bool TryTakeReservedStock(
        ShelfReservation reservation,
        out StockObject stockObject)
    {
        stockObject = null;

        if (!OwnsReservation(reservation) ||
            reservation.RemainingQuantity <= 0 ||
            ObjectsOnShelf.Count == 0)
        {
            return false;
        }

        int lastIndex = ObjectsOnShelf.Count - 1;
        stockObject = ObjectsOnShelf[lastIndex];
        ObjectsOnShelf.RemoveAt(lastIndex);

        reservation.RemainingQuantity--;
        reservedStockCount =
            Mathf.Max(0,reservedStockCount - 1);

        if (reservation.RemainingQuantity == 0)
        {
            reservations.Remove(
                reservation.ReservationId);
        }

        if (stockObject != null)
        {
            stockObject.PrepareForShelfPickup();
        }

        if (ObjectsOnShelf.Count == 0 &&
            !KeepProductAssignmentWhenEmpty)
        {
            Info = null;
        }

        UpdateShelfLabel();
        NotifyInventoryChanged();
        return stockObject != null;
    }

    public bool TryClearProductAssignment()
    {
        if (StockCount > 0 ||
            ReservedStockCount > 0)
        {
            return false;
        }

        Info = null;
        UpdateShelfLabel();
        InventoryChanged?.Invoke(this);
        return true;
    }

    public void RestoreInventory(
        StockInfo product,
        int quantity)
    {
        ReleaseAllReservations();

        for (int i = ObjectsOnShelf.Count - 1;
             i >= 0;
             i--)
        {
            if (ObjectsOnShelf[i] != null)
            {
                Destroy(
                    ObjectsOnShelf[i].gameObject);
            }
        }

        ObjectsOnShelf.Clear();
        Info = product;

        if (product != null &&
            product.StockPrefab != null)
        {
            int safeQuantity =
                Mathf.Max(0,quantity);

            for (int i = 0;
                 i < safeQuantity;
                 i++)
            {
                StockObject stock =
                    Instantiate(
                        product.StockPrefab,
                        transform);

                stock.Info = product;

                if (!PlaceStock(stock))
                {
                    Destroy(stock.gameObject);
                    break;
                }
            }
        }

        UpdateShelfLabel();
        NotifyInventoryChanged();
    }

    public void StartPriceUpdate()
    {
        RemoveMissingObjects();

        if (Info == null)
        {
            return;
        }

        if (UIController.Instance == null)
        {
            Debug.LogWarning(
                "UIController was not found in the scene.",
                this);

            return;
        }

        UIController.Instance.OpenUpdatePrice(
            this);
    }

    public void SetCurrentPrice(
        float newPrice)
    {
        if (Info == null)
        {
            return;
        }

        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Products
                .TrySetPrice(Info,newPrice);
        }

        UpdateShelfLabel();
    }

    public void UpdateShelfLabel()
    {
        if (ShelfLabel == null)
        {
            return;
        }

        RemoveMissingObjects();

        if (Info == null)
        {
            ShelfLabel.text = string.Empty;
            return;
        }

        ShelfLabel.text =
            CurrencySymbol +
            CurrentPrice.ToString("0.00");
    }

    private void HandleProductPriceChanged(
        ProductState state)
    {
        if (Info != null &&
            state != null &&
            state.ProductId == Info.ProductId)
        {
            UpdateShelfLabel();
        }
    }

    private int GetMaximumObjects(
        StockCategory category)
    {
        if (CurrentPlacementMode ==
            PlacementMode.SmartPlacement)
        {
            return ObjectsPerRow *
                   NumberOfRows;
        }

        PlacementPointGroup group =
            GetPlacementGroup(category);

        if (group == null ||
            group.PlacementPoints == null)
        {
            return 0;
        }

        return group.PlacementPoints.Count;
    }

    private bool TryGetPlacement(
        StockCategory category,
        int objectIndex,
        out Vector3 targetPosition,
        out Quaternion targetRotation)
    {
        targetPosition = Vector3.zero;
        targetRotation =
            Quaternion.identity;

        if (CurrentPlacementMode ==
            PlacementMode.SmartPlacement)
        {
            if (ObjectsPerRow <= 0 ||
                NumberOfRows <= 0)
            {
                return false;
            }

            int column =
                objectIndex % ObjectsPerRow;

            int row =
                objectIndex / ObjectsPerRow;

            if (row >= NumberOfRows)
            {
                return false;
            }

            targetPosition =
                FirstObjectLocalPosition;

            targetPosition.x +=
                ObjectSpacingX * column;

            targetPosition +=
                RowSpacing * row;

            targetRotation =
                Quaternion.Euler(
                    ObjectLocalRotation);

            return true;
        }

        PlacementPointGroup group =
            GetPlacementGroup(category);

        if (group == null ||
            group.PlacementPoints == null)
        {
            return false;
        }

        if (objectIndex < 0 ||
            objectIndex >=
            group.PlacementPoints.Count)
        {
            return false;
        }

        Transform placementPoint =
            group.PlacementPoints[
                objectIndex];

        if (placementPoint == null)
        {
            return false;
        }

        targetPosition =
            transform.InverseTransformPoint(
                placementPoint.position);

        targetRotation =
            Quaternion.Inverse(
                transform.rotation) *
            placementPoint.rotation;

        return true;
    }

    private PlacementPointGroup
        GetPlacementGroup(
            StockCategory category)
    {
        if (category == null)
        {
            return null;
        }

        for (int i = 0;
             i < PlacementGroups.Count;
             i++)
        {
            PlacementPointGroup group =
                PlacementGroups[i];

            if (group != null &&
                group.Category == category)
            {
                return group;
            }
        }

        return null;
    }

    private void RemoveMissingObjects()
    {
        ObjectsOnShelf.RemoveAll(
            stockObject =>
                stockObject == null);

        if (ObjectsOnShelf.Count == 0 &&
            !KeepProductAssignmentWhenEmpty)
        {
            Info = null;
        }

        ReconcileReservations();
    }

    private bool OwnsReservation(
        ShelfReservation reservation)
    {
        return reservation != null &&
               reservation.Shelf == this &&
               reservations.TryGetValue(
                   reservation.ReservationId,
                   out ShelfReservation existing) &&
               ReferenceEquals(existing,reservation);
    }

    private void ReconcileReservations()
    {
        if (reservedStockCount <= ObjectsOnShelf.Count)
        {
            return;
        }

        int excess =
            reservedStockCount -
            ObjectsOnShelf.Count;

        List<ShelfReservation> active =
            new List<ShelfReservation>(
                reservations.Values);

        for (int i = active.Count - 1;
             i >= 0 && excess > 0;
             i--)
        {
            ShelfReservation reservation = active[i];
            int reduction = Mathf.Min(
                reservation.RemainingQuantity,
                excess);

            reservation.RemainingQuantity -= reduction;
            reservedStockCount -= reduction;
            excess -= reduction;

            if (reservation.RemainingQuantity == 0)
            {
                reservations.Remove(
                    reservation.ReservationId);
            }
        }
    }

    private void ReleaseAllReservations()
    {
        foreach (ShelfReservation reservation in
                 reservations.Values)
        {
            reservation.RemainingQuantity = 0;
        }

        reservations.Clear();
        reservedStockCount = 0;
    }

    private void NotifyInventoryChanged()
    {
        InventoryChanged?.Invoke(this);

        if (IsOutOfStock)
        {
            OutOfStock?.Invoke(this);
        }
    }

    private void EnsureId()
    {
        if (string.IsNullOrWhiteSpace(shelfId))
        {
            shelfId =
                Guid.NewGuid().ToString("N");
        }
    }

    public void RegeneratePersistentId()
    {
        shelfId =
            Guid.NewGuid().ToString("N");
    }

    private void UpdateObjectPositions()
    {
        for (int i = 0;
             i < ObjectsOnShelf.Count;
             i++)
        {
            StockObject stockObject =
                ObjectsOnShelf[i];

            if (stockObject == null ||
                stockObject.Info == null ||
                stockObject.Info.Category ==
                null)
            {
                continue;
            }

            if (!TryGetPlacement(
                    stockObject.Info.Category,
                    i,
                    out Vector3 targetPosition,
                    out Quaternion targetRotation))
            {
                continue;
            }

            stockObject.transform.SetParent(
                transform,
                true);

            stockObject.MakePlaced(
                targetPosition,
                targetRotation);
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        ObjectsPerRow =
            Mathf.Max(1,ObjectsPerRow);

        NumberOfRows =
            Mathf.Max(1,NumberOfRows);

        CustomerStoppingDistance =
            Mathf.Max(0f,CustomerStoppingDistance);

        EnsureId();
    }
}
