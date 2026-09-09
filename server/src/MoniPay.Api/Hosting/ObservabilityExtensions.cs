using Serilog;

namespace MoniPay.Api.Hosting;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddMoniPayObservability(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSerilog(
            (services, logger) => logger
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services),
            preserveStaticLogger: true,
            writeToProviders: builder.Environment.IsEnvironment(MoniPayEnvironments.Testing));

        return builder;
    }

    public static WebApplication UseMoniPayObservability(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseSerilogRequestLogging(
            options => options.Logger = app.Services.GetRequiredService<Serilog.ILogger>());

        return app;
    }
}
