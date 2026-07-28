using System.Collections.Generic;
using TMPro;

using UnityEngine;

[System.Serializable]
public class PlacementPointGroup
{
    public StockCategory Category;
    public List<Transform> PlacementPoints = new List<Transform>();
}

public class ShelfSpaceController : MonoBehaviour
{
    public enum PlacementMode
    {
        SmartPlacement,
        PlacementPoints
    }

    //[Header("Shelf Information")]
    public StockInfo Info;

    //[Header("Shelf Label")]
    public TMP_Text ShelfLabel;
    public string CurrencySymbol = "$";

    //[Header("Objects")]
    public List<StockObject> ObjectsOnShelf = new List<StockObject>();

    //[Header("Placement Mode")]
    public PlacementMode CurrentPlacementMode = PlacementMode.SmartPlacement;

    //[Header("Smart Placement")]
    public Vector3 FirstObjectLocalPosition = new Vector3(-0.525f,0f,0.4f);
    public float ObjectSpacingX = 0.175f;
    public int ObjectsPerRow = 7;
    public int NumberOfRows = 2;
    public Vector3 RowSpacing = new Vector3(0f,0f,-0.215f);
    public Vector3 ObjectLocalRotation = Vector3.zero;

    //[Header("Placement Point Groups")]
    public List<PlacementPointGroup> PlacementGroups = new List<PlacementPointGroup>();

    private void Awake()
    {
        if (ObjectsOnShelf == null)
        {
            ObjectsOnShelf = new List<StockObject>();
        }

        if (PlacementGroups == null)
        {
            PlacementGroups = new List<PlacementPointGroup>();
        }

        RemoveMissingObjects();

        if (ObjectsOnShelf.Count > 0 && ObjectsOnShelf[0] != null)
        {
            Info = ObjectsOnShelf[0].Info;
        }

        UpdateObjectPositions();
        UpdateShelfLabel();
    }

    public bool PlaceStock(StockObject objectToPlace)
    {
        if (objectToPlace == null)
        {
            return false;
        }

        if (objectToPlace.Info == null)
        {
            Debug.LogWarning(objectToPlace.name + " does not have a StockInfo asset assigned.",objectToPlace);
            return false;
        }

        if (objectToPlace.Info.Category == null)
        {
            Debug.LogWarning(objectToPlace.Info.name + " does not have a StockCategory assigned.",objectToPlace.Info);
            return false;
        }

        RemoveMissingObjects();

        if (ObjectsOnShelf.Count > 0 && !CanPlaceStock(objectToPlace))
        {
            return false;
        }

        int objectIndex = ObjectsOnShelf.Count;
        int maximumObjects = GetMaximumObjects(objectToPlace.Info.Category);

        if (maximumObjects <= 0)
        {
            Debug.LogWarning("This shelf has no available placement positions for " + objectToPlace.Info.Category.CategoryName + ".",this);
            return false;
        }

        if (objectIndex >= maximumObjects)
        {
            return false;
        }

        if (!TryGetPlacement(objectToPlace.Info.Category,objectIndex,out Vector3 targetPosition,out Quaternion targetRotation))
        {
            return false;
        }

        if (ObjectsOnShelf.Count == 0)
        {
            Info = objectToPlace.Info;
        }

        objectToPlace.transform.SetParent(transform,true);
        ObjectsOnShelf.Add(objectToPlace);
        objectToPlace.MakePlaced(targetPosition,targetRotation);

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

        int lastIndex = ObjectsOnShelf.Count - 1;
        StockObject objectToReturn = ObjectsOnShelf[lastIndex];

        ObjectsOnShelf.RemoveAt(lastIndex);

        if (ObjectsOnShelf.Count == 0)
        {
            Info = null;
        }

        UpdateShelfLabel();

        return objectToReturn;
    }

    private bool CanPlaceStock(StockObject objectToPlace)
    {
        if (Info == null || objectToPlace.Info == null)
        {
            return false;
        }

        return Info == objectToPlace.Info;
    }

    private int GetMaximumObjects(StockCategory category)
    {
        if (CurrentPlacementMode == PlacementMode.SmartPlacement)
        {
            return ObjectsPerRow * NumberOfRows;
        }

        PlacementPointGroup group = GetPlacementGroup(category);

        if (group == null || group.PlacementPoints == null)
        {
            return 0;
        }

        return group.PlacementPoints.Count;
    }

    private bool TryGetPlacement(StockCategory category,int objectIndex,out Vector3 targetPosition,out Quaternion targetRotation)
    {
        targetPosition = Vector3.zero;
        targetRotation = Quaternion.identity;

        if (CurrentPlacementMode == PlacementMode.SmartPlacement)
        {
            if (ObjectsPerRow <= 0 || NumberOfRows <= 0)
            {
                return false;
            }

            int column = objectIndex % ObjectsPerRow;
            int row = objectIndex / ObjectsPerRow;

            if (row >= NumberOfRows)
            {
                return false;
            }

            targetPosition = FirstObjectLocalPosition;
            targetPosition.x += ObjectSpacingX * column;
            targetPosition += RowSpacing * row;
            targetRotation = Quaternion.Euler(ObjectLocalRotation);

            return true;
        }

        PlacementPointGroup group = GetPlacementGroup(category);

        if (group == null || group.PlacementPoints == null)
        {
            return false;
        }

        if (objectIndex < 0 || objectIndex >= group.PlacementPoints.Count)
        {
            return false;
        }

        Transform placementPoint = group.PlacementPoints[objectIndex];

        if (placementPoint == null)
        {
            return false;
        }

        targetPosition = transform.InverseTransformPoint(placementPoint.position);
        targetRotation = Quaternion.Inverse(transform.rotation) * placementPoint.rotation;

        return true;
    }

    private PlacementPointGroup GetPlacementGroup(StockCategory category)
    {
        if (category == null)
        {
            return null;
        }

        for (int i = 0; i < PlacementGroups.Count; i++)
        {
            PlacementPointGroup group = PlacementGroups[i];

            if (group != null && group.Category == category)
            {
                return group;
            }
        }

        return null;
    }

    private void RemoveMissingObjects()
    {
        ObjectsOnShelf.RemoveAll(stockObject => stockObject == null);

        if (ObjectsOnShelf.Count == 0)
        {
            Info = null;
        }
    }

    private void UpdateObjectPositions()
    {
        for (int i = 0; i < ObjectsOnShelf.Count; i++)
        {
            StockObject stockObject = ObjectsOnShelf[i];

            if (stockObject == null || stockObject.Info == null || stockObject.Info.Category == null)
            {
                continue;
            }

            if (!TryGetPlacement(stockObject.Info.Category,i,out Vector3 targetPosition,out Quaternion targetRotation))
            {
                continue;
            }

            stockObject.transform.SetParent(transform,true);
            stockObject.MakePlaced(targetPosition,targetRotation);
        }
    }

    private void UpdateShelfLabel()
    {
        if (ShelfLabel == null)
        {
            return;
        }

        if (ObjectsOnShelf.Count == 0 || Info == null)
        {
            ShelfLabel.text = string.Empty;
            return;
        }

        ShelfLabel.text = CurrencySymbol + Info.Price.ToString("0.00");
    }

    private void OnValidate()
    {
        if (ObjectsPerRow < 1)
        {
            ObjectsPerRow = 1;
        }

        if (NumberOfRows < 1)
        {
            NumberOfRows = 1;
        }
    }
}