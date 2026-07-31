using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerNavigation : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private NavMeshAgent agent;

    [Header("Arrival")]
    [Tooltip(
        "Extra tolerance added to the NavMeshAgent stopping distance " +
        "when determining whether the customer has arrived.")]
    [Min(0f)]
    [SerializeField]
    private float arrivalTolerance = 0.08f;

    [Tooltip(
        "Maximum velocity magnitude allowed before the agent is " +
        "considered fully stopped at its destination.")]
    [Min(0f)]
    [SerializeField]
    private float stoppedVelocityThreshold = 0.05f;

    [Header("NavMesh Sampling")]
    [Tooltip(
        "When the requested destination is not directly on the NavMesh, " +
        "the system searches within this radius for a valid position.")]
    [Min(0.01f)]
    [SerializeField]
    private float destinationSampleRadius = 1.5f;

    [Tooltip(
        "NavMesh area mask used while sampling destinations.")]
    [SerializeField]
    private int navMeshAreaMask = NavMesh.AllAreas;

    [Header("Rotation")]
    [Tooltip(
        "When enabled, the NavMeshAgent controls the customer's rotation " +
        "while moving.")]
    [SerializeField]
    private bool updateRotationWhileMoving = true;

    [Tooltip(
        "Rotation speed used when facing a target after movement.")]
    [Min(0f)]
    [SerializeField]
    private float manualRotationSpeed = 540f;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs;

    [SerializeField]
    private bool drawDestinationGizmo = true;

    private Vector3 requestedDestination;
    private Vector3 resolvedDestination;

    private bool hasDestination;
    private bool hasReachedDestination;
    private bool movementFailed;

    public event Action DestinationReached;
    public event Action MovementFailed;

    public NavMeshAgent Agent => agent;

    public bool IsOnNavMesh
    {
        get
        {
            return agent != null &&
                   agent.enabled &&
                   agent.isOnNavMesh;
        }
    }

    public bool HasDestination => hasDestination;

    public bool HasReachedDestination =>
        hasDestination &&
        hasReachedDestination;

    public bool MovementFailedFlag =>
        movementFailed;

    public bool IsMoving
    {
        get
        {
            if (!IsOnNavMesh ||
                !hasDestination ||
                agent.isStopped)
            {
                return false;
            }

            return agent.velocity.sqrMagnitude >
                   stoppedVelocityThreshold *
                   stoppedVelocityThreshold;
        }
    }

    public bool IsPathPending
    {
        get
        {
            return IsOnNavMesh &&
                   agent.pathPending;
        }
    }

    public bool HasCompletePath
    {
        get
        {
            return IsOnNavMesh &&
                   agent.hasPath &&
                   agent.pathStatus ==
                   NavMeshPathStatus.PathComplete;
        }
    }

    public Vector3 Velocity
    {
        get
        {
            return IsOnNavMesh
                ? agent.velocity
                : Vector3.zero;
        }
    }

    public Vector3 DesiredVelocity
    {
        get
        {
            return IsOnNavMesh
                ? agent.desiredVelocity
                : Vector3.zero;
        }
    }

    public float CurrentSpeed =>
        Velocity.magnitude;

    public float NormalizedSpeed
    {
        get
        {
            if (agent == null ||
                agent.speed <= Mathf.Epsilon)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                CurrentSpeed / agent.speed);
        }
    }

    public float RemainingDistance
    {
        get
        {
            if (!IsOnNavMesh ||
                !hasDestination)
            {
                return 0f;
            }

            return agent.remainingDistance;
        }
    }

    public Vector3 RequestedDestination =>
        requestedDestination;

    public Vector3 ResolvedDestination =>
        resolvedDestination;

    private void Reset()
    {
        CacheComponents();

        if (agent != null)
        {
            agent.updateRotation =
                updateRotationWhileMoving;
        }
    }

    private void Awake()
    {
        CacheComponents();
        ApplyAgentSettings();
    }

    private void Update()
    {
        EvaluateMovement();
    }

    public void SetMovementSpeed(float speed)
    {
        if (agent == null)
        {
            return;
        }

        agent.speed = Mathf.Max(0.1f,speed);
    }

    public void SetAcceleration(float acceleration)
    {
        if (agent == null)
        {
            return;
        }

        agent.acceleration =
            Mathf.Max(0.1f,acceleration);
    }

    public void SetStoppingDistance(
        float stoppingDistance)
    {
        if (agent == null)
        {
            return;
        }

        agent.stoppingDistance =
            Mathf.Max(0f,stoppingDistance);
    }

    public bool MoveTo(Vector3 worldPosition)
    {
        return MoveTo(
            worldPosition,
            agent != null
                ? agent.stoppingDistance
                : 0f);
    }

    public bool MoveTo(
        Vector3 worldPosition,
        float stoppingDistance)
    {
        requestedDestination = worldPosition;
        movementFailed = false;
        hasReachedDestination = false;

        if (!EnsureAgentIsReady())
        {
            FailMovement(
                "The NavMeshAgent is not enabled or is not positioned on a NavMesh.");

            return false;
        }

        if (!TryResolveNavMeshPosition(
                worldPosition,
                out Vector3 navMeshPosition))
        {
            FailMovement(
                "No valid NavMesh position was found near the requested destination.");

            return false;
        }

        resolvedDestination = navMeshPosition;

        agent.stoppingDistance =
            Mathf.Max(0f,stoppingDistance);

        agent.isStopped = false;

        bool destinationAccepted =
            agent.SetDestination(
                resolvedDestination);

        if (!destinationAccepted)
        {
            FailMovement(
                "NavMeshAgent.SetDestination rejected the requested destination.");

            return false;
        }

        hasDestination = true;

        if (showDebugLogs)
        {
            Debug.Log(
                name +
                " moving to " +
                resolvedDestination +
                ".",
                this);
        }

        return true;
    }

    public bool MoveTo(CustomerPoint point)
    {
        if (point == null)
        {
            return false;
        }

        return MoveTo(point.Position);
    }

    public void Stop()
    {
        if (!IsOnNavMesh)
        {
            return;
        }

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    public void Resume()
    {
        if (!IsOnNavMesh ||
            !hasDestination)
        {
            return;
        }

        agent.isStopped = false;
    }

    public void ClearDestination()
    {
        hasDestination = false;
        hasReachedDestination = false;
        movementFailed = false;

        requestedDestination =
            transform.position;

        resolvedDestination =
            transform.position;

        if (!IsOnNavMesh)
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    public bool WarpTo(Vector3 worldPosition)
    {
        if (!EnsureAgentIsReady())
        {
            return false;
        }

        if (!TryResolveNavMeshPosition(
                worldPosition,
                out Vector3 navMeshPosition))
        {
            return false;
        }

        ClearDestination();

        bool warped =
            agent.Warp(navMeshPosition);

        if (warped)
        {
            requestedDestination =
                navMeshPosition;

            resolvedDestination =
                navMeshPosition;
        }

        return warped;
    }

    public void FaceDirection(
        Vector3 worldDirection,
        bool immediate = false)
    {
        worldDirection.y = 0f;

        if (worldDirection.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                worldDirection.normalized,
                Vector3.up);

        if (immediate ||
            manualRotationSpeed <= 0f)
        {
            transform.rotation =
                targetRotation;

            return;
        }

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                manualRotationSpeed *
                Time.deltaTime);
    }

    public void FacePoint(
        Vector3 worldPosition,
        bool immediate = false)
    {
        FaceDirection(
            worldPosition -
            transform.position,
            immediate);
    }

    public bool IsAtPosition(
        Vector3 worldPosition,
        float tolerance)
    {
        Vector3 currentPosition =
            transform.position;

        currentPosition.y = 0f;

        worldPosition.y = 0f;

        float safeTolerance =
            Mathf.Max(0f,tolerance);

        return Vector3.SqrMagnitude(
                   currentPosition -
                   worldPosition) <=
               safeTolerance *
               safeTolerance;
    }

    private void EvaluateMovement()
    {
        if (!hasDestination ||
            hasReachedDestination ||
            movementFailed)
        {
            return;
        }

        if (!EnsureAgentIsReady())
        {
            FailMovement(
                "The NavMeshAgent became unavailable while moving.");

            return;
        }

        if (agent.pathPending)
        {
            return;
        }

        if (agent.pathStatus ==
            NavMeshPathStatus.PathInvalid)
        {
            FailMovement(
                "The NavMeshAgent produced an invalid path.");

            return;
        }

        if (float.IsInfinity(
                agent.remainingDistance))
        {
            return;
        }

        float arrivalDistance =
            agent.stoppingDistance +
            arrivalTolerance;

        bool withinArrivalDistance =
            agent.remainingDistance <=
            arrivalDistance;

        bool nearlyStopped =
            agent.velocity.sqrMagnitude <=
            stoppedVelocityThreshold *
            stoppedVelocityThreshold;

        if (!withinArrivalDistance ||
            !nearlyStopped)
        {
            return;
        }

        CompleteMovement();
    }

    private void CompleteMovement()
    {
        hasReachedDestination = true;

        if (IsOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                name +
                " reached its destination.",
                this);
        }

        DestinationReached?.Invoke();
    }

    private void FailMovement(string message)
    {
        movementFailed = true;
        hasDestination = false;
        hasReachedDestination = false;

        if (IsOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        if (showDebugLogs)
        {
            Debug.LogWarning(
                name +
                ": " +
                message,
                this);
        }

        MovementFailed?.Invoke();
    }

    private bool EnsureAgentIsReady()
    {
        return agent != null &&
               agent.enabled &&
               agent.isOnNavMesh;
    }

    private bool TryResolveNavMeshPosition(
        Vector3 requestedPosition,
        out Vector3 resolvedPosition)
    {
        if (NavMesh.SamplePosition(
                requestedPosition,
                out NavMeshHit hit,
                destinationSampleRadius,
                navMeshAreaMask))
        {
            resolvedPosition =
                hit.position;

            return true;
        }

        resolvedPosition =
            requestedPosition;

        return false;
    }

    private void CacheComponents()
    {
        if (agent == null)
        {
            agent =
                GetComponent<NavMeshAgent>();
        }
    }

    private void ApplyAgentSettings()
    {
        if (agent == null)
        {
            return;
        }

        agent.updatePosition = true;

        agent.updateRotation =
            updateRotationWhileMoving;
    }

    private void OnValidate()
    {
        CacheComponents();

        arrivalTolerance =
            Mathf.Max(
                0f,
                arrivalTolerance);

        stoppedVelocityThreshold =
            Mathf.Max(
                0f,
                stoppedVelocityThreshold);

        destinationSampleRadius =
            Mathf.Max(
                0.01f,
                destinationSampleRadius);

        manualRotationSpeed =
            Mathf.Max(
                0f,
                manualRotationSpeed);

        ApplyAgentSettings();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawDestinationGizmo ||
            !hasDestination)
        {
            return;
        }

        Gizmos.DrawWireSphere(
            resolvedDestination,
            0.2f);

        Gizmos.DrawLine(
            transform.position,
            resolvedDestination);
    }
#endif
}