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

    public CustomerMood CurrentMood => currentMood;

    public void Initialize(CustomerContext customer)
    {
        context = customer;
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
    }

    private void Update()
    {
        RefreshMood(false);
    }

    private void LateUpdate()
    {
        Camera targetCamera = Camera.main;

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
        Texture2D texture = catalog.GetTexture(nextMood);

        if (texture == null)
        {
            iconRenderer.enabled = false;
            return;
        }

        iconRenderer.enabled = true;

        if (iconRenderer.sprite != null)
        {
            Destroy(iconRenderer.sprite);
        }

        iconRenderer.sprite = Sprite.Create(
            texture,
            new Rect(0f,0f,texture.width,texture.height),
            new Vector2(0.5f,0.5f),
            Mathf.Max(texture.width,texture.height));

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

    private void OnDestroy()
    {
        if (iconRenderer != null &&
            iconRenderer.sprite != null)
        {
            Destroy(iconRenderer.sprite);
        }
    }
}
