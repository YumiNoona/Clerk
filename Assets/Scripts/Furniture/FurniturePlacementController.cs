using UnityEngine;

public class FurniturePlacementController : MonoBehaviour
{
    public static FurniturePlacementController Instance { get; private set; }

    [Header("References")]
    public Camera PlayerCamera;
    public Transform FurnitureHoldPoint;
    public FurniturePlacementArea PlacementArea;

    [Header("Placement Raycast")]
    public LayerMask PlacementSurfaceMask;
    public float MaximumPlacementDistance = 8f;

    [Header("Movement Smoothing")]
    public float PositionSmoothTime = 0.05f;
    public float MaximumPositionSpeed = 30f;

    [Header("Collision Validation")]
    public LayerMask PlacementBlockingMask;
    public float BoundsPadding = 0.02f;

    [Header("Input")]
    public float KeyboardRotationAmount = 90f;

    [Header("Debug")]
    public bool ShowPlacementDebug;

    private PlaceableFurniture activeFurniture;
    private bool placementIsValid;
    private bool hasValidSurface;

    private Vector3 smoothedSurfacePoint;
    private Vector3 surfacePointVelocity;
    private bool hasSmoothedSurfacePoint;

    public bool IsPlacing
    {
        get
        {
            return activeFurniture != null;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (PlayerCamera == null)
        {
            PlayerCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (activeFurniture == null)
        {
            return;
        }

        UpdateFurniturePosition();
        HandlePlacementInput();
    }

    public bool BeginMovePlacement(PlaceableFurniture furniture)
    {
        if (furniture == null || activeFurniture != null || furniture.IsBeingPlaced)
        {
            return false;
        }

        activeFurniture = furniture;
        placementIsValid = false;
        hasValidSurface = false;
        hasSmoothedSurfacePoint = false;
        surfacePointVelocity = Vector3.zero;

        activeFurniture.BeginMovePlacement();

        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.GameplayModes
                .TrySetMode(
                    GameplayMode.FurniturePlacement);
        }

        if (FurnitureHoldPoint != null)
        {
            activeFurniture.AttachToHoldPoint(FurnitureHoldPoint);
        }

        return true;
    }

    private void UpdateFurniturePosition()
    {
        if (PlayerCamera == null || activeFurniture == null)
        {
            return;
        }

        Ray placementRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f,0.5f,0f));

        if (!Physics.Raycast(placementRay,out RaycastHit hit,MaximumPlacementDistance,PlacementSurfaceMask,QueryTriggerInteraction.Ignore))
        {
            hasValidSurface = false;
            placementIsValid = false;
            hasSmoothedSurfacePoint = false;

            if (FurnitureHoldPoint != null)
            {
                activeFurniture.AttachToHoldPoint(FurnitureHoldPoint);
            }

            activeFurniture.SetPlacementValid(false);
            return;
        }

        hasValidSurface = true;

        if (!hasSmoothedSurfacePoint)
        {
            smoothedSurfacePoint = hit.point;
            hasSmoothedSurfacePoint = true;
        }
        else
        {
            smoothedSurfacePoint = Vector3.SmoothDamp(smoothedSurfacePoint,hit.point,ref surfacePointVelocity,PositionSmoothTime,MaximumPositionSpeed);
        }

        activeFurniture.SetPlacementPosition(smoothedSurfacePoint);

        placementIsValid = ValidatePlacement(activeFurniture);
        activeFurniture.SetPlacementValid(placementIsValid);
    }

    private bool ValidatePlacement(PlaceableFurniture furniture)
    {
        if (furniture == null || !hasValidSurface)
        {
            return false;
        }

        if (PlacementArea != null && !PlacementArea.ContainsFurniture(furniture))
        {
            return false;
        }

        Vector3 center = furniture.GetClearanceWorldCenter();
        Vector3 halfExtents = furniture.GetClearanceWorldHalfExtents();

        halfExtents.x = Mathf.Max(0.01f,halfExtents.x - BoundsPadding);
        halfExtents.y = Mathf.Max(0.01f,halfExtents.y - BoundsPadding);
        halfExtents.z = Mathf.Max(0.01f,halfExtents.z - BoundsPadding);

        Collider[] overlaps = Physics.OverlapBox(center,halfExtents,furniture.GetClearanceWorldRotation(),PlacementBlockingMask,QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];

            if (overlap == null || furniture.ContainsCollider(overlap))
            {
                continue;
            }

            if (ShowPlacementDebug)
            {
                Debug.Log("Furniture placement blocked by: " + overlap.name,overlap);
            }

            return false;
        }

        return true;
    }

    private void HandlePlacementInput()
    {
        GameInputController input =
            GameBootstrap.Instance != null
                ? GameBootstrap.Instance.Input
                : null;

        if (input == null)
        {
            return;
        }

        float scrollValue =
            input.ReadFloat(GameplayAction.Scroll);

        if (!Mathf.Approximately(scrollValue,0f))
        {
            float scrollDirection = Mathf.Sign(scrollValue);
            activeFurniture.AddScrollRotation(scrollDirection);
        }

        if (input.WasPressedThisFrame(
                GameplayAction.Primary))
        {
            ConfirmPlacement();
        }

        if (input.WasPressedThisFrame(
                GameplayAction.Secondary) ||
            input.WasPressedThisFrame(
                GameplayAction.Cancel))
        {
            CancelPlacement();
            return;
        }

        if (input.WasPressedThisFrame(
                GameplayAction.Rotate))
        {
            activeFurniture.RotateByStep(
                KeyboardRotationAmount);
        }
    }

    public void ConfirmPlacement()
    {
        if (activeFurniture == null)
        {
            return;
        }

        if (!placementIsValid)
        {
            Debug.LogWarning("Furniture cannot be placed at this position.",activeFurniture);
            return;
        }

        activeFurniture.ConfirmPlacement();

        activeFurniture = null;
        placementIsValid = false;
        hasValidSurface = false;
        hasSmoothedSurfacePoint = false;
        surfacePointVelocity = Vector3.zero;

        ReturnToGameplayMode();
    }

    public void CancelPlacement()
    {
        if (activeFurniture == null)
        {
            return;
        }

        activeFurniture.CancelPlacement();

        activeFurniture = null;
        placementIsValid = false;
        hasValidSurface = false;
        hasSmoothedSurfacePoint = false;
        surfacePointVelocity = Vector3.zero;

        ReturnToGameplayMode();
    }

    private static void ReturnToGameplayMode()
    {
        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.GameplayModes
                .TrySetMode(GameplayMode.Gameplay);
        }
    }

    private void OnDrawGizmos()
    {
        if (activeFurniture == null)
        {
            return;
        }

        Gizmos.matrix = Matrix4x4.TRS(activeFurniture.GetClearanceWorldCenter(),activeFurniture.GetClearanceWorldRotation(),Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero,activeFurniture.GetClearanceWorldHalfExtents() * 2f);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
