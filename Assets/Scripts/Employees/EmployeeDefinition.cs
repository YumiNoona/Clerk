using UnityEngine;

public enum EmployeeRole
{
    Restocker,
    Cashier
}

[CreateAssetMenu(
    fileName = "New Employee",
    menuName = "Store System/Employees/Employee")]
public sealed class EmployeeDefinition : ScriptableObject
{
    [SerializeField,HideInInspector]
    private string employeeTypeId;

    public string DisplayName = "Employee";
    public EmployeeRole Role = EmployeeRole.Restocker;
    public GameObject Prefab;

    [Min(0f)]
    public float HiringCost = 100f;

    [Min(0f)]
    public float DailyWage = 35f;

    [Min(0.1f)]
    public float MovementSpeed = 3.5f;

    [Min(0.05f)]
    public float WorkInterval = 0.4f;

    public string EmployeeTypeId => employeeTypeId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        string path =
            UnityEditor.AssetDatabase.GetAssetPath(this);

        if (!string.IsNullOrWhiteSpace(path))
        {
            employeeTypeId =
                UnityEditor.AssetDatabase
                    .AssetPathToGUID(path);
        }

        HiringCost = Mathf.Max(0f,HiringCost);
        DailyWage = Mathf.Max(0f,DailyWage);
        MovementSpeed =
            Mathf.Max(0.1f,MovementSpeed);
        WorkInterval =
            Mathf.Max(0.05f,WorkInterval);
    }
#endif
}
