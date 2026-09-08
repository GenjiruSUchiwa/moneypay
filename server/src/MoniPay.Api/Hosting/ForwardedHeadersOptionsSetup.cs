using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace MoniPay.Api.Hosting;

/// <summary>
/// Trusts only explicitly configured proxy addresses for client IP and protocol forwarding.
/// </summary>
internal sealed class ForwardedHeadersOptionsSetup(IConfiguration configuration) : IConfigureOptions<ForwardedHeadersOptions>
{
    public void Configure(ForwardedHeadersOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        IConfigurationSection section = configuration.GetSection(MoniPayConfiguration.ForwardedHeadersKnownProxies);
        string? configured = section.Value;
        string[] proxies = string.IsNullOrWhiteSpace(configured)
            ? []
            : configured.Split(',', StringSplitOptions.TrimEntries);
        if (section.GetChildren().Any() || proxies.Any(proxy => !IPAddress.TryParse(proxy, out _)))
        {
            throw new OptionsValidationException(
                Options.DefaultName,
                typeof(ForwardedHeadersOptions),
                [$"{MoniPayConfiguration.ForwardedHeadersKnownProxies} must be a comma-separated list of IP addresses."]);
        }

        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (string proxy in proxies)
        {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        // Empty trust lists allow every peer, so forwarding must be disabled without named edges.
        options.ForwardedHeaders = proxies.Length == 0
            ? ForwardedHeaders.None
            : ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    }
}
