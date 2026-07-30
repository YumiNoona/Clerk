using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionController : MonoBehaviour
{
    [Header("References")]
    public Camera TheCamera;

    [Header("Interaction")]
    [Tooltip("Include Stock, Shelf, StockBox and Furniture layers.")]
    public LayerMask InteractionMask =
        Physics.DefaultRaycastLayers;

    public float InteractionRange = 3f;

    [Header("Hold Points")]
    public Transform HoldPoint;
    public Transform BoxHoldPoint;

    [Header("Debug")]
    public bool ShowInteractionDebug;

    private IHeldItem heldItem;

    private readonly List<InteractableCandidate> candidates =
        new List<InteractableCandidate>();

    private readonly StringBuilder promptBuilder =
        new StringBuilder();

    private static readonly InteractionType[]
        PromptInteractionTypes =
        {
            InteractionType.Primary,
            InteractionType.Secondary,
            InteractionType.Use,
            InteractionType.Move
        };

    public IHeldItem HeldItem => heldItem;
    public bool IsHoldingAnything => heldItem != null;

    private void Awake()
    {
        if (TheCamera == null)
        {
            TheCamera =
                GetComponentInChildren<Camera>();
        }
    }

    private void Update()
    {
        if (!CanProcessInteraction() ||
            TheCamera == null)
        {
            return;
        }

        Ray interactionRay =
            GetInteractionRay();

        if (heldItem != null)
        {
            HandleHeldItem(interactionRay);
            return;
        }

        HandleWorldInteraction(interactionRay);
    }

    private Ray GetInteractionRay()
    {
        return TheCamera.ViewportPointToRay(
            new Vector3(0.5f,0.5f,0f));
    }

    private bool CanProcessInteraction()
    {
        if (UIController.Instance != null &&
            UIController.Instance.IsPricePanelOpen)
        {
            return false;
        }

        if (FurniturePlacementController.Instance != null &&
            FurniturePlacementController.Instance.IsPlacing)
        {
            return false;
        }

        return true;
    }

    private void HandleHeldItem(
        Ray interactionRay)
    {
        if (heldItem is UnityEngine.Object unityObject &&
            unityObject == null)
        {
            heldItem = null;
            return;
        }

        heldItem.HandleHeldUpdate(
            this,
            interactionRay);
    }

    private void HandleWorldInteraction(
        Ray interactionRay)
    {
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton
                .wasPressedThisFrame)
            {
                TryInteract(
                    interactionRay,
                    InteractionType.Primary);
            }

            if (Mouse.current.rightButton
                .wasPressedThisFrame)
            {
                TryInteract(
                    interactionRay,
                    InteractionType.Secondary);
            }
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.eKey
            .wasPressedThisFrame)
        {
            TryInteract(
                interactionRay,
                InteractionType.Use);
        }

        if (Keyboard.current.fKey
            .wasPressedThisFrame)
        {
            TryInteract(
                interactionRay,
                InteractionType.Move);
        }
    }

    public string GetCurrentPrompt()
    {
        if (!CanProcessInteraction() ||
            TheCamera == null)
        {
            return string.Empty;
        }

        Ray interactionRay =
            GetInteractionRay();

        if (heldItem != null)
        {
            if (heldItem is UnityEngine.Object unityObject &&
                unityObject == null)
            {
                heldItem = null;
                return string.Empty;
            }

            return heldItem.GetHeldPrompt(
                this,
                interactionRay);
        }

        promptBuilder.Clear();

        for (int i = 0;
             i < PromptInteractionTypes.Length;
             i++)
        {
            InteractionType interactionType =
                PromptInteractionTypes[i];

            if (!TryGetBestCandidate(
                    interactionRay,
                    interactionType,
                    out InteractableCandidate candidate))
            {
                continue;
            }

            if (!(candidate.Interactable is
                IInteractionPromptProvider provider))
            {
                continue;
            }

            string prompt =
                provider.GetInteractionPrompt(
                    candidate.Context);

            AppendUniquePrompt(prompt);
        }

        return promptBuilder.ToString();
    }

    private void AppendUniquePrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return;
        }

        string existing = promptBuilder.ToString();

        if (existing.Contains(prompt))
        {
            return;
        }

        if (promptBuilder.Length > 0)
        {
            promptBuilder.AppendLine();
        }

        promptBuilder.Append(prompt);
    }

    public bool TryInteract(
        Ray interactionRay,
        InteractionType interactionType)
    {
        if (!TryGetBestCandidate(
                interactionRay,
                interactionType,
                out InteractableCandidate candidate))
        {
            return false;
        }

        if (ShowInteractionDebug)
        {
            Debug.Log(
                "Interacting with " +
                candidate.Component.name +
                " using " +
                interactionType +
                ".",
                candidate.Component);
        }

        candidate.Interactable.Interact(
            candidate.Context);

        return true;
    }

    private bool TryGetBestCandidate(
        Ray interactionRay,
        InteractionType interactionType,
        out InteractableCandidate selectedCandidate)
    {
        candidates.Clear();

        RaycastHit[] hits = Physics.RaycastAll(
            interactionRay,
            InteractionRange,
            InteractionMask,
            QueryTriggerInteraction.Collide);

        Array.Sort(
            hits,
            (firstHit,secondHit) =>
                firstHit.distance.CompareTo(
                    secondHit.distance));

        for (int hitIndex = 0;
             hitIndex < hits.Length;
             hitIndex++)
        {
            AddCandidatesFromHit(
                hits[hitIndex],
                interactionRay,
                interactionType);
        }

        if (candidates.Count == 0)
        {
            selectedCandidate = default;
            return false;
        }

        candidates.Sort(CompareCandidates);
        selectedCandidate = candidates[0];

        return true;
    }

    private void AddCandidatesFromHit(
        RaycastHit hit,
        Ray interactionRay,
        InteractionType interactionType)
    {
        MonoBehaviour[] components =
            hit.collider
                .GetComponentsInParent<MonoBehaviour>(
                    true);

        for (int componentIndex = 0;
             componentIndex < components.Length;
             componentIndex++)
        {
            MonoBehaviour component =
                components[componentIndex];

            if (component == null ||
                !(component is IInteractable interactable))
            {
                continue;
            }

            if (ContainsCandidate(interactable))
            {
                continue;
            }

            InteractionContext context =
                new InteractionContext(
                    this,
                    interactionRay,
                    hit,
                    interactionType);

            if (!interactable.CanInteract(context))
            {
                continue;
            }

            candidates.Add(
                new InteractableCandidate(
                    interactable,
                    component,
                    context,
                    hit.distance));
        }
    }

    private bool ContainsCandidate(
        IInteractable interactable)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            if (ReferenceEquals(
                    candidates[i].Interactable,
                    interactable))
            {
                return true;
            }
        }

        return false;
    }

    private static int CompareCandidates(
        InteractableCandidate first,
        InteractableCandidate second)
    {
        int priorityComparison =
            second.Interactable
                .InteractionPriority
                .CompareTo(
                    first.Interactable
                        .InteractionPriority);

        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        return first.Distance.CompareTo(
            second.Distance);
    }

    public bool TryHold(IHeldItem item)
    {
        if (item == null ||
            heldItem != null ||
            !item.CanBeHeld)
        {
            return false;
        }

        Transform holdPoint =
            item.GetHoldPoint(this);

        if (holdPoint == null)
        {
            Debug.LogWarning(
                "No valid hold point is assigned for this item.",
                this);

            return false;
        }

        if (!item.Pickup(this,holdPoint))
        {
            return false;
        }

        heldItem = item;
        return true;
    }

    public void ClearHeldItem(IHeldItem item)
    {
        if (item == null)
        {
            return;
        }

        if (ReferenceEquals(heldItem,item))
        {
            heldItem = null;
        }
    }

    public void ForceDropHeldItem()
    {
        if (heldItem == null)
        {
            return;
        }

        IHeldItem itemToRelease = heldItem;
        heldItem = null;

        itemToRelease.ForceRelease(this);
    }

    public bool TryGetComponentInRay<T>(
        Ray interactionRay,
        out T component,
        out RaycastHit hit,
        QueryTriggerInteraction triggerInteraction =
            QueryTriggerInteraction.Collide)
        where T : Component
    {
        component = null;

        RaycastHit[] hits = Physics.RaycastAll(
            interactionRay,
            InteractionRange,
            InteractionMask,
            triggerInteraction);

        Array.Sort(
            hits,
            (firstHit,secondHit) =>
                firstHit.distance.CompareTo(
                    secondHit.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            T foundComponent =
                hits[i].collider
                    .GetComponentInParent<T>();

            if (foundComponent == null)
            {
                continue;
            }

            component = foundComponent;
            hit = hits[i];
            return true;
        }

        hit = default;
        return false;
    }

    private readonly struct InteractableCandidate
    {
        public IInteractable Interactable { get; }
        public MonoBehaviour Component { get; }
        public InteractionContext Context { get; }
        public float Distance { get; }

        public InteractableCandidate(
            IInteractable interactable,
            MonoBehaviour component,
            InteractionContext context,
            float distance)
        {
            Interactable = interactable;
            Component = component;
            Context = context;
            Distance = distance;
        }
    }
}
