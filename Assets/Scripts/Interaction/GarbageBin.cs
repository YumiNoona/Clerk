using UnityEngine;

[DisallowMultipleComponent]
public sealed class GarbageBin :
    InteractableBehaviour,
    IHeldItemInteractionTarget
{
    [Header("Accepted Items")]
    [SerializeField]
    private bool acceptProducts = true;

    [SerializeField]
    private bool acceptStockBoxes = true;

    private void Awake()
    {
        ConfigureLayer();
        EnsureCollider();
    }

    protected override bool SupportsInteraction(
        InteractionType interactionType)
    {
        return interactionType == InteractionType.Use;
    }

    protected override bool CanInteractInternal(
        InteractionContext context)
    {
        return TryGetDisposable(
            context.Player,
            out _);
    }

    protected override void OnInteract(
        InteractionContext context)
    {
        if (!TryGetDisposable(
                context.Player,
                out Component disposable))
        {
            return;
        }

        IHeldItem heldItem =
            context.Player.HeldItem;

        // Clear player ownership before destruction. This avoids a stale
        // interface reference on the following frame.
        context.Player.ClearHeldItem(heldItem);
        disposable.transform.SetParent(null,true);
        Destroy(disposable.gameObject);
    }

    protected override string GetDefaultInteractionPrompt(
        InteractionType interactionType)
    {
        return interactionType == InteractionType.Use
            ? GameBootstrap.Instance != null
                ? GameBootstrap.Instance.Input.FormatPrompt(
                    GameplayAction.Use,
                    "Throw Away")
                : "[E] Throw Away"
            : string.Empty;
    }

    protected override int GetDefaultInteractionPriority()
    {
        return 100;
    }

    private bool TryGetDisposable(
        PlayerInteractionController player,
        out Component disposable)
    {
        disposable = null;

        if (player == null || player.HeldItem == null)
        {
            return false;
        }

        if (acceptProducts &&
            player.HeldItem is StockObject product)
        {
            disposable = product;
            return true;
        }

        if (acceptStockBoxes &&
            player.HeldItem is StockBoxController box)
        {
            disposable = box;
            return true;
        }

        return false;
    }

    protected override void Reset()
    {
        base.Reset();

        ConfigureLayer();
        EnsureCollider();
    }

    private void ConfigureLayer()
    {
        int garbageLayer =
            LayerMask.NameToLayer("Garbage Bin");

        if (garbageLayer < 0)
        {
            return;
        }

        Transform[] hierarchy =
            GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < hierarchy.Length; i++)
        {
            hierarchy[i].gameObject.layer = garbageLayer;
        }
    }

    private void EnsureCollider()
    {
        if (GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        Renderer[] renderers =
            GetComponentsInChildren<Renderer>();

        BoxCollider box =
            gameObject.AddComponent<BoxCollider>();

        if (renderers.Length == 0)
        {
            box.center = new Vector3(0f,0.5f,0f);
            box.size = Vector3.one;
            return;
        }

        Bounds localBounds = new Bounds(
            transform.InverseTransformPoint(
                renderers[0].bounds.center),
            Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            EncapsulateWorldBounds(
                ref localBounds,
                renderers[i].bounds);
        }

        box.center = localBounds.center;
        box.size = localBounds.size;
    }

    private void EncapsulateWorldBounds(
        ref Bounds localBounds,
        Bounds worldBounds)
    {
        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center +
                        Vector3.Scale(
                            extents,
                            new Vector3(x,y,z));

                    localBounds.Encapsulate(
                        transform.InverseTransformPoint(
                            corner));
                }
            }
        }
    }
}
