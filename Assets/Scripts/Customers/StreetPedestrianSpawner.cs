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
    private int successfulSpawnCount;
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

        if (successfulSpawnCount == 0 && pedestrianCount > 0)
        {
            Debug.LogWarning(
                "No pedestrians could be placed on the configured track. " +
                "Move the pedestrian spheres closer to the baked NavMesh.",
                this);
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

        NavMeshHit hit = default;
        bool foundPosition = false;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            float progress = Mathf.Repeat(
                (index + Random.Range(0.15f,0.85f) + attempt * 0.37f) /
                Mathf.Max(1f,pedestrianCount),
                1f);

            Vector3 requested = EvaluateTrack(progress);

            if (TrySampleStreetPosition(requested,out hit) &&
                HasPedestrianSeparation(hit.position,1f))
            {
                foundPosition = true;
                break;
            }
        }

        if (!foundPosition)
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
        successfulSpawnCount++;
    }

    private Vector3 EvaluateTrack(float normalizedProgress)
    {
        float totalLength = 0f;

        for (int i = 1; i < track.Count; i++)
        {
            totalLength += Vector3.Distance(track[i - 1],track[i]);
        }

        float targetDistance =
            Mathf.Clamp01(normalizedProgress) * totalLength;

        for (int i = 1; i < track.Count; i++)
        {
            float segmentLength =
                Vector3.Distance(track[i - 1],track[i]);

            if (targetDistance <= segmentLength)
            {
                return Vector3.Lerp(
                    track[i - 1],
                    track[i],
                    segmentLength > 0.001f
                        ? targetDistance / segmentLength
                        : 0f);
            }

            targetDistance -= segmentLength;
        }

        return track[track.Count - 1];
    }

    private bool HasPedestrianSeparation(
        Vector3 position,
        float minimumDistance)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform existing = transform.GetChild(i);

            if (Vector3.Distance(existing.position,position) < minimumDistance)
            {
                return false;
            }
        }

        return true;
    }

    private bool TrySampleStreetPosition(
        Vector3 requested,
        out NavMeshHit hit)
    {
        if (!NavMesh.SamplePosition(
                requested,
                out hit,
                4f,
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

        return closestDistance <= 3.5f;
    }
}
