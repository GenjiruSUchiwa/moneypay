using Microsoft.AspNetCore.Http;
using MoniPay.Kernel.Http;

namespace MoniPay.Api.Hosting;

internal sealed class NoStoreMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (AppliesTo(context))
        {
            context.Response.OnStarting(() =>
            {
                Apply(context.Response);
                return Task.CompletedTask;
            });
        }

        return next(context);
    }

    private static bool AppliesTo(HttpContext context) =>
        context.GetEndpoint()?.Metadata.OfType<string>().Contains(MoniPayConventions.NoStore) == true;

    private static void Apply(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
    }
}
