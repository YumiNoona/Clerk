using UnityEngine;

public sealed class StoreDeviceInteractable :
    InteractableBehaviour
{
    [SerializeField]
    private StoreDeviceKind deviceKind =
        StoreDeviceKind.Desktop;

    protected override int
        GetDefaultInteractionPriority()
    {
        return 40;
    }

    protected override bool SupportsInteraction(
        InteractionType interactionType)
    {
        return interactionType ==
               InteractionType.Use;
    }

    protected override bool CanInteractInternal(
        InteractionContext context)
    {
        return GameBootstrap.Instance != null &&
               GameBootstrap.Instance.UI != null;
    }

    protected override void OnInteract(
        InteractionContext context)
    {
        GameBootstrap.Instance.UI
            .OpenDevice(deviceKind);
    }

    protected override string
        GetDefaultInteractionPrompt(
            InteractionType interactionType)
    {
        string description =
            deviceKind == StoreDeviceKind.Desktop
                ? "Use Computer"
                : "Use Phone";

        return GameBootstrap.Instance != null
            ? GameBootstrap.Instance.Input
                .FormatPrompt(
                    GameplayAction.Use,
                    description)
            : "[E] " + description;
    }
}
