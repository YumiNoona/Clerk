using System;
using UnityEngine;

[Serializable]
public readonly struct Money :
    IEquatable<Money>,
    IComparable<Money>
{
    [SerializeField]
    private readonly long minorUnits;

    public long MinorUnits => minorUnits;
    public float AsFloat => minorUnits / 100f;
    public decimal AsDecimal => minorUnits / 100m;
    public bool IsNegative => minorUnits < 0;
    public bool IsZero => minorUnits == 0;

    public static Money Zero => new Money(0);

    public Money(long minorUnits)
    {
        this.minorUnits = minorUnits;
    }

    public static Money FromFloat(float value)
    {
        decimal decimalValue =
            Convert.ToDecimal(value);

        return FromDecimal(decimalValue);
    }

    public static Money FromDecimal(decimal value)
    {
        long units = decimal.ToInt64(
            decimal.Round(
                value * 100m,
                0,
                MidpointRounding.AwayFromZero));

        return new Money(units);
    }

    public int CompareTo(Money other)
    {
        return minorUnits.CompareTo(
            other.minorUnits);
    }

    public bool Equals(Money other)
    {
        return minorUnits == other.minorUnits;
    }

    public override bool Equals(object obj)
    {
        return obj is Money other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return minorUnits.GetHashCode();
    }

    public override string ToString()
    {
        return AsDecimal.ToString("0.00");
    }

    public static Money operator +(
        Money left,
        Money right)
    {
        return new Money(
            left.minorUnits + right.minorUnits);
    }

    public static Money operator -(
        Money left,
        Money right)
    {
        return new Money(
            left.minorUnits - right.minorUnits);
    }

    public static bool operator >=(
        Money left,
        Money right)
    {
        return left.minorUnits >= right.minorUnits;
    }

    public static bool operator <=(
        Money left,
        Money right)
    {
        return left.minorUnits <= right.minorUnits;
    }
}
