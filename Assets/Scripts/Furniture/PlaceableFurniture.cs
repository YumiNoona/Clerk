using UnityEngine;

public class PlaceableFurniture : MonoBehaviour
{
    [Header("Placement Bounds")]
    public BoxCollider PlacementBounds;

    [Tooltip("Optional larger box that reserves customer walking/access space. Keep this collider disabled.")]
    public BoxCollider ClearanceBounds;

    [Header("Preview Renderers")]
    public Renderer[] PreviewRenderers;

    [Header("Preview Materials")]
    public Material ValidPlacementMaterial;
    public Material InvalidPlacementMaterial;

    [Header("Placement Settings")]
    public bool SnapToGrid = true;
    public float GridSize = 0.25f;

    [Header("Rotation")]
    public float ScrollRotationAmount = 15f;
    public float RotationSmoothTime = 0.08f;

    private Collider[] furnitureColliders;
    private bool[] originalColliderStates;
    private Material[][] originalMaterials;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;

    private bool isBeingPlaced;
    private bool currentPlacementVisualValid;
    private float targetYRotation;
    private float rotationVelocity;

    public bool IsBeingPlaced
    {
        get
        {
            return isBeingPlaced;
        }
    }

    private void Awake()
    {
        CacheComponents();
        targetYRotation = transform.eulerAngles.y;
    }

    private void Update()
    {
        if (!isBeingPlaced)
        {
            return;
        }

        float smoothedYRotation = Mathf.SmoothDampAngle(transform.eulerAngles.y,targetYRotation,ref rotationVelocity,RotationSmoothTime);
        transform.rotation = Quaternion.Euler(0f,smoothedYRotation,0f);
    }

    private void CacheComponents()
    {
        if (PlacementBounds == null)
        {
            PlacementBounds = GetComponent<BoxCollider>();
        }

        furnitureColliders = GetComponentsInChildren<Collider>(true);
        originalColliderStates = new bool[furnitureColliders.Length];

        for (int i = 0; i < furnitureColliders.Length; i++)
        {
            originalColliderStates[i] = furnitureColliders[i] != null && furnitureColliders[i].enabled;
        }

        if (PreviewRenderers == null || PreviewRenderers.Length == 0)
        {
            PreviewRenderers = GetComponentsInChildren<Renderer>(true);
        }

        originalMaterials = new Material[PreviewRenderers.Length][];

        for (int i = 0; i < PreviewRenderers.Length; i++)
        {
            if (PreviewRenderers[i] != null)
            {
                originalMaterials[i] = PreviewRenderers[i].sharedMaterials;
            }
        }
    }

    public void BeginMovePlacement()
    {
        if (isBeingPlaced)
        {
            return;
        }

        CacheComponents();

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;

        targetYRotation = transform.eulerAngles.y;
        rotationVelocity = 0f;
        isBeingPlaced = true;

        transform.SetParent(null,true);
        ForceUpright();
        SetCollidersEnabled(false);
        SetPlacementValid(false);
    }

    public void AttachToHoldPoint(Transform holdPoint)
    {
        if (!isBeingPlaced || holdPoint == null)
        {
            return;
        }

        transform.SetParent(holdPoint,false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        targetYRotation = holdPoint.eulerAngles.y;
        rotationVelocity = 0f;
    }

    public void SetPlacementPosition(Vector3 surfacePoint)
    {
        if (!isBeingPlaced)
        {
            return;
        }

        transform.SetParent(null,true);

        Vector3 targetPosition = surfacePoint;

        if (SnapToGrid && GridSize > 0f)
        {
            targetPosition.x = Mathf.Round(targetPosition.x / GridSize) * GridSize;
            targetPosition.z = Mathf.Round(targetPosition.z / GridSize) * GridSize;
        }

        transform.position = new Vector3(targetPosition.x,surfacePoint.y,targetPosition.z);
        ForceUpright();

        float currentBottomY = GetPlacementBottomY();
        transform.position += Vector3.up * (surfacePoint.y - currentBottomY);
    }

    public void AddScrollRotation(float scrollAmount)
    {
        if (!isBeingPlaced || Mathf.Approximately(scrollAmount,0f))
        {
            return;
        }

        targetYRotation += scrollAmount * ScrollRotationAmount;
    }

    public void RotateByStep(float rotationAmount)
    {
        if (!isBeingPlaced)
        {
            return;
        }

        targetYRotation += rotationAmount;
    }

    public void SetPlacementValid(bool valid)
    {
        if (!isBeingPlaced)
        {
            return;
        }

        if (currentPlacementVisualValid == valid)
        {
            return;
        }

        currentPlacementVisualValid = valid;

        Material placementMaterial = valid ? ValidPlacementMaterial : InvalidPlacementMaterial;

        if (placementMaterial == null || PreviewRenderers == null)
        {
            return;
        }

        for (int i = 0; i < PreviewRenderers.Length; i++)
        {
            Renderer previewRenderer = PreviewRenderers[i];

            if (previewRenderer == null)
            {
                continue;
            }

            Material[] replacementMaterials = previewRenderer.materials;

            for (int materialIndex = 0; materialIndex < replacementMaterials.Length; materialIndex++)
            {
                replacementMaterials[materialIndex] = placementMaterial;
            }

            previewRenderer.materials = replacementMaterials;
        }
    }

    public void ConfirmPlacement()
    {
        if (!isBeingPlaced)
        {
            return;
        }

        transform.SetParent(null,true);
        transform.rotation = Quaternion.Euler(0f,targetYRotation,0f);

        isBeingPlaced = false;

        RestoreOriginalMaterials();
        RestoreColliderStates();

        if (ClearanceBounds != null)
        {
            ClearanceBounds.enabled = false;
        }
    }

    public void CancelPlacement()
    {
        if (!isBeingPlaced)
        {
            return;
        }

        transform.SetParent(originalParent,true);
        transform.SetPositionAndRotation(originalPosition,originalRotation);

        isBeingPlaced = false;

        RestoreOriginalMaterials();
        RestoreColliderStates();

        if (ClearanceBounds != null)
        {
            ClearanceBounds.enabled = false;
        }
    }

    public Vector3 GetBoundsWorldCenter()
    {
        return GetColliderWorldCenter(PlacementBounds);
    }

    public Vector3 GetBoundsWorldHalfExtents()
    {
        return GetColliderWorldHalfExtents(PlacementBounds);
    }

    public Quaternion GetBoundsWorldRotation()
    {
        return PlacementBounds != null ? PlacementBounds.transform.rotation : transform.rotation;
    }

    public Vector3 GetClearanceWorldCenter()
    {
        BoxCollider boundsToUse = ClearanceBounds != null ? ClearanceBounds : PlacementBounds;
        return GetColliderWorldCenter(boundsToUse);
    }

    public Vector3 GetClearanceWorldHalfExtents()
    {
        BoxCollider boundsToUse = ClearanceBounds != null ? ClearanceBounds : PlacementBounds;
        return GetColliderWorldHalfExtents(boundsToUse);
    }

    public Quaternion GetClearanceWorldRotation()
    {
        BoxCollider boundsToUse = ClearanceBounds != null ? ClearanceBounds : PlacementBounds;
        return boundsToUse != null ? boundsToUse.transform.rotation : transform.rotation;
    }

    public float GetPlacementBottomY()
    {
        Vector3 center = GetBoundsWorldCenter();
        Vector3 halfExtents = GetBoundsWorldHalfExtents();

        return center.y - halfExtents.y;
    }

    public bool ContainsCollider(Collider colliderToCheck)
    {
        if (colliderToCheck == null || furnitureColliders == null)
        {
            return false;
        }

        for (int i = 0; i < furnitureColliders.Length; i++)
        {
            if (furnitureColliders[i] == colliderToCheck)
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 GetColliderWorldCenter(BoxCollider boxCollider)
    {
        if (boxCollider == null)
        {
            return transform.position;
        }

        return boxCollider.transform.TransformPoint(boxCollider.center);
    }

    private Vector3 GetColliderWorldHalfExtents(BoxCollider boxCollider)
    {
        if (boxCollider == null)
        {
            return Vector3.one * 0.5f;
        }

        Vector3 scale = boxCollider.transform.lossyScale;
        Vector3 absoluteScale = new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));

        return Vector3.Scale(boxCollider.size * 0.5f,absoluteScale);
    }

    private void ForceUpright()
    {
        transform.rotation = Quaternion.Euler(0f,transform.eulerAngles.y,0f);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (furnitureColliders == null)
        {
            return;
        }

        for (int i = 0; i < furnitureColliders.Length; i++)
        {
            if (furnitureColliders[i] != null)
            {
                furnitureColliders[i].enabled = enabled;
            }
        }
    }

    private void RestoreColliderStates()
    {
        if (furnitureColliders == null || originalColliderStates == null)
        {
            return;
        }

        int count = Mathf.Min(furnitureColliders.Length,originalColliderStates.Length);

        for (int i = 0; i < count; i++)
        {
            if (furnitureColliders[i] != null)
            {
                furnitureColliders[i].enabled = originalColliderStates[i];
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (PreviewRenderers == null || originalMaterials == null)
        {
            return;
        }

        int count = Mathf.Min(PreviewRenderers.Length,originalMaterials.Length);

        for (int i = 0; i < count; i++)
        {
            if (PreviewRenderers[i] != null && originalMaterials[i] != null)
            {
                PreviewRenderers[i].materials = originalMaterials[i];
            }
        }
    }

    private void OnValidate()
    {
        GridSize = Mathf.Max(0.01f,GridSize);
        ScrollRotationAmount = Mathf.Max(1f,ScrollRotationAmount);
        RotationSmoothTime = Mathf.Max(0.01f,RotationSmoothTime);

        if (PlacementBounds == null)
        {
            PlacementBounds = GetComponent<BoxCollider>();
        }

        if (ClearanceBounds != null)
        {
            ClearanceBounds.enabled = false;
            ClearanceBounds.isTrigger = true;
        }
    }
}