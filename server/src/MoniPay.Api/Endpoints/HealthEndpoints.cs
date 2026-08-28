using System.Reflection;
using MoniPay.Api.Contracts;

namespace MoniPay.Api.Endpoints;

/// <summary>
/// The liveness surface. One file per resource under Endpoints/, each exposing a single
/// <c>Map…</c> extension that Program.cs calls; no endpoint body ever sits in Program.cs.
/// Health lives in the host because no module owns it.
/// </summary>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(HealthRoutes.Live, () => new HealthResponse(HealthResponse.Healthy, AssemblyVersion()))
            .WithName(HealthEndpointNames.GetHealth)
            .WithSummary(HealthSummaries.GetHealth)
            .WithTags(HealthTags.Health);

        return app;
    }

    private static string AssemblyVersion() =>
        typeof(HealthEndpoints).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? HealthResponse.UnknownVersion;
}
