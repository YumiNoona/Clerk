using UnityEngine;

public interface IHeldItem
{
    bool CanBeHeld { get; }

    Transform GetHoldPoint(PlayerInteractionController player);

    bool Pickup(
        PlayerInteractionController player,
        Transform holdPoint);

    void HandleHeldUpdate(
        PlayerInteractionController player,
        Ray interactionRay);

    string GetHeldPrompt(
        PlayerInteractionController player,
        Ray interactionRay);

    void ForceRelease(PlayerInteractionController player);
}

/// <summary>
/// Marks a world interactable that remains usable while the player is
/// carrying an item. Keep this interface narrow so ordinary world
/// interactions cannot accidentally run during held-item input.
/// </summary>
public interface IHeldItemInteractionTarget
{
}
