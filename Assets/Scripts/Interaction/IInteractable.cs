public interface IInteractable
{
    int InteractionPriority { get; }

    bool CanInteract(InteractionContext context);

    void Interact(InteractionContext context);
}
