using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http;
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
        Assert.Equal("no-store", rejected.Headers.CacheControl?.ToString());
        Assert.Equal("no-cache", rejected.Headers.Pragma.ToString());
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

    [Theory]
    [InlineData("")]
    [InlineData("192.0.2.10")]
    public async Task A_forwarded_header_from_an_untrusted_edge_is_ignored(string trustedProxies)
    {
        using WebApplicationFactory<Program> host = Api.CreateHost(builder =>
            builder.UseSetting(MoniPayConfiguration.ForwardedHeadersKnownProxies, trustedProxies));
        int limit = host.Services.GetRequiredService<IOptions<RateLimitOptions>>().Value.StartPerHour;

        for (int request = 0; request <= limit; request++)
        {
            // TestServer leaves RemoteIpAddress null unless the test supplies a real peer.
            HttpContext response = await host.Server.SendAsync(context =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Loopback;
                context.Request.Path = "/test/limited/start";
                context.Request.Headers["X-Forwarded-For"] = $"203.0.113.{request + 1}";
            }, Cancellation);

            Assert.Equal(
                request < limit ? StatusCodes.Status200OK : StatusCodes.Status429TooManyRequests,
                response.Response.StatusCode);
        }
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("127.0.0.1,not-an-ip")]
    [InlineData("127.0.0.1,")]
    public void Invalid_proxy_configuration_prevents_startup(string proxies)
    {
        using WebApplicationFactory<Program> host = Api.CreateHost(builder =>
            builder.UseSetting(MoniPayConfiguration.ForwardedHeadersKnownProxies, proxies));

        Assert.Throws<OptionsValidationException>(() => host.CreateClient());
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
