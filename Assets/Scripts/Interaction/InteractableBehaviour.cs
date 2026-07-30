using UnityEngine;

public abstract class InteractableBehaviour :
    MonoBehaviour,
    IInteractable,
    IInteractionPromptProvider
{
    [Header("Interaction")]
    [SerializeField]
    private bool interactionEnabled = true;

    [SerializeField]
    private bool overrideInteractionPriority;

    [SerializeField]
    private int interactionPriority;

    [Header("Interaction Prompt")]
    [Tooltip("Leave a field empty to use the default text supplied by the component.")]
    [SerializeField]
    private string primaryPrompt;

    [SerializeField]
    private string secondaryPrompt;

    [SerializeField]
    private string usePrompt;

    [SerializeField]
    private string movePrompt;

    public int InteractionPriority
    {
        get
        {
            return overrideInteractionPriority
                ? interactionPriority
                : GetDefaultInteractionPriority();
        }
    }

    public bool InteractionEnabled => interactionEnabled;

    public bool CanInteract(InteractionContext context)
    {
        if (!interactionEnabled || !isActiveAndEnabled)
        {
            return false;
        }

        if (context.Player == null)
        {
            return false;
        }

        if (!SupportsInteraction(context.Type))
        {
            return false;
        }

        return CanInteractInternal(context);
    }

    public void Interact(InteractionContext context)
    {
        if (!CanInteract(context))
        {
            return;
        }

        OnInteract(context);
    }

    public string GetInteractionPrompt(
        InteractionContext context)
    {
        string customPrompt =
            GetCustomPrompt(context.Type);

        if (!string.IsNullOrWhiteSpace(customPrompt))
        {
            return customPrompt;
        }

        return GetDefaultInteractionPrompt(
            context.Type);
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
    }

    public void EnableInteraction()
    {
        interactionEnabled = true;
    }

    public void DisableInteraction()
    {
        interactionEnabled = false;
    }

    protected abstract bool SupportsInteraction(
        InteractionType interactionType);

    protected virtual bool CanInteractInternal(
        InteractionContext context)
    {
        return true;
    }

    protected abstract void OnInteract(
        InteractionContext context);

    protected virtual int GetDefaultInteractionPriority()
    {
        return 0;
    }

    protected virtual string GetDefaultInteractionPrompt(
        InteractionType interactionType)
    {
        return string.Empty;
    }

    private string GetCustomPrompt(
        InteractionType interactionType)
    {
        switch (interactionType)
        {
            case InteractionType.Primary:
                return primaryPrompt;

            case InteractionType.Secondary:
                return secondaryPrompt;

            case InteractionType.Use:
                return usePrompt;

            case InteractionType.Move:
                return movePrompt;

            default:
                return string.Empty;
        }
    }

    protected virtual void Reset()
    {
        interactionEnabled = true;
        overrideInteractionPriority = false;
        interactionPriority =
            GetDefaultInteractionPriority();
    }

    protected virtual void OnValidate()
    {
        interactionPriority = Mathf.Clamp(
            interactionPriority,
            -1000,
            1000);
    }
}
