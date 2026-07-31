using UnityEngine;

public abstract class CustomerPoint : MonoBehaviour
{
    [Header("Customer Position")]
    [Tooltip("Optional exact position where the customer should stand. " +
             "When empty, this GameObject's transform is used.")]
    [SerializeField]
    private Transform standingPoint;

    [Header("Waiting")]
    [Tooltip("Minimum time a customer may wait at this point.")]
    [Min(0f)]
    [SerializeField]
    private float minimumWaitTime;

    [Tooltip("Maximum time a customer may wait at this point.")]
    [Min(0f)]
    [SerializeField]
    private float maximumWaitTime;

    public Transform StandingPoint
    {
        get
        {
            return standingPoint != null
                ? standingPoint
                : transform;
        }
    }

    public Vector3 Position => StandingPoint.position;

    public Quaternion Rotation => StandingPoint.rotation;

    public float MinimumWaitTime => minimumWaitTime;

    public float MaximumWaitTime => maximumWaitTime;

    public virtual float GetRandomWaitTime()
    {
        if (maximumWaitTime <= minimumWaitTime)
        {
            return minimumWaitTime;
        }

        return Random.Range(
            minimumWaitTime,
            maximumWaitTime);
    }

    public virtual void PlaceCustomerImmediately(
        Transform customerTransform)
    {
        if (customerTransform == null)
        {
            return;
        }

        customerTransform.SetPositionAndRotation(
            Position,
            Rotation);
    }

    protected virtual void Reset()
    {
        standingPoint = transform;
        minimumWaitTime = 0f;
        maximumWaitTime = 0f;
    }

    protected virtual void OnValidate()
    {
        minimumWaitTime =
            Mathf.Max(0f,minimumWaitTime);

        maximumWaitTime =
            Mathf.Max(
                minimumWaitTime,
                maximumWaitTime);
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        Transform point =
            standingPoint != null
                ? standingPoint
                : transform;

        Gizmos.DrawWireSphere(
            point.position,
            0.2f);

        Gizmos.DrawLine(
            point.position,
            point.position +
            point.forward * 0.6f);
    }
#endif
}