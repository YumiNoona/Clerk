using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed class CustomerSpawner : MonoBehaviour
{
    [Header("Data")]
    [SerializeField]
    private CustomerDatabase customerDatabase;

    [SerializeField]
    private PurchaseCatalog purchaseCatalog;

    [Header("Capacity")]
    [Min(1)]
    [SerializeField]
    private int maximumActiveCustomers = 8;

    [Min(0f)]
    [SerializeField]
    private float initialSpawnDelay = 2f;

    [Min(0.1f)]
    [SerializeField]
    private float minimumSpawnInterval = 6f;

    [Min(0.1f)]
    [SerializeField]
    private float maximumSpawnInterval = 12f;

    [Header("Scene Points")]
    [SerializeField]
    private CustomerSpawnPoint[] spawnPoints;

    [SerializeField]
    private CustomerEntrancePoint[] entrancePoints;

    [SerializeField]
    private CustomerExitPoint[] exitPoints;

    private readonly List<StockInfo> products =
        new List<StockInfo>();

    private Coroutine spawnRoutine;

    private void Awake()
    {
        ResolveScenePoints();
        BuildProductList();
    }

    private void OnEnable()
    {
        spawnRoutine =
            StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    public void Configure(
        CustomerDatabase database,
        PurchaseCatalog catalog)
    {
        customerDatabase = database;
        purchaseCatalog = catalog;
        BuildProductList();
    }

    public void ConfigureScenePoints(
        IReadOnlyList<CustomerSpawnPoint> configuredSpawns,
        CustomerEntrancePoint configuredEntrance,
        CustomerExitPoint configuredExit)
    {
        spawnPoints = configuredSpawns != null
            ? new List<CustomerSpawnPoint>(configuredSpawns).ToArray()
            : new CustomerSpawnPoint[0];

        entrancePoints = configuredEntrance != null
            ? new[] { configuredEntrance }
            : new CustomerEntrancePoint[0];

        exitPoints = configuredExit != null
            ? new[] { configuredExit }
            : new CustomerExitPoint[0];
    }

    public bool TrySpawnCustomer()
    {
        if (customerDatabase == null ||
            GameBootstrap.Instance == null ||
            !GameBootstrap.Instance.Days.IsDayRunning ||
            GameBootstrap.Instance.Customers.Count >=
                maximumActiveCustomers)
        {
            return false;
        }

        CustomerDefinition definition =
            customerDatabase.GetRandomCustomer();

        CustomerSpawnPoint spawnPoint =
            GetRandomEnabled(spawnPoints);

        CustomerEntrancePoint entrance =
            GetRandomEnabled(entrancePoints);

        CustomerExitPoint exit =
            GetRandomEnabled(exitPoints);

        if (definition == null ||
            definition.CustomerPrefab == null ||
            spawnPoint == null ||
            entrance == null ||
            exit == null)
        {
            return false;
        }

        CustomerShoppingPlan plan =
            CustomerShoppingPlanner.Create(
                products,
                definition);

        if (!TryResolveSpawnPosition(
                spawnPoint,
                out Vector3 spawnPosition))
        {
            return false;
        }

        GameObject customerObject =
            Instantiate(
                definition.CustomerPrefab,
                spawnPosition,
                spawnPoint.GetSpawnRotation());

        NavMeshAgent agent =
            customerObject.GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.radius = Mathf.Max(agent.radius,0.38f);
            agent.stoppingDistance =
                Mathf.Max(agent.stoppingDistance,0.22f);
            agent.avoidancePriority = Random.Range(25,76);
            agent.obstacleAvoidanceType =
                ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        CustomerContext context =
            customerObject.GetComponent<CustomerContext>();

        if (context == null)
        {
            context =
                customerObject.AddComponent<CustomerContext>();
        }

        CustomerBrain brain =
            customerObject.GetComponent<CustomerBrain>();

        if (brain == null)
        {
            brain =
                customerObject.AddComponent<CustomerBrain>();
        }

        if (!context.Initialize(
                definition,
                entrance,
                exit,
                plan))
        {
            Destroy(customerObject);
            return false;
        }

        brain.BeginLifecycle();
        return true;
    }

    private bool TryResolveSpawnPosition(
        CustomerSpawnPoint spawnPoint,
        out Vector3 position)
    {
        position = spawnPoint.Position;
        float radius = Mathf.Max(
            1.25f,
            spawnPoint.RandomSpawnRadius);

        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 offset = attempt == 0
                ? Vector2.zero
                : Random.insideUnitCircle * radius;

            Vector3 requested = spawnPoint.Position +
                new Vector3(offset.x,0f,offset.y);

            if (!NavMesh.SamplePosition(
                    requested,
                    out NavMeshHit hit,
                    1.5f,
                    NavMesh.AllAreas) ||
                !HasCustomerSeparation(hit.position,1.1f))
            {
                continue;
            }

            position = hit.position;
            return true;
        }

        return false;
    }

    private static bool HasCustomerSeparation(
        Vector3 position,
        float minimumDistance)
    {
        if (GameBootstrap.Instance == null)
        {
            return true;
        }

        IReadOnlyList<CustomerContext> active =
            GameBootstrap.Instance.Customers.Customers;

        for (int i = 0; i < active.Count; i++)
        {
            if (active[i] != null &&
                Vector3.Distance(
                    active[i].transform.position,
                    position) < minimumDistance)
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerator SpawnLoop()
    {
        if (initialSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(
                initialSpawnDelay);
        }

        while (enabled)
        {
            TrySpawnCustomer();

            float wait = Random.Range(
                minimumSpawnInterval,
                maximumSpawnInterval);

            yield return new WaitForSeconds(wait);
        }
    }

    private void BuildProductList()
    {
        products.Clear();

        if (purchaseCatalog != null &&
            purchaseCatalog.StockPurchases != null)
        {
            for (int i = 0;
                 i < purchaseCatalog.StockPurchases.Count;
                 i++)
            {
                StockPurchaseData purchase =
                    purchaseCatalog.StockPurchases[i];

                AddProduct(
                    purchase != null
                        ? purchase.Product
                        : null);
            }
        }

        if (products.Count == 0 &&
            GameBootstrap.Instance != null)
        {
            IReadOnlyList<ShelfSpaceController> shelves =
                GameBootstrap.Instance.Shelves.Shelves;

            for (int i = 0; i < shelves.Count; i++)
            {
                AddProduct(shelves[i].Info);
            }
        }
    }

    private void AddProduct(StockInfo product)
    {
        if (product != null &&
            !products.Contains(product))
        {
            products.Add(product);
        }
    }

    private void ResolveScenePoints()
    {
        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            spawnPoints =
                FindObjectsByType<CustomerSpawnPoint>(
                    FindObjectsInactive.Exclude);
        }

        if (entrancePoints == null ||
            entrancePoints.Length == 0)
        {
            entrancePoints =
                FindObjectsByType<CustomerEntrancePoint>(
                    FindObjectsInactive.Exclude);
        }

        if (exitPoints == null ||
            exitPoints.Length == 0)
        {
            exitPoints =
                FindObjectsByType<CustomerExitPoint>(
                    FindObjectsInactive.Exclude);
        }
    }

    private static T GetRandomEnabled<T>(T[] points)
        where T : CustomerPoint
    {
        if (points == null || points.Length == 0)
        {
            return null;
        }

        T selected = null;
        int enabledCount = 0;
        float totalWeight = 0f;

        for (int i = 0; i < points.Length; i++)
        {
            T point = points[i];

            if (point is CustomerSpawnPoint spawn &&
                !spawn.SpawningEnabled)
            {
                continue;
            }

            if (point is CustomerEntrancePoint entrance &&
                !entrance.EntranceEnabled)
            {
                continue;
            }

            if (point is CustomerExitPoint exit &&
                !exit.ExitEnabled)
            {
                continue;
            }

            if (point != null && point.isActiveAndEnabled)
            {
                if (point is CustomerSpawnPoint weightedSpawn)
                {
                    float weight = Mathf.Max(0f,weightedSpawn.SpawnWeight);
                    totalWeight += weight;
                    if (weight > 0f && Random.value * totalWeight <= weight)
                    {
                        selected = point;
                    }
                }
                else
                {
                    enabledCount++;
                    if (Random.Range(0,enabledCount) == 0)
                    {
                        selected = point;
                    }
                }
            }
        }

        return selected;
    }

    private void OnValidate()
    {
        maximumActiveCustomers =
            Mathf.Max(1,maximumActiveCustomers);

        initialSpawnDelay =
            Mathf.Max(0f,initialSpawnDelay);

        minimumSpawnInterval =
            Mathf.Max(0.1f,minimumSpawnInterval);

        maximumSpawnInterval =
            Mathf.Max(
                minimumSpawnInterval,
                maximumSpawnInterval);
    }
}
