using System;
using UnityEngine;

public class WalletController : MonoBehaviour
{
    public static WalletController Instance { get; private set; }

    [SerializeField]
    private float currentMoney = 1000f;

    public float CurrentMoney
    {
        get
        {
            return currentMoney;
        }
    }

    public event Action<float> MoneyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool CanAfford(float amount)
    {
        return amount >= 0f && currentMoney >= amount;
    }

    public bool TrySpendMoney(float amount)
    {
        if (!CanAfford(amount))
        {
            return false;
        }

        currentMoney -= amount;
        MoneyChanged?.Invoke(currentMoney);

        return true;
    }

    public void AddMoney(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentMoney += amount;
        MoneyChanged?.Invoke(currentMoney);
    }

    public void SetMoney(float amount)
    {
        currentMoney = Mathf.Max(0f,amount);
        MoneyChanged?.Invoke(currentMoney);
    }
}