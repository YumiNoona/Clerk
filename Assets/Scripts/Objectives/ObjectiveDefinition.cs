using UnityEngine;

public enum ObjectiveType
{
    PurchaseStock,
    PurchaseFurniture,
    ServeCustomers,
    SellItems,
    EarnRevenue,
    ReachProfit,
    ReachStoreLevel
}

[CreateAssetMenu(
    fileName = "New Objective",
    menuName = "Clerk/Objectives/Definition")]
public sealed class ObjectiveDefinition : ScriptableObject
{
    [SerializeField,HideInInspector]
    private string objectiveId;

    public string Title;

    [TextArea]
    public string Description;

    public ObjectiveType Type;

    [Min(1)]
    public int TargetAmount = 1;

    [Header("Rewards")]
    [Min(0f)]
    public float MoneyReward;

    [Min(0)]
    public int ExperienceReward;

    public string ObjectiveId => objectiveId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        string assetPath =
            UnityEditor.AssetDatabase
                .GetAssetPath(this);

        if (!string.IsNullOrEmpty(assetPath))
        {
            string guid =
                UnityEditor.AssetDatabase
                    .AssetPathToGUID(assetPath);

            if (objectiveId != guid)
            {
                objectiveId = guid;
                UnityEditor.EditorUtility
                    .SetDirty(this);
            }
        }

        TargetAmount = Mathf.Max(1,TargetAmount);
        MoneyReward = Mathf.Max(0f,MoneyReward);
        ExperienceReward =
            Mathf.Max(0,ExperienceReward);
    }
#endif
}
