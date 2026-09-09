namespace MoniPay.Kernel;

public readonly record struct Money(long MinorUnits, Currency Currency)
{
    public static Money Xaf(long francs) => new(francs, Currency.Xaf);

    public static Money Usd(long cents) => new(cents, Currency.Usd);

    public bool IsPositive => MinorUnits > 0;

    public decimal ToDecimal() => MinorUnits / (decimal)Currency.MinorUnitScale();

    public static Money operator +(Money left, Money right)
    {
        Require(left.Currency == right.Currency);
        return left with { MinorUnits = left.MinorUnits + right.MinorUnits };
    }

    public static Money operator -(Money left, Money right)
    {
        Require(left.Currency == right.Currency);
        return left with { MinorUnits = left.MinorUnits - right.MinorUnits };
    }

    public static Money Add(Money left, Money right) => left + right;

    public static Money Subtract(Money left, Money right) => left - right;

    private static void Require(bool sameCurrency)
    {
        if (!sameCurrency)
        {
            throw new InvalidOperationException("Two amounts in different currencies cannot be combined.");
        }
    }
}
