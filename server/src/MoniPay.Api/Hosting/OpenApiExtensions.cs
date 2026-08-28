using MoniPay.Api.OpenApi;

namespace MoniPay.Api.Hosting;

/// <summary>The generated contract: what the iOS client is built from.</summary>
public static class OpenApiExtensions
{
    public static IServiceCollection AddMoniPayOpenApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddOpenApi(
            options => options.AddDocumentTransformer<SortedOpenApiDocumentTransformer>());
    }

    public static WebApplication MapMoniPayOpenApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Configuration.GetValue(
                MoniPayConfiguration.OpenApiEnabled,
                defaultValue: app.Environment.IsDevelopment()))
        {
            app.MapOpenApi();
        }

        return app;
    }
}
