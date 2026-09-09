using MoniPay.Api.Http;

namespace MoniPay.Api.Hosting;

/// <summary>
/// The middleware pipeline, in order. Order is behaviour here, not style: the forwarded headers
/// run first so the rate limiter partitions by the client IP and not by the edge's; localization
/// runs before anything that can produce text a user reads; and the exception handler runs
/// before the endpoints it protects. Rate limiting and authentication come after, so a rejected
/// or refused request is answered without ever reaching a handler. The JSON:API transport check
/// comes last, after authentication, so the security middleware still short-circuits first.
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

        // The limiter runs after authentication so an authenticated policy can partition on the
        // session the ticket names rather than on the client IP every caller behind one NAT
        // shares. The anonymous routes it also guards present no credential, so nothing is
        // validated before their budget is checked.
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();

        app.UseMiddleware<JsonApiTransportMiddleware>();

        return app;
    }
}
