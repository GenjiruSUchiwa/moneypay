using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MoniPay.Api;
using MoniPay.Api.Hosting;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

/// <summary>
/// The IP rate limits: a budget per client IP, exhausted budgets answered with 429 and
/// <c>Retry-After</c>, and an <c>X-Forwarded-For</c> header an untrusted edge sends is worth
/// nothing.
/// </summary>
public sealed class RateLimitTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task The_request_above_the_budget_is_rejected_with_a_retry_delay()
    {
        using WebApplicationFactory<Program> host = Api.CreateHost();
        using HttpClient client = host.CreateClient();
        int limit = host.Services.GetRequiredService<IOptions<RateLimitOptions>>().Value.StartPerHour;

        for (int request = 0; request < limit; request++)
        {
            Assert.Equal(HttpStatusCode.OK, await ProbeAsync(client));
        }

        using HttpResponseMessage rejected = await SendAsync(client);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(int.TryParse(rejected.Headers.RetryAfter?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds));
        Assert.InRange(seconds, 1, 3_600);
    }

    [Fact]
    public async Task Two_client_ips_have_separate_budgets()
    {
        // The edge is trusted, so the client IP comes from the forwarded header.
        using WebApplicationFactory<Program> host = Api.CreateHost(builder =>
            builder.UseSetting(MoniPayConfiguration.ForwardedHeadersKnownProxies, "127.0.0.1,::1"));
        using HttpClient client = host.CreateClient();
        int limit = host.Services.GetRequiredService<IOptions<RateLimitOptions>>().Value.StartPerHour;

        for (int request = 0; request < limit; request++)
        {
            Assert.Equal(HttpStatusCode.OK, await ProbeAsync(client, forwardedFor: "203.0.113.1"));
        }

        // A second client IP behind the trusted edge starts with a full budget.
        Assert.Equal(HttpStatusCode.OK, await ProbeAsync(client, forwardedFor: "203.0.113.2"));
    }

    [Fact]
    public async Task A_forwarded_header_from_an_untrusted_edge_is_ignored()
    {
        // Nobody is trusted: the connection itself is the client, whatever the header claims.
        using WebApplicationFactory<Program> host = Api.CreateHost();
        using HttpClient client = host.CreateClient();
        int limit = host.Services.GetRequiredService<IOptions<RateLimitOptions>>().Value.StartPerHour;

        for (int request = 0; request < limit; request++)
        {
            string forged = request % 2 == 0 ? "203.0.113.1" : "203.0.113.2";
            Assert.Equal(HttpStatusCode.OK, await ProbeAsync(client, forwardedFor: forged));
        }

        // A rotating forged address must not spread the budget: the real client IP is the key.
        using HttpResponseMessage rejected = await SendAsync(client, forwardedFor: "203.0.113.3");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    private static async Task<HttpStatusCode> ProbeAsync(HttpClient client, string? forwardedFor = null)
    {
        using HttpResponseMessage response = await SendAsync(client, forwardedFor);
        return response.StatusCode;
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string? forwardedFor = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, "/test/limited/start");
        if (forwardedFor is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
