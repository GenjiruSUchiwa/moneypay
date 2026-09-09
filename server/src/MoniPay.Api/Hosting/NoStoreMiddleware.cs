using Microsoft.AspNetCore.Http;
using MoniPay.Kernel.Http;

namespace MoniPay.Api.Hosting;

/// <summary>
/// The single owner of the <c>no-store</c> policy: a response of a marked endpoint must never be
/// cached, because credentials and personal data travel there. It runs after routing, so the
/// endpoint is known, and before rate limiting and authentication, so a response they
/// short-circuit is marked too. Setting the headers through <c>OnStarting</c> also covers a
/// response the exception handler formats further up the pipeline, and it never overwrites the
/// independent headers a response already carries, such as <c>WWW-Authenticate</c>,
/// <c>Allow</c> or <c>Retry-After</c>.
/// </summary>
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
