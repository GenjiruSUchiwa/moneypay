using Microsoft.AspNetCore.Http;
using MoniPay.Kernel.Http;

namespace MoniPay.Sessions.Security;

/// <summary>
/// Marks every response of a group that carries the <see cref="MoniPayConventions.NoStore"/>
/// marker as uncacheable. Credentials travel on these routes; a shared cache must never hold a
/// response that names one. The headers are written on response start, so a late failure — an
/// authorization challenge after the handler ran — is covered too.
/// </summary>
public sealed class NoStoreEndpointFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<string>() is not null)
        {
            context.HttpContext.Response.OnStarting(WriteHeaders, context.HttpContext);
        }

        return next(context);
    }

    private static Task WriteHeaders(object? state)
    {
        if (state is HttpContext http)
        {
            http.Response.Headers.CacheControl = "no-store";
            http.Response.Headers.Pragma = "no-cache";
        }

        return Task.CompletedTask;
    }
}
