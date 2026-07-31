using UnityEngine;

[CreateAssetMenu(
    fileName = "New Customer Definition",
    menuName = "Store System/Customers/Customer Definition")]
public class CustomerDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string displayName = "Customer";

    [Header("Prefab")]
    [Tooltip("Customer prefab containing the future CustomerBrain, " +
             "CustomerNavigation, CustomerAnimator and NavMeshAgent components.")]
    [SerializeField]
    private GameObject customerPrefab;

    [Header("Spawn Weight")]
    [Tooltip("Higher values make this customer more likely to be selected.")]
    [Min(0f)]
    [SerializeField]
    private float spawnWeight = 1f;

    [Header("Movement")]
    [Min(0.1f)]
    [SerializeField]
    private float minimumWalkSpeed = 1.8f;

    [Min(0.1f)]
    [SerializeField]
    private float maximumWalkSpeed = 2.8f;

    [Header("Entrance Waiting")]
    [Min(0f)]
    [SerializeField]
    private float minimumEntranceWaitTime = 0.25f;

    [Min(0f)]
    [SerializeField]
    private float maximumEntranceWaitTime = 1.5f;

    [Header("Visual Scale Variation")]
    [SerializeField]
    private bool randomizeScale = true;

    [Min(0.1f)]
    [SerializeField]
    private float minimumScale = 0.95f;

    [Min(0.1f)]
    [SerializeField]
    private float maximumScale = 1.05f;

    [Header("Animation Variation")]
    [Tooltip("Optional Animator Override Controller for this customer type.")]
    [SerializeField]
    private AnimatorOverrideController animatorOverrideController;

    [Header("Shopping")]
    [Min(1)]
    [SerializeField]
    private int minimumShoppingLines = 1;

    [Min(1)]
    [SerializeField]
    private int maximumShoppingLines = 3;

    [Min(1)]
    [SerializeField]
    private int maximumQuantityPerProduct = 2;

    [Min(0f)]
    [SerializeField]
    private float minimumBrowseTime = 1f;

    [Min(0f)]
    [SerializeField]
    private float maximumBrowseTime = 2.5f;

    [Header("Patience")]
    [Min(1f)]
    [SerializeField]
    private float minimumPatience = 60f;

    [Min(1f)]
    [SerializeField]
    private float maximumPatience = 120f;

    [Min(0f)]
    [SerializeField]
    private float unavailableProductPenalty = 8f;

    [Min(0f)]
    [SerializeField]
    private float priceRejectionPenalty = 3f;

    public string DisplayName => displayName;

    public GameObject CustomerPrefab =>
        customerPrefab;

    public float SpawnWeight =>
        spawnWeight;

    public AnimatorOverrideController
        AnimatorOverrideController =>
            animatorOverrideController;

    public float UnavailableProductPenalty =>
        unavailableProductPenalty;

    public float PriceRejectionPenalty =>
        priceRejectionPenalty;

    public bool IsValid
    {
        get
        {
            return customerPrefab != null &&
                   spawnWeight > 0f;
        }
    }

    public float GetRandomWalkSpeed()
    {
        if (maximumWalkSpeed <= minimumWalkSpeed)
        {
            return minimumWalkSpeed;
        }

        return Random.Range(
            minimumWalkSpeed,
            maximumWalkSpeed);
    }

    public float GetRandomEntranceWaitTime()
    {
        if (maximumEntranceWaitTime <=
            minimumEntranceWaitTime)
        {
            return minimumEntranceWaitTime;
        }

        return Random.Range(
            minimumEntranceWaitTime,
            maximumEntranceWaitTime);
    }

    public float GetRandomScale()
    {
        if (!randomizeScale)
        {
            return 1f;
        }

        if (maximumScale <= minimumScale)
        {
            return minimumScale;
        }

        return Random.Range(
            minimumScale,
            maximumScale);
    }

    public int GetRandomShoppingLineCount()
    {
        return Random.Range(
            minimumShoppingLines,
            maximumShoppingLines + 1);
    }

    public int GetRandomDesiredQuantity()
    {
        return Random.Range(
            1,
            maximumQuantityPerProduct + 1);
    }

    public float GetRandomBrowseTime()
    {
        return Random.Range(
            minimumBrowseTime,
            maximumBrowseTime);
    }

    public float GetRandomPatience()
    {
        return Random.Range(
            minimumPatience,
            maximumPatience);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = name;
        }

        spawnWeight =
            Mathf.Max(0f,spawnWeight);

        minimumWalkSpeed =
            Mathf.Max(0.1f,minimumWalkSpeed);

        maximumWalkSpeed =
            Mathf.Max(
                minimumWalkSpeed,
                maximumWalkSpeed);

        minimumEntranceWaitTime =
            Mathf.Max(
                0f,
                minimumEntranceWaitTime);

        maximumEntranceWaitTime =
            Mathf.Max(
                minimumEntranceWaitTime,
                maximumEntranceWaitTime);

        minimumScale =
            Mathf.Max(0.1f,minimumScale);

        maximumScale =
            Mathf.Max(
                minimumScale,
                maximumScale);

        minimumShoppingLines =
            Mathf.Max(1,minimumShoppingLines);

        maximumShoppingLines =
            Mathf.Max(
                minimumShoppingLines,
                maximumShoppingLines);

        maximumQuantityPerProduct =
            Mathf.Max(1,maximumQuantityPerProduct);

        minimumBrowseTime =
            Mathf.Max(0f,minimumBrowseTime);

        maximumBrowseTime =
            Mathf.Max(
                minimumBrowseTime,
                maximumBrowseTime);

        minimumPatience =
            Mathf.Max(1f,minimumPatience);

        maximumPatience =
            Mathf.Max(
                minimumPatience,
                maximumPatience);

        unavailableProductPenalty =
            Mathf.Max(
                0f,
                unavailableProductPenalty);

        priceRejectionPenalty =
            Mathf.Max(
                0f,
                priceRejectionPenalty);
    }
#endif
}
