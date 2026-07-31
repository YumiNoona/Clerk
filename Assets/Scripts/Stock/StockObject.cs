using UnityEngine;

public class StockObject :
    InteractableBehaviour,
    IHeldItem
{
    [Header("Product")]
    public StockInfo Info;

    [Header("Shelf Movement")]
    public float MoveSpeed = 10f;
    public bool IsPlaced;

    [Header("Physics")]
    public Rigidbody TheRB;
    public MeshCollider MeshCollider;

    [Header("Held Interaction")]
    public float ThrowForce = 10f;

    [TextArea]
    public string PlaceOnShelfPrompt =
        "[Left Click] Place Stock";

    [TextArea]
    public string ThrowPrompt =
        "[Right Click] Throw";

    private Vector3 targetLocalPosition;
    private Quaternion targetLocalRotation =
        Quaternion.identity;

    private bool isBoxPreview;
    private bool isHeld;

    public bool IsBoxPreview => isBoxPreview;
    public bool IsHeld => isHeld;

    public bool CanBeHeld =>
        !isBoxPreview &&
        !isHeld &&
        !IsPlaced;

    private void Awake()
    {
        CacheComponents();
    }

    private void Update()
    {
        if (!IsPlaced)
        {
            return;
        }

        transform.localPosition =
            Vector3.MoveTowards(
                transform.localPosition,
                targetLocalPosition,
                MoveSpeed * Time.deltaTime);

        transform.localRotation =
            Quaternion.Slerp(
                transform.localRotation,
                targetLocalRotation,
                MoveSpeed * Time.deltaTime);
    }

    protected override int
        GetDefaultInteractionPriority()
    {
        return 30;
    }

    protected override bool SupportsInteraction(
        InteractionType interactionType)
    {
        return interactionType ==
               InteractionType.Primary;
    }

    protected override bool CanInteractInternal(
        InteractionContext context)
    {
        return CanBeHeld &&
               !context.Player.IsHoldingAnything;
    }

    protected override void OnInteract(
        InteractionContext context)
    {
        context.Player.TryHold(this);
    }

    protected override string
        GetDefaultInteractionPrompt(
            InteractionType interactionType)
    {
        return interactionType ==
               InteractionType.Primary
            ? GameBootstrap.Instance != null
                ? GameBootstrap.Instance.Input
                    .FormatPrompt(
                        GameplayAction.Primary,
                        "Pick Up")
                : "[Left Click] Pick Up"
            : string.Empty;
    }

    public Transform GetHoldPoint(
        PlayerInteractionController player)
    {
        return player != null
            ? player.HoldPoint
            : null;
    }

    public bool Pickup(
        PlayerInteractionController player,
        Transform holdPoint)
    {
        if (!CanBeHeld || holdPoint == null)
        {
            return false;
        }

        isHeld = true;
        IsPlaced = false;

        transform.SetParent(holdPoint,false);
        transform.localPosition = Vector3.zero;
        transform.localRotation =
            Quaternion.identity;

        SetPhysicsHeld(true);

        return true;
    }

    public void PrepareForShelfPickup()
    {
        IsPlaced = false;
    }

    public void HandleHeldUpdate(
        PlayerInteractionController player,
        Ray interactionRay)
    {
        if (!isHeld || player == null)
        {
            return;
        }

        if (player.WasPressed(
                GameplayAction.Primary))
        {
            TryPlaceOnShelf(
                player,
                interactionRay);
        }

        if (player.WasPressed(
                GameplayAction.Secondary))
        {
            Throw(player);
        }
    }

    public string GetHeldPrompt(
        PlayerInteractionController player,
        Ray interactionRay)
    {
        bool canPlace = false;

        if (player != null &&
            player.TryGetComponentInRay(
                interactionRay,
                out ShelfSpaceController shelf,
                out _))
        {
            canPlace =
                shelf.CanAcceptStock(this);
        }

        if (canPlace)
        {
            return GetPrompt(
                       player,
                       GameplayAction.Primary,
                       "Place Stock",
                       PlaceOnShelfPrompt) +
                   "\n" +
                   GetPrompt(
                       player,
                       GameplayAction.Secondary,
                       "Throw",
                       ThrowPrompt);
        }

        return GetPrompt(
            player,
            GameplayAction.Secondary,
            "Throw",
            ThrowPrompt);
    }

    private static string GetPrompt(
        PlayerInteractionController player,
        GameplayAction action,
        string description,
        string legacyPrompt)
    {
        return player != null &&
               GameBootstrap.Instance != null
            ? player.FormatPrompt(action,description)
            : legacyPrompt;
    }

    private void TryPlaceOnShelf(
        PlayerInteractionController player,
        Ray interactionRay)
    {
        if (!player.TryGetComponentInRay(
                interactionRay,
                out ShelfSpaceController shelf,
                out _))
        {
            return;
        }

        if (!shelf.PlaceStock(this))
        {
            return;
        }

        isHeld = false;
        player.ClearHeldItem(this);
    }

    private void Throw(
        PlayerInteractionController player)
    {
        Release();

        if (TheRB != null &&
            player.TheCamera != null)
        {
            TheRB.AddForce(
                player.TheCamera.transform.forward *
                ThrowForce,
                ForceMode.Impulse);
        }

        player.ClearHeldItem(this);
    }

    public void ForceRelease(
        PlayerInteractionController player)
    {
        Release();

        if (player != null)
        {
            player.ClearHeldItem(this);
        }
    }

    public void MakePlaced(
        Vector3 localPosition,
        Quaternion localRotation)
    {
        isBoxPreview = false;
        isHeld = false;

        targetLocalPosition = localPosition;
        targetLocalRotation = localRotation;
        IsPlaced = true;

        SetPhysicsHeld(true);
    }

    public void SetAsBoxPreview()
    {
        isBoxPreview = true;
        isHeld = false;
        IsPlaced = false;

        SetPhysicsHeld(true);
        gameObject.layer = 0;
    }

    public void Release()
    {
        if (isBoxPreview)
        {
            return;
        }

        isHeld = false;
        IsPlaced = false;

        transform.SetParent(null,true);
        SetPhysicsHeld(false);
    }

    private void CacheComponents()
    {
        if (TheRB == null)
        {
            TheRB =
                GetComponent<Rigidbody>();
        }

        if (MeshCollider == null)
        {
            MeshCollider =
                GetComponentInChildren<
                    MeshCollider>();
        }
    }

    private void SetPhysicsHeld(bool held)
    {
        if (TheRB != null)
        {
            if (!TheRB.isKinematic)
            {
                TheRB.linearVelocity =
                    Vector3.zero;

                TheRB.angularVelocity =
                    Vector3.zero;
            }

            TheRB.isKinematic = held;

            if (!held)
            {
                TheRB.linearVelocity =
                    Vector3.zero;

                TheRB.angularVelocity =
                    Vector3.zero;
            }
        }

        if (MeshCollider != null)
        {
            MeshCollider.enabled = !held;
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        MoveSpeed =
            Mathf.Max(0.01f,MoveSpeed);

        ThrowForce =
            Mathf.Max(0f,ThrowForce);
    }
}
