using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum FurnitureMarkerType
{
    Customer,
    Employee,
    Player,
    Queue,
    Pickup,
    DropOff,
    Clearance
}

[Serializable]
public sealed class FurnitureInteractionMarker
{
    public string Name = "Interaction";
    public FurnitureMarkerType Type = FurnitureMarkerType.Customer;
    public Vector3 LocalPosition;
    public Vector3 LocalEulerAngles;
    public Vector2 AreaSize = new Vector2(0.65f,0.65f);
    public bool ShowFacingArrow = true;
}

[DisallowMultipleComponent]
public sealed class FurniturePlacementProfile : MonoBehaviour
{
    public bool OverrideFootprint;
    public Vector3 FootprintCenter;
    public Vector2 FootprintSize = Vector2.one;
    public List<FurnitureInteractionMarker> Markers = new List<FurnitureInteractionMarker>();

    public void GetLocalFootprint(PlaceableFurniture furniture,out Vector3 center,out Vector2 size)
    {
        if (OverrideFootprint)
        {
            center = FootprintCenter;
            size = new Vector2(Mathf.Max(0.05f,FootprintSize.x),Mathf.Max(0.05f,FootprintSize.y));
            return;
        }
        BoxCollider bounds = furniture != null ? furniture.PlacementBounds : null;
        center = bounds != null ? bounds.center : Vector3.zero;
        Vector3 scaled = bounds != null ? Vector3.Scale(bounds.size,bounds.transform.lossyScale) : Vector3.one;
        Vector3 localScale = transform.lossyScale;
        size = new Vector2(
            Mathf.Max(0.05f,scaled.x / Mathf.Max(0.001f,localScale.x)),
            Mathf.Max(0.05f,scaled.z / Mathf.Max(0.001f,localScale.z)));
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        PlaceableFurniture furniture = GetComponent<PlaceableFurniture>();
        GetLocalFootprint(furniture,out Vector3 center,out Vector2 size);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.25f,0.75f,1f,0.9f);
        Gizmos.DrawWireCube(center,new Vector3(size.x,0.03f,size.y));
        for (int i = 0; i < Markers.Count; i++)
        {
            FurnitureInteractionMarker marker = Markers[i];
            if (marker == null) continue;
            Gizmos.color = PlacementGuideRenderer.GetMarkerColor(marker.Type);
            Gizmos.DrawWireCube(marker.LocalPosition,new Vector3(marker.AreaSize.x,0.03f,marker.AreaSize.y));
            Quaternion rotation = Quaternion.Euler(marker.LocalEulerAngles);
            Gizmos.DrawLine(marker.LocalPosition,marker.LocalPosition + rotation * Vector3.forward * 0.65f);
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}

[DisallowMultipleComponent]
public sealed class PlacementGuideRenderer : MonoBehaviour
{
    public static PlacementGuideRenderer Instance { get; private set; }
    private readonly List<LineRenderer> segments = new List<LineRenderer>();
    private int used;
    private Material sharedMaterial;
    private TextMeshPro slotLabel;
    private const float GroundLift = 0.025f;

    public static Color GetMarkerColor(FurnitureMarkerType type) => type switch
    {
        FurnitureMarkerType.Customer => new Color(0.2f,0.75f,1f,1f),
        FurnitureMarkerType.Employee => new Color(1f,0.7f,0.15f,1f),
        FurnitureMarkerType.Player => new Color(0.55f,0.35f,1f,1f),
        FurnitureMarkerType.Queue => new Color(0.2f,0.9f,0.55f,1f),
        FurnitureMarkerType.Pickup => new Color(0.15f,1f,0.75f,1f),
        FurnitureMarkerType.DropOff => new Color(1f,0.45f,0.2f,1f),
        _ => Color.white
    };

    private void Awake()
    {
        Instance = this;
        Shader shader = Shader.Find("Sprites/Default");
        sharedMaterial = new Material(shader) { name = "Runtime Placement Guide" };
        GameObject labelObject = new GameObject("Placement Guide Label");
        labelObject.transform.SetParent(transform,false);
        slotLabel = labelObject.AddComponent<TextMeshPro>();
        slotLabel.fontSize = 2.4f;
        slotLabel.alignment = TextAlignmentOptions.Center;
        slotLabel.color = Color.white;
        slotLabel.gameObject.SetActive(false);
    }

    public void ShowFurniture(PlaceableFurniture furniture,bool valid)
    {
        Hide();
        if (furniture == null) return;
        FurniturePlacementProfile profile = furniture.GetComponent<FurniturePlacementProfile>();
        Vector3 localCenter;
        Vector2 size;
        if (profile != null) profile.GetLocalFootprint(furniture,out localCenter,out size);
        else
        {
            BoxCollider bounds = furniture.PlacementBounds;
            localCenter = bounds != null ? bounds.center : Vector3.zero;
            size = bounds != null ? new Vector2(bounds.size.x,bounds.size.z) : Vector2.one;
        }
        float floorY = furniture.GetClearanceWorldCenter().y - furniture.GetClearanceWorldHalfExtents().y + GroundLift;
        DrawDashedRect(furniture.transform,localCenter,size,floorY,
            valid ? new Color(0.2f,0.95f,0.45f,1f) : new Color(1f,0.25f,0.25f,1f),0.16f,0.09f,0.035f);
        if (profile != null)
        {
            for (int i = 0; i < profile.Markers.Count; i++)
            {
                FurnitureInteractionMarker marker = profile.Markers[i];
                if (marker == null) continue;
                DrawDashedRect(furniture.transform,marker.LocalPosition,marker.AreaSize,floorY,
                    GetMarkerColor(marker.Type),0.12f,0.07f,0.025f);
                if (marker.ShowFacingArrow)
                {
                    Quaternion rotation = furniture.transform.rotation * Quaternion.Euler(marker.LocalEulerAngles);
                    Vector3 start = furniture.transform.TransformPoint(marker.LocalPosition);
                    start.y = floorY;
                    DrawArrow(start,rotation * Vector3.forward,GetMarkerColor(marker.Type));
                }
            }
        }
        DrawAutomaticFurnitureMarkers(furniture,floorY);
    }

    public void ShowShelfSlot(ShelfSpaceController shelf,Vector3 worldPosition,Quaternion rotation,
        Vector2 size,bool valid,int remainingSlots)
    {
        Hide();
        if (shelf == null) return;
        DrawWorldDashedBox(worldPosition + Vector3.up * 0.11f,rotation,
            new Vector3(size.x,0.22f,size.y),
            valid ? new Color(0.2f,1f,0.45f,1f) : new Color(1f,0.25f,0.25f,1f),
            0.08f,0.04f,0.018f);
        slotLabel.gameObject.SetActive(true);
        slotLabel.text = valid ? remainingSlots + " SLOTS" : "BLOCKED";
        slotLabel.color = valid ? new Color(0.2f,1f,0.45f,1f) : new Color(1f,0.25f,0.25f,1f);
        slotLabel.transform.position = worldPosition + Vector3.up * 0.34f;
        Camera camera = Camera.main;
        if (camera != null) slotLabel.transform.rotation = camera.transform.rotation;
    }

    public void Hide()
    {
        for (int i = 0; i < segments.Count; i++) segments[i].gameObject.SetActive(false);
        if (slotLabel != null) slotLabel.gameObject.SetActive(false);
        used = 0;
    }

    private void DrawAutomaticFurnitureMarkers(PlaceableFurniture furniture,float floorY)
    {
        CheckoutCounter checkout = furniture.GetComponentInChildren<CheckoutCounter>(true);
        if (checkout != null)
        {
            for (int i = 0; i < checkout.QueueCapacity; i++)
            {
                Vector3 position = checkout.GetQueueWorldPosition(i);
                position.y = floorY;
                DrawWorldDashedRect(position,checkout.transform.rotation,new Vector2(0.62f,0.62f),
                    GetMarkerColor(FurnitureMarkerType.Queue),0.10f,0.055f,0.022f);
            }
            Vector3 clerk = checkout.ClerkStandingPosition;
            clerk.y = floorY;
            DrawWorldDashedRect(clerk,checkout.transform.rotation,new Vector2(0.7f,0.7f),
                GetMarkerColor(FurnitureMarkerType.Player),0.10f,0.055f,0.022f);
        }
        ShelfSpaceController[] shelves = furniture.GetComponentsInChildren<ShelfSpaceController>(true);
        for (int i = 0; i < shelves.Length; i++)
        {
            Vector3 customer = shelves[i].CustomerStandingPosition;
            customer.y = floorY;
            DrawWorldDashedRect(customer,shelves[i].CustomerStandingRotation,new Vector2(0.65f,0.65f),
                GetMarkerColor(FurnitureMarkerType.Customer),0.10f,0.055f,0.022f);
        }
    }

    private void DrawDashedRect(Transform root,Vector3 localCenter,Vector2 size,float worldY,
        Color color,float dash,float gap,float width)
    {
        Vector3 center = root.TransformPoint(localCenter);
        center.y = worldY;
        Vector3 scale = root.lossyScale;
        DrawWorldDashedRect(center,root.rotation,
            new Vector2(size.x*Mathf.Abs(scale.x),size.y*Mathf.Abs(scale.z)),color,dash,gap,width);
    }

    private void DrawWorldDashedRect(Vector3 center,Quaternion rotation,Vector2 size,
        Color color,float dash,float gap,float width)
    {
        Vector3 right = rotation * Vector3.right * size.x * 0.5f;
        Vector3 forward = rotation * Vector3.forward * size.y * 0.5f;
        Vector3 a=center-right-forward,b=center+right-forward,c=center+right+forward,d=center-right+forward;
        DrawDashedLine(a,b,color,dash,gap,width);
        DrawDashedLine(b,c,color,dash,gap,width);
        DrawDashedLine(c,d,color,dash,gap,width);
        DrawDashedLine(d,a,color,dash,gap,width);
    }

    private void DrawWorldDashedBox(Vector3 center,Quaternion rotation,Vector3 size,
        Color color,float dash,float gap,float width)
    {
        Vector3 half = size * 0.5f;
        Vector3[] points = new Vector3[8];
        int index = 0;
        for (int y=-1; y<=1; y+=2)
        for (int z=-1; z<=1; z+=2)
        for (int x=-1; x<=1; x+=2)
            points[index++] = center + rotation * new Vector3(half.x*x,half.y*y,half.z*z);
        int[,] edges =
        {
            {0,1},{2,3},{4,5},{6,7},
            {0,2},{1,3},{4,6},{5,7},
            {0,4},{1,5},{2,6},{3,7}
        };
        for (int i=0; i<12; i++)
            DrawDashedLine(points[edges[i,0]],points[edges[i,1]],color,dash,gap,width);
    }

    private void DrawArrow(Vector3 start,Vector3 direction,Color color)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        direction.Normalize();
        Vector3 tip = start + direction * 0.6f;
        DrawSolid(start,tip,color,0.025f);
        DrawSolid(tip,tip - Quaternion.Euler(0f,35f,0f)*direction*0.18f,color,0.025f);
        DrawSolid(tip,tip - Quaternion.Euler(0f,-35f,0f)*direction*0.18f,color,0.025f);
    }

    private void DrawDashedLine(Vector3 from,Vector3 to,Color color,float dash,float gap,float width)
    {
        float length = Vector3.Distance(from,to);
        Vector3 direction = length > 0f ? (to-from)/length : Vector3.zero;
        for (float distance=0f; distance<length; distance+=dash+gap)
        {
            DrawSolid(from+direction*distance,from+direction*Mathf.Min(length,distance+dash),color,width);
        }
    }

    private void DrawSolid(Vector3 from,Vector3 to,Color color,float width)
    {
        LineRenderer line = GetSegment();
        line.startColor = line.endColor = color;
        line.startWidth = line.endWidth = width;
        line.SetPosition(0,from);
        line.SetPosition(1,to);
    }

    private LineRenderer GetSegment()
    {
        LineRenderer line;
        if (used < segments.Count) line = segments[used];
        else
        {
            GameObject child = new GameObject("Guide Segment");
            child.transform.SetParent(transform,false);
            line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = sharedMaterial;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.textureMode = LineTextureMode.Stretch;
            segments.Add(line);
        }
        used++;
        line.gameObject.SetActive(true);
        return line;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (sharedMaterial != null) Destroy(sharedMaterial);
    }
}
