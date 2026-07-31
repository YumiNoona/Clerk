using System;
using UnityEngine;

[Serializable]
public sealed class FinanceData
{
    public long OutstandingLoanCents;
    public float DailyInterestRate = 0.01f;
}

public sealed class StoreFinanceService :
    MonoBehaviour
{
    [Min(0f)]
    [SerializeField]
    private float maximumLoan = 5000f;

    [Range(0f,0.25f)]
    [SerializeField]
    private float dailyInterestRate = 0.01f;

    public Money OutstandingLoan { get; private set; }
    public Money AvailableCredit =>
        Money.FromFloat(maximumLoan) -
        OutstandingLoan;

    public event Action FinanceChanged;

    private void Start()
    {
        GameBootstrap.Instance.Days.DayEnded +=
            HandleDayEnded;
    }

    private void OnDestroy()
    {
        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Days.DayEnded -=
                HandleDayEnded;
        }
    }

    public bool Borrow(Money amount)
    {
        if (amount.IsNegative ||
            amount.IsZero ||
            amount.CompareTo(AvailableCredit) > 0 ||
            GameBootstrap.Instance == null)
        {
            return false;
        }

        OutstandingLoan += amount;

        GameBootstrap.Instance.Economy.GrantFunds(
            amount,
            LedgerEntryType.Loan,
            "Business loan");

        FinanceChanged?.Invoke();
        return true;
    }

    public bool Repay(Money amount)
    {
        Money repayment =
            amount.CompareTo(OutstandingLoan) > 0
                ? OutstandingLoan
                : amount;

        if (repayment.IsNegative ||
            repayment.IsZero ||
            !GameBootstrap.Instance.Economy.TrySpend(
                repayment,
                LedgerEntryType.Loan,
                "Loan repayment"))
        {
            return false;
        }

        OutstandingLoan -= repayment;
        FinanceChanged?.Invoke();
        return true;
    }

    public FinanceData Capture()
    {
        return new FinanceData
        {
            OutstandingLoanCents =
                OutstandingLoan.MinorUnits,
            DailyInterestRate =
                dailyInterestRate
        };
    }

    public void Restore(FinanceData data)
    {
        data ??= new FinanceData();

        OutstandingLoan =
            new Money(
                Math.Max(
                    0L,
                    data.OutstandingLoanCents));

        dailyInterestRate =
            Mathf.Clamp(
                data.DailyInterestRate,
                0f,
                0.25f);

        FinanceChanged?.Invoke();
    }

    private void HandleDayEnded(int day)
    {
        if (OutstandingLoan.IsZero)
        {
            return;
        }

        Money interest =
            Money.FromFloat(
                OutstandingLoan.AsFloat *
                dailyInterestRate);

        OutstandingLoan += interest;
        FinanceChanged?.Invoke();
    }
}
