namespace MoniPay.Kernel.Http;

/// <summary>
/// Endpoint metadata marker names a module puts on a route group so the host applies a
/// cross-cutting behavior without the module referencing the host.
/// </summary>
public static class MoniPayConventions
{
    /// <summary>JSON:API content negotiation for the group.</summary>
    public const string JsonApi = nameof(JsonApi);

    /// <summary><c>Cache-Control: no-store</c> on every response from the group.</summary>
    public const string NoStore = nameof(NoStore);
}
