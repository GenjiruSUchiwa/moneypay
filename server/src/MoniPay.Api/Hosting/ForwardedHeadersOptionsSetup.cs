using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using MoniPay.Api;

namespace MoniPay.Api.Hosting;

/// <summary>
/// The forwarded-headers configuration the rate limiter's IP partitioning needs: the client IP
/// is read from <c>X-Forwarded-For</c>, but only when the connection itself comes from an edge
/// the host names. Networks are never trusted wholesale — an unlisted edge cannot forge a
/// client IP, and a direct client cannot forge its own.
/// </summary>
internal sealed class ForwardedHeadersOptionsSetup(IConfiguration configuration) : IConfigureOptions<ForwardedHeadersOptions>
{
    public void Configure(ForwardedHeadersOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        IConfigurationSection proxiesSection = configuration.GetSection(MoniPayConfiguration.ForwardedHeadersKnownProxies);
        // A flat value ("10.0.0.5,10.0.0.6") does not bind to an array; split it by hand.
        string[] proxies = proxiesSection.Get<string[]>() is { Length: > 0 } listed
            ? listed
            : proxiesSection.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? [];
        if (proxies.Length == 0)
        {
            // No edge is trusted, and an empty trust list means the middleware would trust every
            // peer. With nobody named, the headers are simply not read: the client IP is the
            // connection's own.
            options.ForwardedHeaders = ForwardedHeaders.None;
            return;
        }

        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        foreach (string proxy in proxies)
        {
            if (IPAddress.TryParse(proxy, out IPAddress? address))
            {
                options.KnownProxies.Add(address);
            }
        }
    }
}
