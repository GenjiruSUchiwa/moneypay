using System.Reflection;
using MoniPay.Api.Contracts;

namespace MoniPay.Api.Endpoints;

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
