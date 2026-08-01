using UnityEngine;

public enum CustomerMood
{
    Excited,
    Happy,
    Neutral,
    Annoyed,
    Angry,
    Furious
}

[DisallowMultipleComponent]
public sealed class CustomerMoodPresenter : MonoBehaviour
{
    private const string CatalogResourceName =
        "Customer Mood Catalog";

    private const float IconWorldSize = 0.55f;
    private const float HeightPadding = 0.22f;

    private CustomerContext context;
    private CustomerMoodCatalog catalog;
    private SpriteRenderer iconRenderer;
    private Transform iconTransform;
    private float initialPatience;
    private float temperamentOffset;
    private CustomerMood currentMood;
    private Camera targetCamera;
    private float nextRefreshTime;

    public CustomerMood CurrentMood => currentMood;

    public void Initialize(CustomerContext customer)
    {
        context = customer;
        targetCamera = Camera.main;
        initialPatience = Mathf.Max(
            1f,
            customer != null
                ? customer.PatienceRemaining
                : 1f);

        // Individual temperament keeps a crowd visually varied while every
        // customer still moves downward through the same mood sequence.
        temperamentOffset = Random.Range(-0.22f,0.08f);
        catalog = Resources.Load<CustomerMoodCatalog>(
            CatalogResourceName);

        if (catalog == null)
        {
            Debug.LogWarning(
                "Customer Mood Catalog was not found in Resources.",
                this);
            enabled = false;
            return;
        }

        CreateIcon();
        RefreshMood(true);
        iconRenderer.enabled =
            ShouldShowMood(context.State);
    }

    private void Update()
    {
        if (iconRenderer == null || context == null)
        {
            return;
        }

        bool shouldShow = ShouldShowMood(context.State);
        iconRenderer.enabled = shouldShow;

        if (!shouldShow)
        {
            return;
        }

        if (Time.time < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.time + 0.15f;
        RefreshMood(false);
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (iconTransform != null && targetCamera != null)
        {
            iconTransform.rotation =
                targetCamera.transform.rotation;
        }
    }

    private void CreateIcon()
    {
        Renderer[] customerRenderers =
            GetComponentsInChildren<Renderer>();

        Bounds bounds = new Bounds(
            transform.position + Vector3.up * 1.7f,
            Vector3.zero);

        bool foundRenderer = false;

        for (int i = 0; i < customerRenderers.Length; i++)
        {
            if (!customerRenderers[i].enabled)
            {
                continue;
            }

            if (!foundRenderer)
            {
                bounds = customerRenderers[i].bounds;
                foundRenderer = true;
            }
            else
            {
                bounds.Encapsulate(
                    customerRenderers[i].bounds);
            }
        }

        GameObject iconObject =
            new GameObject("Mood Icon");

        iconTransform = iconObject.transform;
        iconTransform.SetParent(transform,true);
        iconTransform.position = new Vector3(
            bounds.center.x,
            bounds.max.y + HeightPadding,
            bounds.center.z);

        iconRenderer =
            iconObject.AddComponent<SpriteRenderer>();

        iconRenderer.sortingOrder = 1000;
        iconRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        iconRenderer.receiveShadows = false;
    }

    private void RefreshMood(bool force)
    {
        if (context == null || iconRenderer == null)
        {
            return;
        }

        float patienceRatio = Mathf.Clamp01(
            context.PatienceRemaining /
            initialPatience + temperamentOffset);

        CustomerMood nextMood =
            EvaluateMood(patienceRatio);

        if (!force && nextMood == currentMood)
        {
            return;
        }

        currentMood = nextMood;
        Sprite sprite = catalog.GetSprite(nextMood);

        if (sprite == null)
        {
            iconRenderer.enabled = false;
            return;
        }

        iconRenderer.enabled = true;

        iconRenderer.sprite = sprite;

        float spriteSize = Mathf.Max(
            iconRenderer.sprite.bounds.size.x,
            iconRenderer.sprite.bounds.size.y);

        iconTransform.localScale = Vector3.one *
            (IconWorldSize / Mathf.Max(0.01f,spriteSize));
    }

    private static CustomerMood EvaluateMood(
        float patienceRatio)
    {
        if (patienceRatio >= 0.84f)
        {
            return CustomerMood.Excited;
        }

        if (patienceRatio >= 0.67f)
        {
            return CustomerMood.Happy;
        }

        if (patienceRatio >= 0.5f)
        {
            return CustomerMood.Neutral;
        }

        if (patienceRatio >= 0.33f)
        {
            return CustomerMood.Annoyed;
        }

        if (patienceRatio >= 0.16f)
        {
            return CustomerMood.Angry;
        }

        return CustomerMood.Furious;
    }

    private static bool ShouldShowMood(CustomerState state)
    {
        switch (state)
        {
            case CustomerState.Shopping:
            case CustomerState.MovingToCheckout:
            case CustomerState.WaitingInCheckoutQueue:
            case CustomerState.CheckingOut:
                return true;

            default:
                return false;
        }
    }

}
