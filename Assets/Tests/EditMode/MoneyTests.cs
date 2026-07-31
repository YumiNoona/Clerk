using NUnit.Framework;

public sealed class MoneyTests
{
    [Test]
    public void FromFloat_RoundsToMinorUnits()
    {
        Money value = Money.FromFloat(12.345f);

        Assert.That(value.MinorUnits,Is.EqualTo(1235));
    }

    [Test]
    public void Arithmetic_PreservesExactMinorUnits()
    {
        Money first = new Money(105);
        Money second = new Money(210);

        Assert.That(
            (first + second).MinorUnits,
            Is.EqualTo(315));

        Assert.That(
            (second - first).MinorUnits,
            Is.EqualTo(105));
    }
}
