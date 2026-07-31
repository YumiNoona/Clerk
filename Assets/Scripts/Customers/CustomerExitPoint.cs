using UnityEngine;

public class CustomerExitPoint : CustomerPoint
{
    [Header("Exit Settings")]
    [Tooltip("When disabled, customers will not use this exit.")]
    [SerializeField]
    private bool exitEnabled = true;

    [Tooltip("Optional point outside the store that customers move toward " +
             "before they are despawned.")]
    [SerializeField]
    private Transform despawnPoint;

    public bool ExitEnabled => exitEnabled;

    public bool HasDespawnPoint =>
        despawnPoint != null;

    public Vector3 DespawnPosition
    {
        get
        {
            return despawnPoint != null
                ? despawnPoint.position
                : Position;
        }
    }

    public Quaternion DespawnRotation
    {
        get
        {
            return despawnPoint != null
                ? despawnPoint.rotation
                : Rotation;
        }
    }

    public void SetExitEnabled(bool enabled)
    {
        exitEnabled = enabled;
    }

    protected override void Reset()
    {
        base.Reset();

        exitEnabled = true;
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (despawnPoint == null)
        {
            return;
        }

        Gizmos.DrawWireCube(
            despawnPoint.position,
            Vector3.one * 0.3f);

        Gizmos.DrawLine(
            Position,
            despawnPoint.position);

        Gizmos.DrawLine(
            despawnPoint.position,
            despawnPoint.position +
            despawnPoint.forward * 0.6f);
    }
#endif
}