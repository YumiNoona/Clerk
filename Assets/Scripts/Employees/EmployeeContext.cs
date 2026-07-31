using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class EmployeeContext : MonoBehaviour
{
    [SerializeField,HideInInspector]
    private string employeeId;

    [SerializeField]
    private EmployeeDefinition definition;

    private NavMeshAgent agent;

    public string EmployeeId => employeeId;
    public EmployeeDefinition Definition => definition;
    public NavMeshAgent Agent => agent;

    private void Awake()
    {
        EnsureId();
        agent = GetComponent<NavMeshAgent>();
        ApplyDefinition();
    }

    private void OnEnable()
    {
        GameBootstrap.Instance?.Employees
            .Register(this);
    }

    private void OnDisable()
    {
        GameBootstrap.Instance?.Employees
            .Unregister(this);
    }

    public void Initialize(
        EmployeeDefinition employeeDefinition,
        string restoredId = null)
    {
        definition = employeeDefinition;

        if (!string.IsNullOrWhiteSpace(restoredId))
        {
            employeeId = restoredId;
        }

        EnsureId();
        ApplyDefinition();
    }

    private void ApplyDefinition()
    {
        if (agent != null && definition != null)
        {
            agent.speed = definition.MovementSpeed;
        }
    }

    private void EnsureId()
    {
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            employeeId =
                Guid.NewGuid().ToString("N");
        }
    }
}
