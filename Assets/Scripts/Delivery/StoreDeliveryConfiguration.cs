using UnityEngine;

[DisallowMultipleComponent]
public sealed class StoreDeliveryConfiguration : MonoBehaviour
{
    [Header("Delivery Positions")]
    [SerializeField] private ScenePose stockDelivery;
    [SerializeField] private ScenePose furnitureDelivery;

    private GameObject runtimePoints;

    private void Awake()
    {
        Apply();
    }

    public void Apply()
    {
        ClearRuntimePoints();
        runtimePoints = new GameObject("Runtime Delivery Points");
        runtimePoints.hideFlags = HideFlags.HideInHierarchy;

        Transform stockPoint = CreatePoint("Stock Delivery",stockDelivery);
        Transform furniturePoint = CreatePoint("Furniture Delivery",furnitureDelivery);

        StockDeliveryService stockService = FindAnyObjectByType<StockDeliveryService>();
        if (stockService != null)
        {
            stockService.DeliverySpawnPoint = stockPoint;
        }

        FurnitureDeliveryService furnitureService = FindAnyObjectByType<FurnitureDeliveryService>();
        if (furnitureService != null)
        {
            furnitureService.FurnitureSpawnPoint = furniturePoint;
        }
    }

    private Transform CreatePoint(string pointName, ScenePose pose)
    {
        GameObject point = new GameObject(pointName);
        point.hideFlags = HideFlags.None;
        point.transform.SetParent(runtimePoints.transform,false);
        point.transform.SetPositionAndRotation(
            transform.TransformPoint(pose.Position),
            transform.rotation * pose.Rotation);
        return point.transform;
    }

    private void ClearRuntimePoints()
    {
        if (runtimePoints == null)
        {
            return;
        }

        runtimePoints.SetActive(false);

        if (Application.isPlaying)
        {
            Destroy(runtimePoints);
        }
        else
        {
            DestroyImmediate(runtimePoints);
        }

        runtimePoints = null;
    }

    private void OnDestroy()
    {
        ClearRuntimePoints();
    }
}
