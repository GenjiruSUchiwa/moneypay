using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MoniPay.Api.Errors;

namespace MoniPay.Api.Hosting;

/// <summary>
/// The two calls that make a MoniPay host. Program.cs stays five statements long, and the
/// composition lives here where it can be read as a list.
///
/// Each step below is one focused extension, and this facade only names them. That is what
/// keeps every type under the class-coupling limit: a composition root is coupled to everything
/// by definition, so the coupling is spread across small single-purpose extensions instead of
/// being suppressed on one large method.
/// </summary>
public static class MoniPayHostExtensions
{
    /// <summary>Everything the host needs before it is built.</summary>
    public static WebApplicationBuilder AddMoniPay(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddMoniPayObservability();

        builder.Services
            .AddTransient<IConfigureOptions<ForwardedHeadersOptions>, ForwardedHeadersOptionsSetup>();

        builder.Services
            .AddMoniPayModules(builder.Configuration)
            .AddMoniPayRateLimiter(builder.Configuration)
            .AddMoniPayLocalization()
            .AddMoniPayHealthChecks()
            .AddMoniPayOpenApi()
            .AddMoniPayProblemDetails();

        return builder;
    }

    /// <summary>
    /// The IP rate limits: the options they are tuned by, and the fixed-window limiters the
    /// modules' route groups attach by name.
    /// </summary>
    private static IServiceCollection AddMoniPayRateLimiter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .Validate(limits => limits.StartPerHour >= 1
                    && limits.ResendPerHour >= 1
                    && limits.VerifyPerHour >= 1
                    && limits.CompletePerHour >= 1
                    && limits.RefreshPerHour >= 1,
                "The MoniPay:RateLimits limits are invalid: every limit must be at least one per hour.")
            .ValidateOnStart();
        services.AddTransient<IConfigureOptions<RateLimiterOptions>, RateLimiterSetup>();
        return services.AddRateLimiter();
    }

    /// <summary>
    /// The pipeline and the routes, in order. Async because applying migrations is: a host that
    /// blocks its own startup thread on a database round trip is a deadlock waiting for load.
    /// </summary>
    public static async Task<WebApplication> UseMoniPayAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMoniPayPipeline();
        await app.ApplyMigrationsIfConfiguredAsync();
        app.MapMoniPayEndpoints();

        return app;
    }

    /// <summary>
    /// The HTTP surface. Health and the OpenAPI document belong to the host; every other route
    /// belongs to the module that owns the data behind it.
    /// </summary>
    private static void MapMoniPayEndpoints(this WebApplication app)
    {
        app.MapMoniPayOpenApi();
        app.MapMoniPayHealthChecks();
        app.MapMoniPayModules();

        // The throwaway probe endpoints the security tests mount their credentials on. They
        // exist in the Testing environment only, and disappear when the real routes land.
        if (app.Environment.IsEnvironment(MoniPayEnvironments.Testing))
        {
            app.MapTestProbes();
        }
    }
}
