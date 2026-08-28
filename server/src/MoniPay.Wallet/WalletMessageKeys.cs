namespace MoniPay.Wallet;

/// <summary>
/// The resource keys in <c>Resources/WalletMessages.resx</c>. A localizer returns the key itself
/// when it finds no entry, so a typo in a literal is a silent English-looking string shipped to
/// a French user. Naming them here makes that a compile error instead.
/// </summary>
public static class WalletMessageKeys
{
    public const string UnsupportedCurrency = nameof(UnsupportedCurrency);
}
