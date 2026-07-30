using System.Collections.Generic;
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
    public StockInfo Info;

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

    public bool HasStock =>
        StockCount > 0 &&
        Info != null;

    private void Awake()
    {
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
            return ObjectsOnShelf.Count > 0 &&
                   !context.Player
                       .IsHoldingAnything;
        }

        if (context.Type ==
            InteractionType.Use)
        {
            return ObjectsOnShelf.Count > 0 &&
                   Info != null;
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
            return "[Right Click] Take Stock";
        }

        if (interactionType ==
            InteractionType.Use)
        {
            return "[E] Change Price";
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

        return true;
    }

    public StockObject GetStock()
    {
        RemoveMissingObjects();

        if (ObjectsOnShelf.Count == 0)
        {
            Info = null;
            UpdateShelfLabel();
            return null;
        }

        int lastIndex =
            ObjectsOnShelf.Count - 1;

        StockObject objectToReturn =
            ObjectsOnShelf[lastIndex];

        ObjectsOnShelf.RemoveAt(lastIndex);

        if (ObjectsOnShelf.Count == 0)
        {
            Info = null;
        }

        UpdateShelfLabel();

        return objectToReturn;
    }

    public void StartPriceUpdate()
    {
        RemoveMissingObjects();

        if (ObjectsOnShelf.Count == 0 ||
            Info == null)
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

        Info.CurrentPrice =
            Mathf.Max(0f,newPrice);

        UpdateShelfLabel();
    }

    public void UpdateShelfLabel()
    {
        if (ShelfLabel == null)
        {
            return;
        }

        RemoveMissingObjects();

        if (ObjectsOnShelf.Count == 0 ||
            Info == null)
        {
            ShelfLabel.text = string.Empty;
            return;
        }

        ShelfLabel.text =
            CurrencySymbol +
            Info.CurrentPrice.ToString("0.00");
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

        if (ObjectsOnShelf.Count == 0)
        {
            Info = null;
        }
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
    }
}
