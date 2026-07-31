using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CheckoutRegistry : MonoBehaviour
{
    private readonly List<CheckoutCounter> counters =
        new List<CheckoutCounter>();

    public IReadOnlyList<CheckoutCounter> Counters =>
        counters;

    public event Action<CheckoutCounter> CounterRegistered;
    public event Action<CheckoutCounter> CounterUnregistered;

    public void Register(CheckoutCounter counter)
    {
        if (counter == null ||
            counters.Contains(counter))
        {
            return;
        }

        counters.Add(counter);
        CounterRegistered?.Invoke(counter);
    }

    public void Unregister(CheckoutCounter counter)
    {
        if (counter == null ||
            !counters.Remove(counter))
        {
            return;
        }

        CounterUnregistered?.Invoke(counter);
    }

    public CheckoutCounter FindBestCounter()
    {
        CheckoutCounter best = null;
        int bestQueueCount = int.MaxValue;

        for (int i = counters.Count - 1;
             i >= 0;
             i--)
        {
            CheckoutCounter counter = counters[i];

            if (counter == null)
            {
                counters.RemoveAt(i);
                continue;
            }

            if (!counter.IsOpen ||
                counter.QueueCount >=
                    counter.QueueCapacity)
            {
                continue;
            }

            if (counter.QueueCount < bestQueueCount)
            {
                best = counter;
                bestQueueCount = counter.QueueCount;
            }
        }

        return best;
    }
}
