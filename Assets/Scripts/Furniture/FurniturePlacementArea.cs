using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FurniturePlacementArea : MonoBehaviour
{
    public BoxCollider AreaCollider;

    private void Awake()
    {
        SetupCollider();
    }

    public bool ContainsFurniture(PlaceableFurniture furniture)
    {
        if (AreaCollider == null || furniture == null)
        {
            return false;
        }

        Vector3 center = furniture.GetBoundsWorldCenter();
        Vector3 halfExtents = furniture.GetBoundsWorldHalfExtents();
        Quaternion rotation = furniture.GetBoundsWorldRotation();

        Vector3[] footprintCorners = new Vector3[4];

        footprintCorners[0] = center + rotation * new Vector3(-halfExtents.x,0f,-halfExtents.z);
        footprintCorners[1] = center + rotation * new Vector3(-halfExtents.x,0f,halfExtents.z);
        footprintCorners[2] = center + rotation * new Vector3(halfExtents.x,0f,-halfExtents.z);
        footprintCorners[3] = center + rotation * new Vector3(halfExtents.x,0f,halfExtents.z);

        for (int i = 0; i < footprintCorners.Length; i++)
        {
            if (!ContainsHorizontalPoint(footprintCorners[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool ContainsHorizontalPoint(Vector3 worldPoint)
    {
        Vector3 localPoint = AreaCollider.transform.InverseTransformPoint(worldPoint);
        Vector3 relativePoint = localPoint - AreaCollider.center;
        Vector3 halfSize = AreaCollider.size * 0.5f;

        return Mathf.Abs(relativePoint.x) <= halfSize.x && Mathf.Abs(relativePoint.z) <= halfSize.z;
    }

    private void SetupCollider()
    {
        if (AreaCollider == null)
        {
            AreaCollider = GetComponent<BoxCollider>();
        }

        if (AreaCollider != null)
        {
            AreaCollider.isTrigger = true;
        }
    }

    private void OnValidate()
    {
        SetupCollider();
    }
}