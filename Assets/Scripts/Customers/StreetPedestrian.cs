using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CustomerNavigation))]
public sealed class StreetPedestrian : MonoBehaviour
{
    private CustomerNavigation navigation;
    private readonly List<Vector3> track =
        new List<Vector3>();
    private int targetIndex;
    private int direction;
    private float laneOffset;
    private float waitUntil;

    public void Configure(
        IReadOnlyList<Vector3> points,
        bool startTowardEnd)
    {
        navigation = GetComponent<CustomerNavigation>();
        track.Clear();
        track.AddRange(points);
        direction = startTowardEnd ? 1 : -1;
        targetIndex = startTowardEnd
            ? Mathf.Min(1,track.Count - 1)
            : Mathf.Max(0,track.Count - 2);
        laneOffset = Random.Range(-1.4f,1.4f);

        NavMeshAgent agent = navigation.Agent;
        agent.radius = Random.Range(0.34f,0.42f);
        agent.speed = Random.Range(1.5f,2.25f);
        agent.acceleration = 10f;
        agent.angularSpeed = 540f;
        agent.stoppingDistance = 0.3f;
        agent.avoidancePriority = Random.Range(10,91);
        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        MoveToNextEndpoint();
    }

    private void Awake()
    {
        navigation = GetComponent<CustomerNavigation>();
    }

    private void Update()
    {
        if (Time.time < waitUntil || navigation == null)
        {
            return;
        }

        if (navigation.HasReachedDestination ||
            navigation.MovementFailedFlag ||
            !navigation.HasDestination)
        {
            waitUntil = Time.time + Random.Range(0.25f,1.5f);
            AdvanceTrackTarget();
            MoveToNextEndpoint();
        }
    }

    private void MoveToNextEndpoint()
    {
        if (track.Count < 2)
        {
            return;
        }

        int previousIndex = Mathf.Clamp(
            targetIndex - direction,
            0,
            track.Count - 1);
        Vector3 route = track[targetIndex] - track[previousIndex];
        Vector3 sideways = route.sqrMagnitude > 0.01f
            ? Vector3.Cross(Vector3.up,route.normalized)
            : Vector3.right;

        Vector3 target =
            track[targetIndex] +
            sideways * laneOffset;

        navigation.MoveTo(target,0.3f);
    }

    private void AdvanceTrackTarget()
    {
        targetIndex += direction;

        if (targetIndex >= track.Count)
        {
            direction = -1;
            targetIndex = Mathf.Max(0,track.Count - 2);
        }
        else if (targetIndex < 0)
        {
            direction = 1;
            targetIndex = Mathf.Min(1,track.Count - 1);
        }
    }
}
