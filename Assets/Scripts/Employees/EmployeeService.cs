using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EmployeeService : MonoBehaviour
{
    private readonly List<EmployeeContext> employees =
        new List<EmployeeContext>();

    private readonly HashSet<StockBoxController>
        claimedBoxes =
            new HashSet<StockBoxController>();

    public IReadOnlyList<EmployeeContext> Employees =>
        employees;

    public event Action<EmployeeContext> EmployeeHired;
    public event Action<EmployeeContext> EmployeeRemoved;

    private void Start()
    {
        if (GameBootstrap.Instance?.Days != null)
        {
            GameBootstrap.Instance.Days.DayEnded +=
                HandleDayEnded;
        }
    }

    private void OnDestroy()
    {
        if (GameBootstrap.Instance?.Days != null)
        {
            GameBootstrap.Instance.Days.DayEnded -=
                HandleDayEnded;
        }
    }

    public void Register(EmployeeContext employee)
    {
        if (employee == null ||
            employees.Contains(employee))
        {
            return;
        }

        employees.Add(employee);
    }

    public void Unregister(EmployeeContext employee)
    {
        if (employee == null)
        {
            return;
        }

        employees.Remove(employee);
    }

    public bool TryHire(
        EmployeeDefinition definition,
        Vector3 position,
        Quaternion rotation,
        out EmployeeContext employee)
    {
        employee = null;

        if (definition == null ||
            definition.Prefab == null ||
            GameBootstrap.Instance == null ||
            !GameBootstrap.Instance.Economy.TrySpend(
                Money.FromFloat(definition.HiringCost),
                LedgerEntryType.OperatingCost,
                "Hired " + definition.DisplayName,
                definition.EmployeeTypeId))
        {
            return false;
        }

        GameObject instance =
            Instantiate(
                definition.Prefab,
                position,
                rotation);

        employee =
            instance.GetComponent<EmployeeContext>() ??
            instance.AddComponent<EmployeeContext>();

        employee.Initialize(definition);

        if (definition.Role ==
                EmployeeRole.Restocker &&
            instance.GetComponent<
                RestockEmployeeBrain>() == null)
        {
            instance.AddComponent<
                RestockEmployeeBrain>();
        }

        Register(employee);
        EmployeeHired?.Invoke(employee);
        return true;
    }

    public bool Fire(EmployeeContext employee)
    {
        if (employee == null ||
            !employees.Remove(employee))
        {
            return false;
        }

        EmployeeRemoved?.Invoke(employee);
        Destroy(employee.gameObject);
        return true;
    }

    public bool TryClaimBox(StockBoxController box)
    {
        return box != null &&
               !box.IsHeld &&
               box.Quantity > 0 &&
               claimedBoxes.Add(box);
    }

    public void ReleaseBox(StockBoxController box)
    {
        if (box != null)
        {
            claimedBoxes.Remove(box);
        }
    }

    private void HandleDayEnded(int day)
    {
        float wages = 0f;

        for (int i = employees.Count - 1;
             i >= 0;
             i--)
        {
            EmployeeContext employee = employees[i];

            if (employee == null)
            {
                employees.RemoveAt(i);
                continue;
            }

            if (employee.Definition != null)
            {
                wages +=
                    employee.Definition.DailyWage;
            }
        }

        if (wages > 0f)
        {
            GameBootstrap.Instance.Economy
                .RecordOperatingCost(
                    Money.FromFloat(wages),
                    "Employee wages");
        }
    }
}
