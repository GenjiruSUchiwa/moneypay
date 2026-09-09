namespace MoniPay.Kernel;

public enum Currency
{
    Xaf = 950,

    Usd = 840,
}

public static class CurrencyExtensions
{
    public static int MinorUnitScale(this Currency currency) => currency switch
    {
        Currency.Xaf => 1,
        Currency.Usd => 100,
        _ => throw new ArgumentOutOfRangeException(nameof(currency)),
    };
}
