namespace MoniPay.Api.Contracts;

/// <summary>
/// What <c>GET /health</c> answers. Request and response types live in Contracts and never
/// leave the host: a module speaks its own domain types, and an endpoint maps between the two.
/// </summary>
/// <param name="Status">Always <see cref="Healthy"/> when the host answers at all.</param>
/// <param name="Version">The MinVer version of the running assembly.</param>
public sealed record HealthResponse(string Status, string Version)
{
    /// <summary>The only status this endpoint can report: an unhealthy host does not answer.</summary>
    public const string Healthy = "healthy";

    /// <summary>Stands in when the assembly carries no informational version.</summary>
    public const string UnknownVersion = "0.0.0";
}
