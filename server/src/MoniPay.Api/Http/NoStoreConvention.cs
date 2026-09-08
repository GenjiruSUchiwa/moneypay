using Microsoft.AspNetCore.Http;
using MoniPay.Kernel.Http;

namespace MoniPay.Api.Http;

/// <summary>
/// The <c>no-store</c> convention: a marked endpoint's responses must never be cached, because
/// credentials and personal data travel there. The middleware applies it to every response of a
/// marked route; the problem writer applies it to an error body it formats directly.
/// </summary>
internal static class NoStoreConvention
{
    public static bool AppliesTo(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetEndpoint()?.Metadata.OfType<string>().Contains(MoniPayConventions.NoStore) == true;
    }

    public static void Apply(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
    }
}
