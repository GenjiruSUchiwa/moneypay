using Microsoft.AspNetCore.Http;
using MoniPay.Api.Http;

namespace MoniPay.Api.Hosting;

/// <summary>
/// Protects marked responses before authentication or rate limiting can short-circuit them.
/// </summary>
internal sealed class NoStoreMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (NoStoreConvention.AppliesTo(context))
        {
            context.Response.OnStarting(() =>
            {
                NoStoreConvention.Apply(context.Response);
                return Task.CompletedTask;
            });
        }

        return next(context);
    }
}
