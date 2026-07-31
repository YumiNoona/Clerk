using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ScenePose
{
    public Vector3 Position;
    public Vector3 EulerAngles;

    public Quaternion Rotation => Quaternion.Euler(EulerAngles);
}

[Serializable]
public sealed class WeightedCustomerSpawn
{
    public ScenePose Pose;
    [Min(0f)] public float Weight = 1f;
    [Min(0f)] public float Radius;
}

[DisallowMultipleComponent]
public sealed class StoreSceneConfiguration : MonoBehaviour
{
    [Header("Customer Spawning")]
    [SerializeField] private List<WeightedCustomerSpawn> customerSpawns =
        new List<WeightedCustomerSpawn>();

    [Header("Store Route")]
    [SerializeField] private ScenePose entranceWaitPoint;
    [SerializeField] private ScenePose insidePoint;
    [SerializeField,Min(0f)] private float entranceWaitMinimum = 0.5f;
    [SerializeField,Min(0f)] private float entranceWaitMaximum = 2f;
    [SerializeField] private CheckoutCounter checkout;
    [SerializeField] private List<ScenePose> checkoutQueue =
        new List<ScenePose>();
    [SerializeField] private ScenePose exitPoint;
    [SerializeField] private ScenePose despawnPoint;

    [Header("Pedestrian Track")]
    [Tooltip("Pedestrians are disabled until this track contains at least two separated points.")]
    [SerializeField] private List<Vector3> pedestrianTrack =
        new List<Vector3>();

    private readonly List<Vector3> pedestrianWorldTrack =
        new List<Vector3>();
    private readonly List<CustomerSpawnPoint> runtimeSpawns =
        new List<CustomerSpawnPoint>();
    private CustomerEntrancePoint runtimeEntrance;
    private CustomerExitPoint runtimeExit;
    private GameObject runtimePoints;

    [SerializeField,HideInInspector]
    private bool legacyHierarchyMigrated;

    public IReadOnlyList<Vector3> PedestrianTrack => pedestrianWorldTrack;
    public IReadOnlyList<CustomerSpawnPoint> RuntimeSpawns => runtimeSpawns;
    public CustomerEntrancePoint RuntimeEntrance => runtimeEntrance;
    public CustomerExitPoint RuntimeExit => runtimeExit;

    public bool HasPedestrianTrack
    {
        get
        {
            RebuildPedestrianWorldTrack();
            float length = 0f;

            for (int i = 1; i < pedestrianWorldTrack.Count; i++)
            {
                length += Vector3.Distance(
                    pedestrianWorldTrack[i - 1],
                    pedestrianWorldTrack[i]);
            }

            return pedestrianWorldTrack.Count >= 2 && length >= 2f;
        }
    }

    private void Awake()
    {
        SynchronizeRuntimePoints();
    }

    public void SynchronizeRuntimePoints()
    {
        ClearRuntimePoints();
        runtimeSpawns.Clear();
        runtimeEntrance = null;
        runtimeExit = null;
        runtimePoints = new GameObject("Runtime Customer Points");
        // Hide only the temporary container from the authored hierarchy.
        // DontSave objects are excluded by Unity object searches, which
        // prevented CustomerSpawner from discovering these point adapters.
        runtimePoints.hideFlags = HideFlags.HideInHierarchy;

        for (int i = 0; i < customerSpawns.Count; i++)
        {
            WeightedCustomerSpawn entry = customerSpawns[i];
            if (entry == null || entry.Weight <= 0f)
            {
                continue;
            }

            Transform point = CreateRuntimePoint(
                "Customer Spawn " + (i + 1),entry.Pose);
            CustomerSpawnPoint spawn = point.gameObject.AddComponent<CustomerSpawnPoint>();
            spawn.Configure(entry.Weight,entry.Radius);
            runtimeSpawns.Add(spawn);
        }

        Transform entranceTransform = CreateRuntimePoint("Entrance",entranceWaitPoint);
        Transform insideTransform = CreateRuntimePoint("Inside",insidePoint);
        CustomerEntrancePoint entrance =
            entranceTransform.gameObject.AddComponent<CustomerEntrancePoint>();
        entrance.Configure(
            insideTransform,
            entranceWaitMinimum,
            entranceWaitMaximum);
        runtimeEntrance = entrance;

        Transform exitTransform = CreateRuntimePoint("Exit",exitPoint);
        Transform despawnTransform = CreateRuntimePoint("Despawn",despawnPoint);
        CustomerExitPoint exit =
            exitTransform.gameObject.AddComponent<CustomerExitPoint>();
        exit.Configure(despawnTransform);
        runtimeExit = exit;

        if (checkout != null)
        {
            List<Transform> queue = new List<Transform>();

            for (int i = 0; i < checkoutQueue.Count; i++)
            {
                queue.Add(CreateRuntimePoint(
                    "Checkout Queue " + (i + 1),
                    checkoutQueue[i]));
            }

            checkout.ConfigureQueuePoints(queue);
        }

        RebuildPedestrianWorldTrack();
    }

    private Transform CreateRuntimePoint(string pointName, ScenePose pose)
    {
        GameObject point = new GameObject(pointName);
        point.hideFlags = HideFlags.None;
        point.transform.SetParent(runtimePoints.transform,false);
        point.transform.SetPositionAndRotation(
            transform.TransformPoint(pose.Position),
            transform.rotation * pose.Rotation);
        return point.transform;
    }

    private void RebuildPedestrianWorldTrack()
    {
        pedestrianWorldTrack.Clear();

        for (int i = 0; i < pedestrianTrack.Count; i++)
        {
            pedestrianWorldTrack.Add(
                transform.TransformPoint(pedestrianTrack[i]));
        }
    }

    private void ClearRuntimePoints()
    {
        if (runtimePoints == null)
        {
            return;
        }

        runtimePoints.SetActive(false);

        if (Application.isPlaying)
        {
            Destroy(runtimePoints);
        }
        else
        {
            DestroyImmediate(runtimePoints);
        }

        runtimePoints = null;
    }

    private void OnDestroy()
    {
        ClearRuntimePoints();
    }

    private void OnValidate()
    {
        entranceWaitMinimum = Mathf.Max(0f,entranceWaitMinimum);
        entranceWaitMaximum = Mathf.Max(entranceWaitMinimum,entranceWaitMaximum);

        for (int i = 0; i < customerSpawns.Count; i++)
        {
            if (customerSpawns[i] != null)
            {
                customerSpawns[i].Weight = Mathf.Max(0f,customerSpawns[i].Weight);
                customerSpawns[i].Radius = Mathf.Max(0f,customerSpawns[i].Radius);
            }
        }
    }
}
