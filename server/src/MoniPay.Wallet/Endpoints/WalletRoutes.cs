namespace MoniPay.Wallet.Endpoints;

/// <summary>
/// The paths this module serves. A route is named once here so the endpoint, a test and any
/// future link builder cannot drift apart, and so renaming a path is one edit the compiler
/// checks rather than a search across string literals.
/// </summary>
public static class WalletRoutes
{
    /// <summary>The prefix every wallet route hangs off.</summary>
    public const string Group = "/wallet";

    /// <summary>
    /// The group itself: <c>GET /wallet</c>. Empty, not "/", so the served path and the path in
    /// the generated OpenAPI document stay <c>/wallet</c>.
    /// </summary>
    public const string Root = "";

    /// <summary>
    /// The currency query parameter. Minimal APIs bind a query value by the handler's parameter
    /// name, so this must stay identical to the <c>currency</c> parameter of the handler; it is
    /// declared for the callers that build the URL, which the binder cannot check for them.
    /// </summary>
    public const string CurrencyQuery = "currency";
}
