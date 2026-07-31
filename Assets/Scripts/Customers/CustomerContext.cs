using System;
using UnityEngine;

[RequireComponent(typeof(CustomerNavigation))]
[RequireComponent(typeof(CustomerAnimator))]
public sealed class CustomerContext : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private CustomerNavigation navigation;

    [SerializeField]
    private CustomerAnimator customerAnimator;

    [SerializeField]
    private CustomerVisualVariation visualVariation;

    [SerializeField]
    private GameObject shoppingBag;

    public string CustomerId { get; private set; }
    public CustomerDefinition Definition { get; private set; }
    public CustomerEntrancePoint Entrance { get; private set; }
    public CustomerExitPoint Exit { get; private set; }
    public CustomerNavigation Navigation => navigation;
    public CustomerAnimator Animator => customerAnimator;
    public CustomerBasket Basket { get; } =
        new CustomerBasket();

    public CustomerShoppingPlan ShoppingPlan
    {
        get;
        private set;
    }

    public CustomerState State { get; private set; }
    public float PatienceRemaining { get; private set; }
    public int UnavailableProductCount { get; private set; }
    public int RejectedPriceCount { get; private set; }
    public bool IsInitialized { get; private set; }
    public CheckoutCounter AssignedCheckout { get; private set; }
    public bool CheckoutCompleted { get; private set; }

    public event Action<CustomerContext,CustomerState>
        StateChanged;

    public event Action<CustomerContext>
        BasketChanged;

    public event Action<CustomerContext>
        CheckoutAssignmentChanged;

    public event Action<CustomerContext>
        CheckoutFinished;

    private void Awake()
    {
        CacheComponents();
        SetShoppingBagVisible(false);
    }

    public bool Initialize(
        CustomerDefinition definition,
        CustomerEntrancePoint entrance,
        CustomerExitPoint exit,
        CustomerShoppingPlan shoppingPlan)
    {
        if (definition == null ||
            navigation == null ||
            customerAnimator == null)
        {
            return false;
        }

        CustomerId = Guid.NewGuid().ToString("N");
        Definition = definition;
        Entrance = entrance;
        Exit = exit;
        ShoppingPlan =
            shoppingPlan ??
            new CustomerShoppingPlan();

        PatienceRemaining =
            definition.GetRandomPatience();

        navigation.SetMovementSpeed(
            definition.GetRandomWalkSpeed());

        customerAnimator.ApplyOverrideController(
            definition.AnimatorOverrideController);

        if (visualVariation != null)
        {
            visualVariation.ApplyRandomSkin();
        }

        transform.localScale =
            Vector3.one *
            definition.GetRandomScale();

        CustomerMoodPresenter moodPresenter =
            GetComponent<CustomerMoodPresenter>();

        if (moodPresenter == null)
        {
            moodPresenter =
                gameObject.AddComponent<CustomerMoodPresenter>();
        }

        moodPresenter.Initialize(this);

        IsInitialized = true;

        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Customers
                .Register(this);
        }

        SetState(CustomerState.Spawning);
        return true;
    }

    public void SetState(CustomerState state)
    {
        if (State == state)
        {
            return;
        }

        State = state;
        StateChanged?.Invoke(this,State);
    }

    public void TickPatience(float deltaTime)
    {
        PatienceRemaining =
            Mathf.Max(
                0f,
                PatienceRemaining - deltaTime);
    }

    public void RecordUnavailableProduct()
    {
        UnavailableProductCount++;

        if (Definition != null)
        {
            PatienceRemaining =
                Mathf.Max(
                    0f,
                    PatienceRemaining -
                    Definition.UnavailableProductPenalty);
        }

        customerAnimator.TriggerReaction();
    }

    public void RecordRejectedPrice()
    {
        RejectedPriceCount++;

        if (Definition != null)
        {
            PatienceRemaining =
                Mathf.Max(
                    0f,
                    PatienceRemaining -
                    Definition.PriceRejectionPenalty);
        }

        customerAnimator.TriggerReaction();
    }

    public void AddToBasket(
        StockInfo product,
        float unitPrice)
    {
        Basket.Add(product,1,unitPrice);
        SetShoppingBagVisible(Basket.ItemCount > 0);
        BasketChanged?.Invoke(this);
    }

    public void SetShoppingBagVisible(bool visible)
    {
        if (shoppingBag != null)
        {
            shoppingBag.SetActive(visible);
        }
    }

    public void AssignCheckout(CheckoutCounter checkout)
    {
        AssignedCheckout = checkout;
        CheckoutCompleted = false;
        CheckoutAssignmentChanged?.Invoke(this);
    }

    public void NotifyCheckoutUnavailable(
        CheckoutCounter checkout)
    {
        if (AssignedCheckout != checkout)
        {
            return;
        }

        AssignedCheckout = null;
        CheckoutAssignmentChanged?.Invoke(this);
    }

    public void NotifyCheckoutCompleted(
        CheckoutCounter checkout)
    {
        if (AssignedCheckout != checkout)
        {
            return;
        }

        CheckoutCompleted = true;
        AssignedCheckout = null;
        CheckoutFinished?.Invoke(this);
        CheckoutAssignmentChanged?.Invoke(this);
    }

    private void CacheComponents()
    {
        if (navigation == null)
        {
            navigation =
                GetComponent<CustomerNavigation>();
        }

        if (customerAnimator == null)
        {
            customerAnimator =
                GetComponent<CustomerAnimator>();
        }

        if (visualVariation == null)
        {
            visualVariation =
                GetComponent<CustomerVisualVariation>();
        }

        if (shoppingBag == null)
        {
            Transform[] children =
                GetComponentsInChildren<Transform>(
                    true);

            for (int i = 0;
                 i < children.Length;
                 i++)
            {
                if (children[i] != transform &&
                    children[i].name.IndexOf(
                        "Shopping Bag",
                        StringComparison.OrdinalIgnoreCase)
                    >= 0)
                {
                    shoppingBag =
                        children[i].gameObject;
                    break;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Customers
                .Unregister(this);
        }
    }
}
