namespace MoniPay.Kernel;

/// <summary>
/// An amount in one currency, held in minor units. XAF has no minor unit, so 1 FCFA is
/// <c>1</c>; USD has two, so $1.00 is <c>100</c>. An integer count of minor units is the only
/// representation that survives arithmetic and a round trip through the database, which is why
/// no amount in this codebase is ever a <see cref="double"/>.
/// </summary>
public readonly record struct Money(long MinorUnits, Currency Currency)
{
    public static Money Xaf(long francs) => new(francs, Currency.Xaf);

    public static Money Usd(long cents) => new(cents, Currency.Usd);

    public bool IsPositive => MinorUnits > 0;

    /// <summary>The amount as a decimal, for display and for a caller that must divide.</summary>
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
