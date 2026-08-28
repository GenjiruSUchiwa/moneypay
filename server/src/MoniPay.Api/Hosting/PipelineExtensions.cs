namespace MoniPay.Api.Hosting;

/// <summary>
/// The middleware pipeline, in order. Order is behaviour here, not style: localization runs
/// before anything that can produce text a user reads, and the exception handler runs before the
/// endpoints it protects.
/// </summary>
public static class PipelineExtensions
{
    public static WebApplication UseMoniPayPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Before the exception handler and the endpoints: both can produce text the user reads.
        app.UseRequestLocalization();
        app.UseMoniPayObservability();
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        return app;
    }
}
