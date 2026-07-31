using System;
using System.Collections.Generic;
using UnityEngine;

public enum NotificationKind
{
    Information,
    Success,
    Warning,
    Error
}

public readonly struct StoreNotification
{
    public string Message { get; }
    public NotificationKind Kind { get; }
    public float Duration { get; }

    public StoreNotification(
        string message,
        NotificationKind kind,
        float duration)
    {
        Message = message;
        Kind = kind;
        Duration = Mathf.Max(1f,duration);
    }
}

public sealed class NotificationService :
    MonoBehaviour
{
    private readonly Queue<StoreNotification>
        pending =
            new Queue<StoreNotification>();

    public event Action<StoreNotification>
        NotificationRaised;

    public void Show(
        string message,
        NotificationKind kind =
            NotificationKind.Information,
        float duration = 3f)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        StoreNotification notification =
            new StoreNotification(
                message,
                kind,
                duration);

        pending.Enqueue(notification);
        NotificationRaised?.Invoke(notification);
    }

    public bool TryDequeue(
        out StoreNotification notification)
    {
        if (pending.Count == 0)
        {
            notification = default;
            return false;
        }

        notification = pending.Dequeue();
        return true;
    }
}
