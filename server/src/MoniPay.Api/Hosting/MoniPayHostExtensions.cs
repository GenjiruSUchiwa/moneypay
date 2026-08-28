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
            .AddMoniPayModules(builder.Configuration)
            .AddMoniPayLocalization()
            .AddMoniPayHealthChecks()
            .AddMoniPayOpenApi()
            .AddProblemDetails();

        return builder;
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
    }
}
