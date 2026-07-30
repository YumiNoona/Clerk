using UnityEngine;

public class PlaceableFurniture : InteractableBehaviour
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
    private bool placementVisualInitialized;

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

        UpdateRotation();
    }

    protected override int GetDefaultInteractionPriority()
    {
        return 20;
    }

    protected override bool SupportsInteraction(
        InteractionType interactionType)
    {
        return interactionType == InteractionType.Move;
    }

    protected override bool CanInteractInternal(
        InteractionContext context)
    {
        if (isBeingPlaced)
        {
            return false;
        }

        if (FurniturePlacementController.Instance == null)
        {
            return false;
        }

        return !FurniturePlacementController.Instance.IsPlacing;
    }

    protected override void OnInteract(
        InteractionContext context)
    {
        FurniturePlacementController.Instance
            .BeginMovePlacement(this);
    }

    protected override string GetDefaultInteractionPrompt(
        InteractionType interactionType)
    {
        return interactionType == InteractionType.Move
            ? "[F] Move"
            : string.Empty;
    }

    private void UpdateRotation()
    {
        float smoothedYRotation = Mathf.SmoothDampAngle(
            transform.eulerAngles.y,
            targetYRotation,
            ref rotationVelocity,
            RotationSmoothTime);

        transform.rotation = Quaternion.Euler(
            0f,
            smoothedYRotation,
            0f);
    }

    private void CacheComponents()
    {
        CachePlacementBounds();
        CacheFurnitureColliders();
        CachePreviewRenderers();
        CacheOriginalMaterials();
    }

    private void CachePlacementBounds()
    {
        if (PlacementBounds == null)
        {
            PlacementBounds = GetComponent<BoxCollider>();
        }
    }

    private void CacheFurnitureColliders()
    {
        furnitureColliders =
            GetComponentsInChildren<Collider>(true);

        originalColliderStates =
            new bool[furnitureColliders.Length];

        for (int i = 0; i < furnitureColliders.Length; i++)
        {
            Collider furnitureCollider =
                furnitureColliders[i];

            originalColliderStates[i] =
                furnitureCollider != null &&
                furnitureCollider.enabled;
        }
    }

    private void CachePreviewRenderers()
    {
        if (PreviewRenderers != null &&
            PreviewRenderers.Length > 0)
        {
            return;
        }

        PreviewRenderers =
            GetComponentsInChildren<Renderer>(true);
    }

    private void CacheOriginalMaterials()
    {
        if (PreviewRenderers == null)
        {
            originalMaterials = null;
            return;
        }

        originalMaterials =
            new Material[PreviewRenderers.Length][];

        for (int i = 0; i < PreviewRenderers.Length; i++)
        {
            Renderer previewRenderer =
                PreviewRenderers[i];

            if (previewRenderer == null)
            {
                continue;
            }

            originalMaterials[i] =
                previewRenderer.sharedMaterials;
        }
    }

    public void BeginMovePlacement()
    {
        if (isBeingPlaced)
        {
            return;
        }

        CacheComponents();
        SaveOriginalTransform();

        targetYRotation = transform.eulerAngles.y;
        rotationVelocity = 0f;

        isBeingPlaced = true;
        placementVisualInitialized = false;

        transform.SetParent(null,true);

        ForceUpright();
        SetCollidersEnabled(false);
        SetPlacementValid(false);
    }

    private void SaveOriginalTransform()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;
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

        Vector3 targetPosition =
            GetSnappedPosition(surfacePoint);

        transform.position = new Vector3(
            targetPosition.x,
            surfacePoint.y,
            targetPosition.z);

        ForceUpright();
        AlignBottomToSurface(surfacePoint.y);
    }

    private Vector3 GetSnappedPosition(
        Vector3 position)
    {
        if (!SnapToGrid || GridSize <= 0f)
        {
            return position;
        }

        position.x =
            Mathf.Round(position.x / GridSize) *
            GridSize;

        position.z =
            Mathf.Round(position.z / GridSize) *
            GridSize;

        return position;
    }

    private void AlignBottomToSurface(float surfaceY)
    {
        float currentBottomY =
            GetPlacementBottomY();

        float verticalOffset =
            surfaceY - currentBottomY;

        transform.position +=
            Vector3.up * verticalOffset;
    }

    public void AddScrollRotation(float scrollAmount)
    {
        if (!isBeingPlaced ||
            Mathf.Approximately(scrollAmount,0f))
        {
            return;
        }

        targetYRotation +=
            scrollAmount * ScrollRotationAmount;
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

        if (placementVisualInitialized &&
            currentPlacementVisualValid == valid)
        {
            return;
        }

        placementVisualInitialized = true;
        currentPlacementVisualValid = valid;

        Material placementMaterial =
            valid
                ? ValidPlacementMaterial
                : InvalidPlacementMaterial;

        ApplyPreviewMaterial(placementMaterial);
    }

    private void ApplyPreviewMaterial(
        Material placementMaterial)
    {
        if (placementMaterial == null ||
            PreviewRenderers == null)
        {
            return;
        }

        for (int i = 0; i < PreviewRenderers.Length; i++)
        {
            Renderer previewRenderer =
                PreviewRenderers[i];

            if (previewRenderer == null)
            {
                continue;
            }

            Material[] replacementMaterials =
                previewRenderer.materials;

            for (int materialIndex = 0;
                 materialIndex <
                 replacementMaterials.Length;
                 materialIndex++)
            {
                replacementMaterials[materialIndex] =
                    placementMaterial;
            }

            previewRenderer.materials =
                replacementMaterials;
        }
    }

    public void ConfirmPlacement()
    {
        if (!isBeingPlaced)
        {
            return;
        }

        transform.SetParent(null,true);

        transform.rotation = Quaternion.Euler(
            0f,
            targetYRotation,
            0f);

        FinishPlacement();
    }

    public void CancelPlacement()
    {
        if (!isBeingPlaced)
        {
            return;
        }

        transform.SetParent(originalParent,true);

        transform.SetPositionAndRotation(
            originalPosition,
            originalRotation);

        FinishPlacement();
    }

    private void FinishPlacement()
    {
        isBeingPlaced = false;
        placementVisualInitialized = false;

        RestoreOriginalMaterials();
        RestoreColliderStates();
        DisableClearanceCollider();
    }

    public Vector3 GetBoundsWorldCenter()
    {
        return GetColliderWorldCenter(
            PlacementBounds);
    }

    public Vector3 GetBoundsWorldHalfExtents()
    {
        return GetColliderWorldHalfExtents(
            PlacementBounds);
    }

    public Quaternion GetBoundsWorldRotation()
    {
        if (PlacementBounds != null)
        {
            return PlacementBounds.transform.rotation;
        }

        return transform.rotation;
    }

    public Vector3 GetClearanceWorldCenter()
    {
        return GetColliderWorldCenter(
            GetClearanceBounds());
    }

    public Vector3 GetClearanceWorldHalfExtents()
    {
        return GetColliderWorldHalfExtents(
            GetClearanceBounds());
    }

    public Quaternion GetClearanceWorldRotation()
    {
        BoxCollider boundsToUse =
            GetClearanceBounds();

        if (boundsToUse != null)
        {
            return boundsToUse.transform.rotation;
        }

        return transform.rotation;
    }

    private BoxCollider GetClearanceBounds()
    {
        if (ClearanceBounds != null)
        {
            return ClearanceBounds;
        }

        return PlacementBounds;
    }

    public float GetPlacementBottomY()
    {
        Vector3 center =
            GetBoundsWorldCenter();

        Vector3 halfExtents =
            GetBoundsWorldHalfExtents();

        return center.y - halfExtents.y;
    }

    public bool ContainsCollider(
        Collider colliderToCheck)
    {
        if (colliderToCheck == null ||
            furnitureColliders == null)
        {
            return false;
        }

        for (int i = 0;
             i < furnitureColliders.Length;
             i++)
        {
            if (furnitureColliders[i] ==
                colliderToCheck)
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 GetColliderWorldCenter(
        BoxCollider boxCollider)
    {
        if (boxCollider == null)
        {
            return transform.position;
        }

        return boxCollider.transform.TransformPoint(
            boxCollider.center);
    }

    private Vector3 GetColliderWorldHalfExtents(
        BoxCollider boxCollider)
    {
        if (boxCollider == null)
        {
            return Vector3.one * 0.5f;
        }

        Vector3 scale =
            boxCollider.transform.lossyScale;

        Vector3 absoluteScale = new Vector3(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y),
            Mathf.Abs(scale.z));

        return Vector3.Scale(
            boxCollider.size * 0.5f,
            absoluteScale);
    }

    private void ForceUpright()
    {
        transform.rotation = Quaternion.Euler(
            0f,
            transform.eulerAngles.y,
            0f);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (furnitureColliders == null)
        {
            return;
        }

        for (int i = 0;
             i < furnitureColliders.Length;
             i++)
        {
            Collider furnitureCollider =
                furnitureColliders[i];

            if (furnitureCollider != null)
            {
                furnitureCollider.enabled = enabled;
            }
        }
    }

    private void RestoreColliderStates()
    {
        if (furnitureColliders == null ||
            originalColliderStates == null)
        {
            return;
        }

        int count = Mathf.Min(
            furnitureColliders.Length,
            originalColliderStates.Length);

        for (int i = 0; i < count; i++)
        {
            Collider furnitureCollider =
                furnitureColliders[i];

            if (furnitureCollider != null)
            {
                furnitureCollider.enabled =
                    originalColliderStates[i];
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (PreviewRenderers == null ||
            originalMaterials == null)
        {
            return;
        }

        int count = Mathf.Min(
            PreviewRenderers.Length,
            originalMaterials.Length);

        for (int i = 0; i < count; i++)
        {
            Renderer previewRenderer =
                PreviewRenderers[i];

            Material[] rendererMaterials =
                originalMaterials[i];

            if (previewRenderer == null ||
                rendererMaterials == null)
            {
                continue;
            }

            previewRenderer.materials =
                rendererMaterials;
        }
    }

    private void DisableClearanceCollider()
    {
        if (ClearanceBounds == null)
        {
            return;
        }

        ClearanceBounds.enabled = false;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        GridSize =
            Mathf.Max(0.01f,GridSize);

        ScrollRotationAmount =
            Mathf.Max(1f,ScrollRotationAmount);

        RotationSmoothTime =
            Mathf.Max(0.01f,RotationSmoothTime);

        CachePlacementBounds();

        if (ClearanceBounds != null)
        {
            ClearanceBounds.enabled = false;
            ClearanceBounds.isTrigger = true;
        }
    }
}