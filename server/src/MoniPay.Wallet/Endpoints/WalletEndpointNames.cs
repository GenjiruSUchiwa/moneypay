namespace MoniPay.Wallet.Endpoints;

/// <summary>
/// The endpoint names. These reach further than a route does: they become the
/// <c>operationId</c> in the OpenAPI document, which the generated Swift client turns into a
/// method name. Renaming one is a breaking change for the client, so it is declared, not typed.
/// </summary>
public static class WalletEndpointNames
{
    public const string GetWallet = nameof(GetWallet);
}
