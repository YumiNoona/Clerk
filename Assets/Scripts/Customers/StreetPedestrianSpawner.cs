using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed class StreetPedestrianSpawner : MonoBehaviour
{
    [SerializeField,Min(0)]
    private int pedestrianCount = 8;

    [SerializeField,Min(0.1f)]
    private float spawnInterval = 0.35f;

    private CustomerDatabase database;
    private readonly List<Vector3> track =
        new List<Vector3>();

    public void Configure(
        CustomerDatabase customerDatabase,
        IReadOnlyList<Vector3> trackPoints)
    {
        database = customerDatabase;
        track.Clear();

        if (trackPoints == null)
        {
            return;
        }

        for (int i = 0; i < trackPoints.Count; i++)
        {
            track.Add(trackPoints[i]);
        }
    }

    private IEnumerator Start()
    {
        yield return null;

        for (int i = 0; i < pedestrianCount; i++)
        {
            SpawnPedestrian(i);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnPedestrian(int index)
    {
        CustomerDefinition definition =
            database != null
                ? database.GetRandomCustomer()
                : null;

        if (definition == null ||
            definition.CustomerPrefab == null)
        {
            return;
        }

        if (track.Count < 2)
        {
            return;
        }

        int segment = index % (track.Count - 1);
        Vector3 requested = Vector3.Lerp(
            track[segment],
            track[segment + 1],
            Random.Range(0.1f,0.9f));

        if (!TrySampleStreetPosition(
                requested,
                out NavMeshHit hit))
        {
            return;
        }

        GameObject pedestrian = Instantiate(
            definition.CustomerPrefab,
            hit.position,
            Quaternion.identity,
            transform);
        pedestrian.name = "Pedestrian " + (index + 1);
        pedestrian.GetComponent<CustomerVisualVariation>()
            ?.ApplyRandomVariation();

        StreetPedestrian walker =
            pedestrian.GetComponent<StreetPedestrian>() ??
            pedestrian.AddComponent<StreetPedestrian>();
        walker.Configure(track,index % 2 == 0);
    }

    private bool TrySampleStreetPosition(
        Vector3 requested,
        out NavMeshHit hit)
    {
        if (!NavMesh.SamplePosition(
                requested,
                out hit,
                1.25f,
                NavMesh.AllAreas))
        {
            return false;
        }

        // The indoor and sidewalk surfaces are close near the storefront.
        // Reject a nearest-point result that jumped away from the street
        // corridor instead of spawning a pedestrian inside the shop.
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < track.Count - 1; i++)
        {
            Vector3 start = track[i];
            Vector3 route = track[i + 1] - start;
            route.y = 0f;

            if (route.sqrMagnitude < 0.01f)
            {
                continue;
            }

            Vector3 fromStart = hit.position - start;
            fromStart.y = 0f;

            float progress = Mathf.Clamp01(
                Vector3.Dot(fromStart,route) /
                route.sqrMagnitude);

            Vector3 closest = start + route * progress;
            closest.y = hit.position.y;
            closestDistance = Mathf.Min(
                closestDistance,
                Vector3.Distance(hit.position,closest));
        }

        return closestDistance <= 0.9f;
    }
}
