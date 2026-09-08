using MoniPay.Kernel.Http;

namespace MoniPay.Api.Hosting;

/// <summary>
/// Protects marked responses before authentication or rate limiting can short-circuit them.
/// </summary>
internal sealed class NoStoreMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (context.GetEndpoint()?.Metadata.OfType<string>().Contains(MoniPayConventions.NoStore) == true)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.CacheControl = "no-store";
                context.Response.Headers.Pragma = "no-cache";
                return Task.CompletedTask;
            });
        }

        return next(context);
    }
}
