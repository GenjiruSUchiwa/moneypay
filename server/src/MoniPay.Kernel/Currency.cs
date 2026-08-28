namespace MoniPay.Kernel;

/// <summary>The currencies MoniPay holds. The wallet is XAF; a virtual card is USD.</summary>
public enum Currency
{
    /// <summary>CFA franc BEAC. ISO 4217 gives it zero decimal places.</summary>
    Xaf = 950,

    /// <summary>United States dollar, two decimal places.</summary>
    Usd = 840,
}

public static class CurrencyExtensions
{
    /// <summary>How many minor units make one major unit: 1 for XAF, 100 for USD.</summary>
    public static int MinorUnitScale(this Currency currency) => currency switch
    {
        Currency.Xaf => 1,
        Currency.Usd => 100,
        _ => throw new ArgumentOutOfRangeException(nameof(currency)),
    };
}
