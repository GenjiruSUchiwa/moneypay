using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MoniPay.Api.Endpoints;
using MoniPay.Persistence;

namespace MoniPay.Api;

public static class HealthChecks
{
    public static class Names
    {
        public const string Database = "postgresql";
    }

    public static class Tags
    {
        public const string Ready = "ready";
    }
}

public static class HealthCheckExtensions
{
    public static IServiceCollection AddMoniPayHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddDbContextCheck<MoniPayDbContext>(HealthChecks.Names.Database, tags: [HealthChecks.Tags.Ready]);

        return services;
    }

    public static WebApplication MapMoniPayHealthChecks(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthEndpoints();
        app.MapHealthChecks(
            HealthRoutes.Ready,
            new HealthCheckOptions { Predicate = check => check.Tags.Contains(HealthChecks.Tags.Ready) });

        return app;
    }
}
