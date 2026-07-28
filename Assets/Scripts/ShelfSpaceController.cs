using System.Collections.Generic;
using UnityEngine;

public class ShelfSpaceController : MonoBehaviour
{
    public enum PlacementMode
    {
        SmartPlacement,
        PlacementPoints
    }

    [Header("Shelf Information")]
    public StockInfo Info;

    [Header("Objects")]
    public List<StockObject> ObjectsOnShelf = new List<StockObject>();

    [Header("Placement Mode")]
    public PlacementMode CurrentPlacementMode = PlacementMode.SmartPlacement;

    [Header("Smart Placement")]
    public Vector3 FirstObjectLocalPosition = new Vector3(-0.525f,0f,0.4f);
    public float ObjectSpacingX = 0.175f;
    public int ObjectsPerRow = 7;
    public int NumberOfRows = 2;
    public Vector3 RowSpacing = new Vector3(0f,0f,-0.215f);
    public Vector3 ObjectLocalRotation = Vector3.zero;

    [Header("Placement Points")]
    public List<Transform> PlacementPoints = new List<Transform>();

    private void Awake()
    {
        if (ObjectsOnShelf == null)
        {
            ObjectsOnShelf = new List<StockObject>();
        }

        if (PlacementPoints == null)
        {
            PlacementPoints = new List<Transform>();
        }

        RemoveMissingObjects();
        UpdateObjectPositions();
    }

    public bool PlaceStock(StockObject objectToPlace)
    {
        if (objectToPlace == null)
        {
            return false;
        }

        if (objectToPlace.Info == null)
        {
            Debug.LogWarning(objectToPlace.name + " does not have StockInfo assigned.",objectToPlace);
            return false;
        }

        RemoveMissingObjects();

        int maximumObjects = GetMaximumObjects();

        if (maximumObjects <= 0)
        {
            Debug.LogWarning("This shelf has no valid placement positions.",this);
            return false;
        }

        if (ObjectsOnShelf.Count >= maximumObjects)
        {
            return false;
        }

        if (ObjectsOnShelf.Count == 0)
        {
            Info = objectToPlace.Info;
        }
        else if (!CanPlaceStock(objectToPlace))
        {
            return false;
        }

        int objectIndex = ObjectsOnShelf.Count;

        objectToPlace.transform.SetParent(transform,true);
        ObjectsOnShelf.Add(objectToPlace);

        Vector3 targetPosition;
        Quaternion targetRotation;

        if (!TryGetPlacement(objectIndex,out targetPosition,out targetRotation))
        {
            ObjectsOnShelf.Remove(objectToPlace);
            return false;
        }

        objectToPlace.MakePlaced(targetPosition,targetRotation);

        return true;
    }

    public StockObject GetStock()
    {
        RemoveMissingObjects();

        if (ObjectsOnShelf.Count == 0)
        {
            Info = null;
            return null;
        }

        int lastIndex = ObjectsOnShelf.Count - 1;
        StockObject objectToReturn = ObjectsOnShelf[lastIndex];

        ObjectsOnShelf.RemoveAt(lastIndex);

        if (ObjectsOnShelf.Count == 0)
        {
            Info = null;
        }

        return objectToReturn;
    }

    private bool CanPlaceStock(StockObject objectToPlace)
    {
        if (Info == null || objectToPlace.Info == null)
        {
            return false;
        }

        return Info.Name == objectToPlace.Info.Name;
    }

    private int GetMaximumObjects()
    {
        if (CurrentPlacementMode == PlacementMode.SmartPlacement)
        {
            return ObjectsPerRow * NumberOfRows;
        }

        if (CurrentPlacementMode == PlacementMode.PlacementPoints)
        {
            return PlacementPoints.Count;
        }

        return 0;
    }

    private bool TryGetPlacement(int objectIndex,out Vector3 targetPosition,out Quaternion targetRotation)
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

        if (CurrentPlacementMode == PlacementMode.PlacementPoints)
        {
            if (objectIndex < 0 || objectIndex >= PlacementPoints.Count)
            {
                return false;
            }

            Transform placementPoint = PlacementPoints[objectIndex];

            if (placementPoint == null)
            {
                return false;
            }

            targetPosition = transform.InverseTransformPoint(placementPoint.position);
            targetRotation = Quaternion.Inverse(transform.rotation) * placementPoint.rotation;

            return true;
        }

        return false;
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

            if (stockObject == null)
            {
                continue;
            }

            Vector3 targetPosition;
            Quaternion targetRotation;

            if (!TryGetPlacement(i,out targetPosition,out targetRotation))
            {
                continue;
            }

            stockObject.transform.SetParent(transform,true);
            stockObject.MakePlaced(targetPosition,targetRotation);
        }
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