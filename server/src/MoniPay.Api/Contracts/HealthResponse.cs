namespace MoniPay.Api.Contracts;

public sealed record HealthResponse(string Status, string Version)
{
    public const string Healthy = "healthy";

    public const string UnknownVersion = "0.0.0";
}
