using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class CheckoutCounter :
    InteractableBehaviour
{
    [Header("Identity")]
    [SerializeField]
    private string checkoutId;

    [Header("Queue")]
    [SerializeField]
    private bool isOpen = true;

    [Min(1)]
    [SerializeField]
    private int fallbackQueueCapacity = 4;

    [Min(0.25f)]
    [SerializeField]
    private float fallbackQueueSpacing = 1f;

    [SerializeField]
    private List<Transform> queuePoints =
        new List<Transform>();

    [Header("Display")]
    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private string currencySymbol = "$";

    private readonly List<CustomerContext> queue =
        new List<CustomerContext>();

    private CheckoutSession activeSession;

    public string CheckoutId => checkoutId;
    public bool IsOpen => isOpen;
    public int QueueCount => queue.Count;
    public int QueueCapacity =>
        queuePoints != null && queuePoints.Count > 0
            ? queuePoints.Count
            : fallbackQueueCapacity;

    public CheckoutSession ActiveSession =>
        activeSession;

    public event Action<CheckoutCounter> QueueChanged;
    public event Action<CheckoutSession> SessionStarted;
    public event Action<CheckoutSession> SessionCompleted;

    private void Awake()
    {
        EnsureId();
        UpdateDisplay();
    }

    private void OnEnable()
    {
        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Checkouts
                .Register(this);
        }
    }

    private void OnDisable()
    {
        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Checkouts
                .Unregister(this);
        }

        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i] != null)
            {
                queue[i].NotifyCheckoutUnavailable(this);
            }
        }

        queue.Clear();
        activeSession = null;
    }

    public bool TryEnqueue(CustomerContext customer)
    {
        if (!isOpen ||
            customer == null ||
            queue.Contains(customer) ||
            queue.Count >= QueueCapacity)
        {
            return false;
        }

        queue.Add(customer);
        customer.AssignCheckout(this);
        QueueChanged?.Invoke(this);
        UpdateDisplay();
        return true;
    }

    public bool IsFirst(CustomerContext customer)
    {
        RemoveMissingCustomers();
        return queue.Count > 0 &&
               queue[0] == customer;
    }

    public bool RemoveCustomer(CustomerContext customer)
    {
        if (customer == null)
        {
            return false;
        }

        bool removed = queue.Remove(customer);

        if (activeSession != null &&
            activeSession.Customer == customer)
        {
            activeSession.ItemScanned -=
                HandleSessionUpdated;

            activeSession = null;
            removed = true;
        }

        if (!removed)
        {
            return false;
        }

        customer.NotifyCheckoutUnavailable(this);
        QueueChanged?.Invoke(this);
        UpdateDisplay();
        return true;
    }

    public Vector3 GetQueuePosition(
        CustomerContext customer)
    {
        RemoveMissingCustomers();
        int index = queue.IndexOf(customer);

        if (index < 0)
        {
            return transform.position;
        }

        if (queuePoints != null &&
            index < queuePoints.Count &&
            queuePoints[index] != null)
        {
            return queuePoints[index].position;
        }

        return transform.position -
               transform.forward *
               (fallbackQueueSpacing * (index + 1));
    }

    public bool NotifyCustomerReady(
        CustomerContext customer)
    {
        if (activeSession != null ||
            !IsFirst(customer))
        {
            return false;
        }

        activeSession =
            new CheckoutSession(customer);

        activeSession.ItemScanned +=
            HandleSessionUpdated;

        SessionStarted?.Invoke(activeSession);
        UpdateDisplay();
        return true;
    }

    public bool ScanNextItem()
    {
        return activeSession != null &&
               activeSession.TryScanNext();
    }

    public bool CompletePayment()
    {
        if (activeSession == null ||
            !activeSession.AllItemsScanned ||
            GameBootstrap.Instance == null)
        {
            return false;
        }

        CheckoutSession completedSession =
            activeSession;

        bool saleRecorded =
            GameBootstrap.Instance.Economy
                .RecordSale(
                    completedSession.Customer.Basket,
                    checkoutId);

        if (!saleRecorded ||
            !completedSession.TryComplete())
        {
            return false;
        }

        completedSession.ItemScanned -=
            HandleSessionUpdated;

        activeSession = null;
        queue.Remove(completedSession.Customer);

        GameBootstrap.Instance.Progression
            .AddExperience(
                Mathf.Max(
                    5,
                    completedSession.TotalItemCount *
                    5));

        float reputationGain =
            1f -
            completedSession.Customer
                .UnavailableProductCount * 0.15f -
            completedSession.Customer
                .RejectedPriceCount * 0.1f;

        GameBootstrap.Instance.Progression
            .AddReputation(
                Mathf.Clamp(
                    reputationGain,
                    -1f,
                    1f));

        completedSession.Customer
            .NotifyCheckoutCompleted(this);

        SessionCompleted?.Invoke(completedSession);
        QueueChanged?.Invoke(this);
        UpdateDisplay();
        return true;
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        UpdateDisplay();
    }

    protected override int GetDefaultInteractionPriority()
    {
        return 50;
    }

    protected override bool SupportsInteraction(
        InteractionType interactionType)
    {
        return interactionType ==
                   InteractionType.Primary ||
               interactionType ==
                   InteractionType.Use;
    }

    protected override bool CanInteractInternal(
        InteractionContext context)
    {
        if (activeSession == null)
        {
            return false;
        }

        if (context.Type ==
            InteractionType.Primary)
        {
            return !activeSession.AllItemsScanned;
        }

        return activeSession.AllItemsScanned;
    }

    protected override void OnInteract(
        InteractionContext context)
    {
        if (context.Type ==
            InteractionType.Primary)
        {
            ScanNextItem();
        }
        else if (context.Type ==
                 InteractionType.Use)
        {
            CompletePayment();
        }
    }

    protected override string
        GetDefaultInteractionPrompt(
            InteractionType interactionType)
    {
        if (GameBootstrap.Instance == null)
        {
            return interactionType ==
                   InteractionType.Primary
                ? "[Left Click] Scan Item"
                : "[E] Take Payment";
        }

        return interactionType ==
               InteractionType.Primary
            ? GameBootstrap.Instance.Input.FormatPrompt(
                GameplayAction.Primary,
                "Scan Item")
            : GameBootstrap.Instance.Input.FormatPrompt(
                GameplayAction.Use,
                "Take Payment");
    }

    private void HandleSessionUpdated(
        CheckoutSession session)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (statusText == null)
        {
            return;
        }

        if (!isOpen)
        {
            statusText.text = "CLOSED";
            return;
        }

        if (activeSession == null)
        {
            statusText.text =
                queue.Count > 0
                    ? "NEXT CUSTOMER"
                    : "OPEN";

            return;
        }

        statusText.text =
            activeSession.ScannedItemCount +
            " / " +
            activeSession.TotalItemCount +
            "\n" +
            currencySymbol +
            activeSession.Total.ToString("0.00");
    }

    private void RemoveMissingCustomers()
    {
        int oldCount = queue.Count;

        queue.RemoveAll(
            customer => customer == null);

        if (oldCount != queue.Count)
        {
            QueueChanged?.Invoke(this);
        }
    }

    private void EnsureId()
    {
        if (string.IsNullOrWhiteSpace(checkoutId))
        {
            checkoutId =
                Guid.NewGuid().ToString("N");
        }
    }

    public void RegeneratePersistentId()
    {
        checkoutId =
            Guid.NewGuid().ToString("N");
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        fallbackQueueCapacity =
            Mathf.Max(1,fallbackQueueCapacity);

        fallbackQueueSpacing =
            Mathf.Max(0.25f,fallbackQueueSpacing);

        EnsureId();
    }
}
