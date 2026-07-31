using System;
using UnityEngine;
using UnityEngine.Serialization;

public class WalletController : MonoBehaviour
{
    public static WalletController Instance { get; private set; }

    [FormerlySerializedAs("currentMoney")]
    [SerializeField]
    private float startingMoney = 1000f;

    [SerializeField]
    private long currentBalanceCents;

    public float CurrentMoney => Balance.AsFloat;
    public Money Balance =>
        new Money(currentBalanceCents);

    public event Action<float> MoneyChanged;
    public event Action<Money> BalanceChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (currentBalanceCents == 0 &&
            startingMoney > 0f)
        {
            currentBalanceCents =
                Money.FromFloat(startingMoney)
                    .MinorUnits;
        }
    }

    public bool CanAfford(float amount)
    {
        return CanAfford(Money.FromFloat(amount));
    }

    public bool CanAfford(Money amount)
    {
        return !amount.IsNegative &&
               Balance >= amount;
    }

    public bool TrySpendMoney(float amount)
    {
        return TrySpend(Money.FromFloat(amount));
    }

    public bool TrySpend(Money amount)
    {
        if (!CanAfford(amount))
        {
            return false;
        }

        currentBalanceCents -= amount.MinorUnits;
        NotifyChanged();

        return true;
    }

    public void AddMoney(float amount)
    {
        Add(Money.FromFloat(amount));
    }

    public void Add(Money amount)
    {
        if (amount.IsNegative || amount.IsZero)
        {
            return;
        }

        currentBalanceCents += amount.MinorUnits;
        NotifyChanged();
    }

    public void SetMoney(float amount)
    {
        SetBalance(Money.FromFloat(amount));
    }

    public void SetBalance(Money amount)
    {
        currentBalanceCents =
            Math.Max(0,amount.MinorUnits);

        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Money balance = Balance;
        MoneyChanged?.Invoke(balance.AsFloat);
        BalanceChanged?.Invoke(balance);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
