using MoniPay.Api.Http;

namespace MoniPay.Api.Hosting;

public static class PipelineExtensions
{
    public static WebApplication UseMoniPayPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseForwardedHeaders();

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

        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();

        app.UseMiddleware<JsonApiTransportMiddleware>();

        return app;
    }
}
