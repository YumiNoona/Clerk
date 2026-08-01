using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Customer Database",
    menuName = "Clerk/Customers/Database")]
public class CustomerDatabase : ScriptableObject
{
    [Header("Available Customers")]
    [SerializeField]
    private List<CustomerDefinition> customers =
        new List<CustomerDefinition>();

    public IReadOnlyList<CustomerDefinition>
        Customers => customers;

    public int Count =>
        customers != null
            ? customers.Count
            : 0;

    public CustomerDefinition GetCustomer(
        int index)
    {
        if (customers == null ||
            index < 0 ||
            index >= customers.Count)
        {
            return null;
        }

        return customers[index];
    }

    public CustomerDefinition
        GetRandomCustomer()
    {
        if (customers == null ||
            customers.Count == 0)
        {
            return null;
        }

        float totalWeight = 0f;

        for (int i = 0;
             i < customers.Count;
             i++)
        {
            CustomerDefinition definition =
                customers[i];

            if (definition == null ||
                !definition.IsValid)
            {
                continue;
            }

            totalWeight +=
                definition.SpawnWeight;
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomValue =
            Random.Range(0f,totalWeight);

        float accumulatedWeight = 0f;

        for (int i = 0;
             i < customers.Count;
             i++)
        {
            CustomerDefinition definition =
                customers[i];

            if (definition == null ||
                !definition.IsValid)
            {
                continue;
            }

            accumulatedWeight +=
                definition.SpawnWeight;

            if (randomValue <= accumulatedWeight)
            {
                return definition;
            }
        }

        return GetLastValidCustomer();
    }

    public bool Contains(
        CustomerDefinition definition)
    {
        return definition != null &&
               customers != null &&
               customers.Contains(definition);
    }

    private CustomerDefinition
        GetLastValidCustomer()
    {
        for (int i = customers.Count - 1;
             i >= 0;
             i--)
        {
            CustomerDefinition definition =
                customers[i];

            if (definition != null &&
                definition.IsValid)
            {
                return definition;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (customers == null)
        {
            customers =
                new List<CustomerDefinition>();

            return;
        }

        for (int i = customers.Count - 1;
             i >= 0;
             i--)
        {
            if (customers[i] == null)
            {
                customers.RemoveAt(i);
            }
        }
    }
#endif
}
