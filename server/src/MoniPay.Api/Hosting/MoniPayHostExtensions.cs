using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MoniPay.Api.Errors;

namespace MoniPay.Api.Hosting;

public static class MoniPayHostExtensions
{
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

    public static async Task<WebApplication> UseMoniPayAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMoniPayPipeline();
        await app.ApplyMigrationsIfConfiguredAsync();
        app.MapMoniPayEndpoints();

        return app;
    }

    private static void MapMoniPayEndpoints(this WebApplication app)
    {
        app.MapMoniPayOpenApi();
        app.MapMoniPayHealthChecks();
        app.MapMoniPayModules();

        if (app.Environment.IsEnvironment(MoniPayEnvironments.Testing))
        {
            app.MapTestProbes();
        }
    }
}
