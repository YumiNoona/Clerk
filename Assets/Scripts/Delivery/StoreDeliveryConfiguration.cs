using UnityEngine;

[DisallowMultipleComponent]
public sealed class StoreDeliveryConfiguration : MonoBehaviour
{
    [Header("Delivery Positions")]
    [SerializeField] private ScenePose stockDelivery;
    [SerializeField] private ScenePose furnitureDelivery;

    private void Awake()
    {
        Apply();
    }

    public void Apply()
    {
        Vector3 stockPosition = transform.TransformPoint(stockDelivery.Position);
        Quaternion stockRotation = transform.rotation * stockDelivery.Rotation;
        Vector3 furniturePosition = transform.TransformPoint(furnitureDelivery.Position);
        Quaternion furnitureRotation = transform.rotation * furnitureDelivery.Rotation;

        StockDeliveryService stockService = FindAnyObjectByType<StockDeliveryService>();
        if (stockService != null)
        {
            stockService.ConfigureSpawnPose(stockPosition,stockRotation);
        }

        FurnitureDeliveryService furnitureService = FindAnyObjectByType<FurnitureDeliveryService>();
        if (furnitureService != null)
        {
            furnitureService.ConfigureSpawnPose(
                furniturePosition,furnitureRotation);
        }
    }
}
