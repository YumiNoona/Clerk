using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    [Header("References")]
    public Camera TheCamera;

    [Header("Interaction")]
    [Tooltip("Include Stock, Shelf, Stock Box, Garbage Bin and Furniture layers.")]
    public LayerMask InteractionMask =
        Physics.DefaultRaycastLayers;

    public float InteractionRange = 3f;

    [Header("Hold Points")]
    public Transform HoldPoint;
    public Transform BoxHoldPoint;
    public Transform MobileHoldPoint;

    [Header("Mobile Presentation")]
    public Vector3 MobileModelLocalPosition =
        new Vector3(0f,0.1f,0.035f);
    public Vector3 MobileModelLocalEulerAngles =
        new Vector3(-90f,180f,90f);
    [Min(0.01f)] public float MobileModelScale = 1.5f;
    public Vector3 MobileScreenLocalPosition =
        new Vector3(0f,0f,-0.045f);
    public Vector3 MobileScreenLocalEulerAngles = Vector3.zero;
    [Min(0.00001f)] public float MobileScreenScale = 0.0003f;
    public Vector2 MobileScreenSize = new Vector2(520f,900f);

    [Header("Debug")]
    public bool ShowInteractionDebug;

    private IHeldItem heldItem;

    private readonly List<InteractableCandidate> candidates =
        new List<InteractableCandidate>();

    private readonly StringBuilder promptBuilder =
        new StringBuilder();

    private int cachedPromptFrame = -1;
    private string cachedPrompt = string.Empty;
    private MonoBehaviour highlightedTarget;

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

    public event Action<MonoBehaviour>
        HighlightTargetChanged;

    private void Awake()
    {
        if (TheCamera == null)
        {
            TheCamera =
                GetComponentInChildren<Camera>();
        }

        if (MobileHoldPoint == null && TheCamera != null)
        {
            MobileHoldPoint = TheCamera.transform.Find("MobilePoint");
        }

        if (GetComponent<
                InteractionHighlightPresenter>() == null)
        {
            gameObject.AddComponent<
                InteractionHighlightPresenter>();
        }
    }

    private void Update()
    {
        if (!CanProcessInteraction() ||
            TheCamera == null)
        {
            SetHighlightedTarget(null);
            return;
        }

        Ray interactionRay =
            GetInteractionRay();

        if (heldItem != null)
        {
            SetHighlightedTarget(null);
            HandleHeldItem(interactionRay);
            return;
        }

        UpdateHighlightedTarget(interactionRay);
        HandleWorldInteraction(interactionRay);
    }

    private void UpdateHighlightedTarget(
        Ray interactionRay)
    {
        InteractableCandidate best = default;
        bool found = false;

        for (int i = 0;
             i < PromptInteractionTypes.Length;
             i++)
        {
            if (!TryGetBestCandidate(
                    interactionRay,
                    PromptInteractionTypes[i],
                    out InteractableCandidate candidate))
            {
                continue;
            }

            if (!found ||
                CompareCandidates(candidate,best) < 0)
            {
                best = candidate;
                found = true;
            }
        }

        SetHighlightedTarget(
            found ? best.Component : null);
    }

    private void SetHighlightedTarget(
        MonoBehaviour target)
    {
        if (highlightedTarget == target)
        {
            return;
        }

        highlightedTarget = target;
        HighlightTargetChanged?.Invoke(target);
    }

    private Ray GetInteractionRay()
    {
        return TheCamera.ViewportPointToRay(
            new Vector3(0.5f,0.5f,0f));
    }

    private bool CanProcessInteraction()
    {
        GameplayModeController modes =
            GameBootstrap.Instance != null
                ? GameBootstrap.Instance.GameplayModes
                : null;

        if (modes != null)
        {
            return modes.AllowsWorldInteraction;
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

        if (WasPressed(GameplayAction.Use) &&
            TryGetBestCandidate(
                interactionRay,
                InteractionType.Use,
                out InteractableCandidate candidate) &&
            candidate.Interactable is
                IHeldItemInteractionTarget)
        {
            candidate.Interactable.Interact(
                candidate.Context);

            cachedPromptFrame = -1;
            return;
        }

        heldItem.HandleHeldUpdate(
            this,
            interactionRay);
    }

    private void HandleWorldInteraction(
        Ray interactionRay)
    {
        if (WasPressed(GameplayAction.Primary))
        {
            TryInteract(
                interactionRay,
                InteractionType.Primary);
        }

        if (WasPressed(GameplayAction.Secondary))
        {
            TryInteract(
                interactionRay,
                InteractionType.Secondary);
        }

        if (WasPressed(GameplayAction.Use))
        {
            TryInteract(
                interactionRay,
                InteractionType.Use);
        }

        if (WasPressed(
                GameplayAction.MoveFurniture))
        {
            TryInteract(
                interactionRay,
                InteractionType.Move);
        }
    }

    public string GetCurrentPrompt()
    {
        if (cachedPromptFrame == Time.frameCount)
        {
            return cachedPrompt;
        }

        cachedPromptFrame = Time.frameCount;

        if (!CanProcessInteraction() ||
            TheCamera == null)
        {
            cachedPrompt = string.Empty;
            return cachedPrompt;
        }

        Ray interactionRay =
            GetInteractionRay();

        if (heldItem != null)
        {
            if (heldItem is UnityEngine.Object unityObject &&
                unityObject == null)
            {
                heldItem = null;
                cachedPrompt = string.Empty;
                return cachedPrompt;
            }

            promptBuilder.Clear();

            if (TryGetBestCandidate(
                    interactionRay,
                    InteractionType.Use,
                    out InteractableCandidate candidate) &&
                candidate.Interactable is
                    IHeldItemInteractionTarget &&
                candidate.Interactable is
                    IInteractionPromptProvider provider)
            {
                AppendUniquePrompt(
                    provider.GetInteractionPrompt(
                        candidate.Context));
            }

            AppendUniquePrompt(
                heldItem.GetHeldPrompt(
                    this,
                    interactionRay));

            cachedPrompt = promptBuilder.ToString();

            return cachedPrompt;
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

        cachedPrompt = promptBuilder.ToString();
        return cachedPrompt;
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

        cachedPromptFrame = -1;
        return true;
    }

    public bool WasPressed(GameplayAction action)
    {
        return GameBootstrap.Instance != null &&
               GameBootstrap.Instance.Input
                   .WasPressedThisFrame(action);
    }

    public bool IsPressed(GameplayAction action)
    {
        return GameBootstrap.Instance != null &&
               GameBootstrap.Instance.Input
                   .IsPressed(action);
    }

    public float ReadFloat(GameplayAction action)
    {
        return GameBootstrap.Instance != null
            ? GameBootstrap.Instance.Input
                .ReadFloat(action)
            : 0f;
    }

    public string FormatPrompt(
        GameplayAction action,
        string description)
    {
        return GameBootstrap.Instance != null
            ? GameBootstrap.Instance.Input
                .FormatPrompt(action,description)
            : "[" + action + "] " + description;
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
