using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EmployeeContext))]
public sealed class RestockEmployeeBrain :
    MonoBehaviour
{
    private EmployeeContext context;
    private StockBoxController currentBox;
    private ShelfSpaceController currentShelf;
    private Coroutine workRoutine;

    private void Awake()
    {
        context = GetComponent<EmployeeContext>();
    }

    private void OnEnable()
    {
        workRoutine =
            StartCoroutine(WorkLoop());
    }

    private void OnDisable()
    {
        if (workRoutine != null)
        {
            StopCoroutine(workRoutine);
        }

        ReleaseJob();
    }

    private IEnumerator WorkLoop()
    {
        while (enabled)
        {
            if (!TryFindJob())
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            yield return MoveTo(
                currentBox.transform.position,
                0.8f);

            if (currentBox == null ||
                currentShelf == null)
            {
                ReleaseJob();
                continue;
            }

            currentBox.SetOpen(true);

            yield return MoveTo(
                currentShelf.CustomerStandingPosition,
                currentShelf.CustomerStoppingDistance);

            while (currentBox != null &&
                   currentShelf != null &&
                   currentBox.Quantity > 0 &&
                   currentShelf.CanAcceptProduct(
                       currentBox.Product))
            {
                if (!currentBox.TryStockShelf(
                        currentShelf))
                {
                    break;
                }

                float interval =
                    context.Definition != null
                        ? context.Definition.WorkInterval
                        : 0.4f;

                yield return new WaitForSeconds(interval);
            }

            if (currentBox != null)
            {
                currentBox.SetOpen(false);
            }

            ReleaseJob();
        }
    }

    private bool TryFindJob()
    {
        if (GameBootstrap.Instance == null)
        {
            return false;
        }

        StockBoxController[] boxes =
            FindObjectsByType<StockBoxController>(
                FindObjectsInactive.Exclude);

        for (int boxIndex = 0;
             boxIndex < boxes.Length;
             boxIndex++)
        {
            StockBoxController box = boxes[boxIndex];

            if (box == null ||
                box.Product == null ||
                box.Quantity <= 0 ||
                box.IsHeld)
            {
                continue;
            }

            ShelfSpaceController shelf =
                FindShelfFor(box.Product);

            if (shelf == null ||
                !GameBootstrap.Instance.Employees
                    .TryClaimBox(box))
            {
                continue;
            }

            currentBox = box;
            currentShelf = shelf;
            return true;
        }

        return false;
    }

    private ShelfSpaceController FindShelfFor(
        StockInfo product)
    {
        var shelves =
            GameBootstrap.Instance.Shelves.Shelves;

        ShelfSpaceController best = null;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < shelves.Count; i++)
        {
            ShelfSpaceController shelf = shelves[i];

            if (shelf == null ||
                !shelf.CanAcceptProduct(product))
            {
                continue;
            }

            float distance =
                (shelf.transform.position -
                 transform.position).sqrMagnitude;

            if (distance < bestDistance)
            {
                best = shelf;
                bestDistance = distance;
            }
        }

        return best;
    }

    private IEnumerator MoveTo(
        Vector3 destination,
        float stoppingDistance)
    {
        NavMeshAgent agent = context.Agent;

        if (agent == null ||
            !agent.isOnNavMesh)
        {
            yield break;
        }

        agent.stoppingDistance =
            Mathf.Max(0.1f,stoppingDistance);

        if (!agent.SetDestination(destination))
        {
            yield break;
        }

        while (agent.pathPending ||
               agent.remainingDistance >
               agent.stoppingDistance + 0.1f)
        {
            if (!agent.hasPath &&
                !agent.pathPending)
            {
                yield break;
            }

            yield return null;
        }
    }

    private void ReleaseJob()
    {
        GameBootstrap.Instance?.Employees
            .ReleaseBox(currentBox);

        currentBox = null;
        currentShelf = null;
    }
}
