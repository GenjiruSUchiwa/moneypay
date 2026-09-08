namespace MoniPay.Api.Hosting;

/// <summary>
/// The middleware pipeline, in order. Order is behaviour here, not style: the forwarded headers
/// run first so the rate limiter partitions by the client IP and not by the edge's; localization
/// runs before anything that can produce text a user reads; and the exception handler runs
/// before the endpoints it protects. Rate limiting and authentication come after, so a rejected
/// or refused request is answered without ever reaching a handler.
/// </summary>
public static class PipelineExtensions
{
    public static WebApplication UseMoniPayPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Before the limiter: it partitions by the client IP, which only the edge can name.
        app.UseForwardedHeaders();

        // Before the exception handler and the endpoints: both can produce text the user reads.
        app.UseRequestLocalization();
        app.UseMoniPayObservability();
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseRouting();
        app.UseMiddleware<NoStoreMiddleware>();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
