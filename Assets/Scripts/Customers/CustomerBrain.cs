using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CustomerContext))]
public sealed class CustomerBrain : MonoBehaviour
{
    private CustomerContext context;
    private Coroutine lifecycleRoutine;
    private ShelfReservation activeReservation;
    private bool movementCompleted;
    private bool movementFailed;

    private void Awake()
    {
        context = GetComponent<CustomerContext>();
    }

    public void BeginLifecycle()
    {
        if (lifecycleRoutine != null ||
            context == null ||
            !context.IsInitialized)
        {
            return;
        }

        lifecycleRoutine =
            StartCoroutine(RunLifecycle());
    }

    private IEnumerator RunLifecycle()
    {
        yield return EnterStore();

        if (context.PatienceRemaining > 0f)
        {
            yield return Shop();
        }

        if (context.Basket.ItemCount > 0 &&
            context.PatienceRemaining > 0f)
        {
            yield return Checkout();
        }

        yield return LeaveStore();

        lifecycleRoutine = null;
    }

    private IEnumerator EnterStore()
    {
        context.SetState(
            CustomerState.MovingToEntrance);

        if (context.Entrance != null)
        {
            yield return MoveTo(
                context.Entrance.Position,
                0.1f);

            if (!movementFailed)
            {
                context.SetState(
                    CustomerState.WaitingAtEntrance);

                float waitTime = Mathf.Max(
                    context.Entrance.GetRandomWaitTime(),
                    context.Definition
                        .GetRandomEntranceWaitTime());

                yield return WaitWithPatience(waitTime);

                if (context.Entrance.HasInsidePoint)
                {
                    yield return MoveTo(
                        context.Entrance.InsidePosition,
                        0.1f);
                }
            }
        }
    }

    private IEnumerator Shop()
    {
        context.SetState(CustomerState.Shopping);

        IReadOnlyList<CustomerShoppingRequest> requests =
            context.ShoppingPlan.Requests;

        for (int requestIndex = 0;
             requestIndex < requests.Count;
             requestIndex++)
        {
            CustomerShoppingRequest request =
                requests[requestIndex];

            while (!request.IsComplete &&
                   context.PatienceRemaining > 0f)
            {
                if (!TryReserveProduct(
                        request.Product,
                        out activeReservation))
                {
                    context.RecordUnavailableProduct();
                    break;
                }

                ShelfSpaceController shelf =
                    activeReservation.Shelf;

                yield return MoveTo(
                    shelf.CustomerStandingPosition,
                    shelf.CustomerStoppingDistance);

                if (movementFailed)
                {
                    shelf.ReleaseReservation(
                        activeReservation);

                    activeReservation = null;
                    context.RecordUnavailableProduct();
                    break;
                }

                context.Navigation.FacePoint(
                    shelf.transform.position,
                    true);

                context.Animator.SetBrowsing(true);

                yield return WaitWithPatience(
                    context.Definition
                        .GetRandomBrowseTime());

                context.Animator.SetBrowsing(false);

                if (GameBootstrap.Instance.Demand != null &&
                    !GameBootstrap.Instance.Demand
                        .WillPurchase(request.Product))
                {
                    shelf.ReleaseReservation(
                        activeReservation);

                    activeReservation = null;
                    context.RecordRejectedPrice();
                    break;
                }

                if (shelf.TryTakeReservedStock(
                        activeReservation,
                        out StockObject stockObject))
                {
                    request.RecordCollectedItem();

                    float price =
                        GameBootstrap.Instance.Products
                            .GetPrice(request.Product);

                    context.AddToBasket(
                        request.Product,
                        price);

                    context.Animator.TriggerPickup();

                    if (stockObject != null)
                    {
                        Destroy(stockObject.gameObject);
                    }
                }
                else
                {
                    shelf.ReleaseReservation(
                        activeReservation);

                    context.RecordUnavailableProduct();
                }

                activeReservation = null;
            }
        }
    }

    private bool TryReserveProduct(
        StockInfo product,
        out ShelfReservation reservation)
    {
        if (GameBootstrap.Instance == null)
        {
            reservation = null;
            return false;
        }

        return GameBootstrap.Instance.Shelves
            .TryReserveNearest(
                context.CustomerId,
                product,
                1,
                transform.position,
                out reservation);
    }

    private IEnumerator Checkout()
    {
        context.SetState(
            CustomerState.MovingToCheckout);

        CheckoutCounter counter = null;

        while (counter == null &&
               context.PatienceRemaining > 0f)
        {
            counter =
                GameBootstrap.Instance.Checkouts
                    .FindBestCounter();

            if (counter != null &&
                !counter.TryEnqueue(context))
            {
                counter = null;
            }

            if (counter == null)
            {
                yield return WaitWithPatience(1f);
            }
        }

        if (counter == null)
        {
            yield break;
        }

        context.SetState(
            CustomerState.WaitingInCheckoutQueue);

        while (!context.CheckoutCompleted &&
               context.PatienceRemaining > 0f &&
               context.AssignedCheckout == counter)
        {
            Vector3 queuePosition =
                counter.GetQueuePosition(context);

            if (!context.Navigation.IsAtPosition(
                    queuePosition,
                    0.15f))
            {
                yield return MoveTo(
                    queuePosition,
                    0.1f);

                if (movementFailed)
                {
                    break;
                }
            }

            if (counter.IsFirst(context))
            {
                context.SetState(
                    CustomerState.CheckingOut);

                counter.NotifyCustomerReady(context);
                context.Animator.SetCheckingOut(true);
            }

            context.TickPatience(Time.deltaTime);
            yield return null;
        }

        context.Animator.SetCheckingOut(false);

        if (!context.CheckoutCompleted)
        {
            counter.RemoveCustomer(context);
        }
    }

    private IEnumerator LeaveStore()
    {
        context.SetState(
            CustomerState.MovingToExit);

        if (context.Exit != null)
        {
            yield return MoveTo(
                context.Exit.Position,
                0.1f);

            if (!movementFailed &&
                context.Exit.HasDespawnPoint)
            {
                yield return MoveTo(
                    context.Exit.DespawnPosition,
                    0.1f);
            }
        }

        context.SetState(
            CustomerState.Despawning);

        Destroy(gameObject);
    }

    private IEnumerator MoveTo(
        Vector3 destination,
        float stoppingDistance)
    {
        movementCompleted = false;
        movementFailed = false;

        context.Navigation.DestinationReached +=
            HandleDestinationReached;

        context.Navigation.MovementFailed +=
            HandleMovementFailed;

        bool started =
            context.Navigation.MoveTo(
                destination,
                stoppingDistance);

        if (!started)
        {
            movementFailed = true;
        }

        while (!movementCompleted &&
               !movementFailed)
        {
            context.TickPatience(Time.deltaTime);
            yield return null;
        }

        context.Navigation.DestinationReached -=
            HandleDestinationReached;

        context.Navigation.MovementFailed -=
            HandleMovementFailed;
    }

    private IEnumerator WaitWithPatience(float duration)
    {
        float remaining = Mathf.Max(0f,duration);

        while (remaining > 0f)
        {
            float delta = Time.deltaTime;
            remaining -= delta;
            context.TickPatience(delta);
            yield return null;
        }
    }

    private void HandleDestinationReached()
    {
        movementCompleted = true;
    }

    private void HandleMovementFailed()
    {
        movementFailed = true;
    }

    private void OnDisable()
    {
        if (context != null &&
            context.Navigation != null)
        {
            context.Navigation.DestinationReached -=
                HandleDestinationReached;

            context.Navigation.MovementFailed -=
                HandleMovementFailed;
        }

        if (activeReservation != null &&
            activeReservation.Shelf != null)
        {
            activeReservation.Shelf
                .ReleaseReservation(
                    activeReservation);
        }

        if (context != null &&
            context.AssignedCheckout != null &&
            !context.CheckoutCompleted)
        {
            context.AssignedCheckout
                .RemoveCustomer(context);
        }
    }

}
