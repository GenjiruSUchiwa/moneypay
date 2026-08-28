namespace MoniPay.Wallet.Endpoints;

/// <summary>
/// The endpoint summaries that land in the OpenAPI document.
/// These are deliberately NOT resx-backed: they describe the API to the developer reading the
/// contract, not to the user holding the phone. Only text a user reads is localized — see
/// <see cref="MoniPay.Wallet.WalletMessages"/>.
/// </summary>
public static class WalletSummaries
{
    public const string GetWallet = "Returns the balance, the holds, and what is available to spend.";
}
