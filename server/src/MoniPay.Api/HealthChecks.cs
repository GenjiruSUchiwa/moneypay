using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MoniPay.Api.Endpoints;
using MoniPay.Persistence;

namespace MoniPay.Api;

/// <summary>
/// The names and tags of the registered health checks. The tag is the filter that decides which
/// checks readiness runs, so a typo silently produces an endpoint that checks nothing and
/// reports healthy — the exact failure a probe exists to prevent.
/// </summary>
public static class HealthChecks
{
    public static class Names
    {
        public const string Database = "postgresql";
    }

    public static class Tags
    {
        /// <summary>Checks a dependency the app cannot serve traffic without.</summary>
        public const string Ready = "ready";
    }
}

public static class HealthCheckExtensions
{
    /// <summary>Liveness answers as long as the process is up; readiness also asks the database.</summary>
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

        // Liveness is the module-free one, so it is an ordinary endpoint in Endpoints/.
        app.MapHealthEndpoints();
        app.MapHealthChecks(
            HealthRoutes.Ready,
            new HealthCheckOptions { Predicate = check => check.Tags.Contains(HealthChecks.Tags.Ready) });

        return app;
    }
}
