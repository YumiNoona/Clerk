using UnityEngine;

public class CustomerSpawnPoint : CustomerPoint
{
    [Header("Spawn Settings")]
    [Tooltip("When disabled, the customer spawner will ignore this point.")]
    [SerializeField]
    private bool spawningEnabled = true;

    [Tooltip("Optional radius used to add a small random spawn offset.")]
    [Min(0f)]
    [SerializeField]
    private float randomSpawnRadius;

    [Min(0f)]
    [SerializeField]
    private float spawnWeight = 1f;

    public bool SpawningEnabled => spawningEnabled;

    public float RandomSpawnRadius =>
        randomSpawnRadius;

    public float SpawnWeight => spawnWeight;

    public void Configure(float weight, float radius)
    {
        spawnWeight = Mathf.Max(0f,weight);
        randomSpawnRadius = Mathf.Max(0f,radius);
        spawningEnabled = spawnWeight > 0f;
    }

    public Vector3 GetSpawnPosition()
    {
        if (randomSpawnRadius <= 0f)
        {
            return Position;
        }

        Vector2 randomCircle =
            Random.insideUnitCircle *
            randomSpawnRadius;

        return Position +
               new Vector3(
                   randomCircle.x,
                   0f,
                   randomCircle.y);
    }

    public Quaternion GetSpawnRotation()
    {
        return Rotation;
    }

    public void SetSpawningEnabled(bool enabled)
    {
        spawningEnabled = enabled;
    }

    protected override void Reset()
    {
        base.Reset();

        spawningEnabled = true;
        randomSpawnRadius = 0f;
        spawnWeight = 1f;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        randomSpawnRadius =
            Mathf.Max(0f,randomSpawnRadius);

        spawnWeight = Mathf.Max(0f,spawnWeight);
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (randomSpawnRadius <= 0f)
        {
            return;
        }

        Gizmos.DrawWireSphere(
            Position,
            randomSpawnRadius);
    }
#endif
}
