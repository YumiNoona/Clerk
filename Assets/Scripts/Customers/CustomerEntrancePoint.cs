using UnityEngine;

public class CustomerEntrancePoint : CustomerPoint
{
    [Header("Entrance Settings")]
    [Tooltip("When disabled, customers will not use this entrance.")]
    [SerializeField]
    private bool entranceEnabled = true;

    [Tooltip("Optional point customers move toward after reaching the entrance. " +
             "This can help move them fully inside the store.")]
    [SerializeField]
    private Transform insidePoint;

    public bool EntranceEnabled =>
        entranceEnabled;

    public bool HasInsidePoint =>
        insidePoint != null;

    public Vector3 InsidePosition
    {
        get
        {
            return insidePoint != null
                ? insidePoint.position
                : Position;
        }
    }

    public Quaternion InsideRotation
    {
        get
        {
            return insidePoint != null
                ? insidePoint.rotation
                : Rotation;
        }
    }

    public void SetEntranceEnabled(bool enabled)
    {
        entranceEnabled = enabled;
    }

    public void Configure(
        Transform destinationInside,
        float minimumWait,
        float maximumWait)
    {
        insidePoint = destinationInside;
        ConfigureWaiting(minimumWait,maximumWait);
        entranceEnabled = true;
    }

    protected override void Reset()
    {
        base.Reset();

        entranceEnabled = true;
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (insidePoint == null)
        {
            return;
        }

        Gizmos.DrawWireCube(
            insidePoint.position,
            Vector3.one * 0.3f);

        Gizmos.DrawLine(
            Position,
            insidePoint.position);

        Gizmos.DrawLine(
            insidePoint.position,
            insidePoint.position +
            insidePoint.forward * 0.6f);
    }
#endif
}
