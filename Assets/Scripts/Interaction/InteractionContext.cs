using UnityEngine;

public readonly struct InteractionContext
{
    public PlayerInteractionController Player { get; }
    public Ray InteractionRay { get; }
    public RaycastHit Hit { get; }
    public InteractionType Type { get; }

    public Camera Camera => Player != null ? Player.TheCamera : null;
    public Collider HitCollider => Hit.collider;
    public Transform HitTransform => Hit.transform;
    public Vector3 HitPoint => Hit.point;
    public Vector3 HitNormal => Hit.normal;
    public float HitDistance => Hit.distance;

    public InteractionContext(
        PlayerInteractionController player,
        Ray interactionRay,
        RaycastHit hit,
        InteractionType type)
    {
        Player = player;
        InteractionRay = interactionRay;
        Hit = hit;
        Type = type;
    }
}
