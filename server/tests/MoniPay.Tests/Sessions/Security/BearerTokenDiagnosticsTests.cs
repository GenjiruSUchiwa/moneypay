using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Sessions;
using MoniPay.Tests.Fakes;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

public sealed class BearerTokenDiagnosticsTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private const string SecureRoute = "/test/secure";

    private const string JwtBearerCategory = "Microsoft.AspNetCore.Authentication.JwtBearer";

    [Fact]
    public async Task An_expired_bearer_is_diagnosed_as_expired()
    {
        string token = TestTokens.Bearer(
            UserId.New(),
            Guid.NewGuid(),
            expires: DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5));

        await AssertDiagnosedAsync(token, "expired");
    }

    [Fact]
    public async Task A_bearer_signed_by_an_unknown_key_is_diagnosed_as_an_invalid_signature()
    {
        string token = TestTokens.Bearer(
            UserId.New(),
            Guid.NewGuid(),
            key: Convert.FromBase64String(TestKeys.OtherSigning));

        await AssertDiagnosedAsync(token, "invalid-signature");
    }

    private async Task AssertDiagnosedAsync(string token, string cause)
    {
        int before = Api.Logs.Entries.Count;

        using HttpResponseMessage response = await SendAsync(token);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized, requireNoStore: false);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
        Assert.Null(problem.Detail);

        string body = await response.Content.ReadAsStringAsync(Cancellation);
        Assert.DoesNotContain(token, body, StringComparison.Ordinal);
        Assert.DoesNotContain(token, response.Headers.WwwAuthenticate.ToString(), StringComparison.Ordinal);

        RecordingLoggerProvider.LogEntry entry = Assert.Single(Api.Logs.Entries.Skip(before), IsBearerRefusal);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(cause, Cause(entry));
        Assert.Null(entry.Exception);
        Assert.DoesNotContain(token, entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(token, Values(entry), StringComparison.Ordinal);

        Assert.DoesNotContain(
            Api.Logs.Entries.Skip(before),
            log => log.Category.StartsWith(JwtBearerCategory, StringComparison.Ordinal));
        Assert.DoesNotContain(
            Api.Logs.Entries.Skip(before),
            log => log.Message.Contains("Failed to validate token", StringComparison.Ordinal));
    }

    private async Task<HttpResponseMessage> SendAsync(string token)
    {
        using HttpClient client = Api.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, SecureRoute)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
        };

        return await client.SendAsync(request, Cancellation);
    }

    private static bool IsBearerRefusal(RecordingLoggerProvider.LogEntry entry) =>
        entry.Category == typeof(SessionMessages).FullName
        && entry.Message.Contains("bearer token was refused", StringComparison.Ordinal);

    private static string Cause(RecordingLoggerProvider.LogEntry entry) =>
        entry.State.First(pair => pair.Key == "Cause").Value as string
        ?? throw new Xunit.Sdk.XunitException("The bearer-refusal event carries no Cause property.");

    private static string Values(RecordingLoggerProvider.LogEntry entry) =>
        string.Join('|', entry.State.Select(pair => $"{pair.Key}={pair.Value}"));
}
