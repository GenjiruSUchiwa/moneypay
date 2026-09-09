using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MoniPay.Api;
using MoniPay.Api.Hosting;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;
using MoniPay.Tests.Support;
using MoniPay.Users.Features.Registration;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

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
        using WebApplicationFactory<Program> host = Api.CreateHost(builder =>
            builder.UseSetting(MoniPayConfiguration.ForwardedHeadersKnownProxies, "127.0.0.1,::1"));
        using HttpClient client = host.CreateClient();
        int limit = host.Services.GetRequiredService<IOptions<RateLimitOptions>>().Value.StartPerHour;

        for (int request = 0; request < limit; request++)
        {
            Assert.Equal(HttpStatusCode.OK, await ProbeAsync(client, forwardedFor: "203.0.113.1"));
        }

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

    [Fact]
    public async Task An_authenticated_read_burst_above_the_budget_is_rejected_per_session()
    {
        using WebApplicationFactory<Program> host = Api.CreateHost(builder =>
            builder.UseSetting(RateLimitOptions.Keys.AuthenticatedReadPerHour, "2"));
        using HttpClient client = host.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.Accept);
        RegisteredUser user = await Api.RegisterUserAsync(new PhoneNumber(TestPhones.Next()));
        OpenedSession session = await Api.CreateSessionAsync(user.Id);
        string token = TestTokens.Bearer(session.Session.UserId, session.Session.SessionId);

        for (int request = 0; request < 2; request++)
        {
            using HttpResponseMessage allowed = await SignUpFlow.GetCurrentUserAsync(client, token);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        using HttpResponseMessage rejected = await SignUpFlow.GetCurrentUserAsync(client, token);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter);

        OpenedSession other = await Api.CreateSessionAsync(user.Id);
        using HttpResponseMessage another = await SignUpFlow.GetCurrentUserAsync(
            client,
            TestTokens.Bearer(other.Session.UserId, other.Session.SessionId));

        Assert.Equal(HttpStatusCode.OK, another.StatusCode);
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
