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
