using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CustomerRegistry : MonoBehaviour
{
    private readonly List<CustomerContext> customers =
        new List<CustomerContext>();

    public IReadOnlyList<CustomerContext> Customers =>
        customers;

    public int Count
    {
        get
        {
            RemoveMissingCustomers();
            return customers.Count;
        }
    }

    public event Action<CustomerContext> CustomerRegistered;
    public event Action<CustomerContext> CustomerUnregistered;

    public void Register(CustomerContext customer)
    {
        if (customer == null ||
            customers.Contains(customer))
        {
            return;
        }

        customers.Add(customer);
        CustomerRegistered?.Invoke(customer);
    }

    public void Unregister(CustomerContext customer)
    {
        if (customer == null ||
            !customers.Remove(customer))
        {
            return;
        }

        CustomerUnregistered?.Invoke(customer);
    }

    private void RemoveMissingCustomers()
    {
        customers.RemoveAll(
            customer => customer == null);
    }
}
