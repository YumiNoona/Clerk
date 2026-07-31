using UnityEngine;

public sealed class ProductDemandService : MonoBehaviour
{
    [Header("Demand Curve")]
    [Min(0.1f)]
    [SerializeField]
    private float priceElasticity = 1.6f;

    [Range(0.05f,1f)]
    [SerializeField]
    private float minimumPurchaseChance = 0.08f;

    [Range(0f,0.5f)]
    [SerializeField]
    private float reputationInfluence = 0.2f;

    public float GetPurchaseProbability(StockInfo product)
    {
        if (product == null ||
            GameBootstrap.Instance == null)
        {
            return 0f;
        }

        float basePrice =
            Mathf.Max(0.01f,product.BasePrice);

        float currentPrice =
            GameBootstrap.Instance.Products
                .GetPrice(product);

        float ratio =
            Mathf.Max(0.01f,currentPrice / basePrice);

        float priceDemand =
            Mathf.Pow(ratio,-priceElasticity);

        float reputation =
            GameBootstrap.Instance.Progression != null
                ? GameBootstrap.Instance.Progression
                    .Reputation / 100f
                : 0.5f;

        float reputationModifier =
            Mathf.Lerp(
                1f - reputationInfluence,
                1f + reputationInfluence,
                reputation);

        return Mathf.Clamp(
            priceDemand * reputationModifier,
            minimumPurchaseChance,
            1f);
    }

    public bool WillPurchase(StockInfo product)
    {
        return Random.value <=
               GetPurchaseProbability(product);
    }

    public float GetDemandScore(StockInfo product)
    {
        return GetPurchaseProbability(product);
    }
}
