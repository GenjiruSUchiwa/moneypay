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

/// <summary>
/// A rejected access JWT keeps a generic public challenge and leaves its cause in the module's own
/// log. The framework's token diagnostics are Information level, which production filters out, so
/// the module records the cause itself at Warning — as a bounded word, never the token, the
/// credentials or the validation exception's message.
/// </summary>
public sealed class BearerTokenDiagnosticsTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private const string SecureRoute = "/test/secure";

    /// <summary>The framework category the bearer handler logs its own diagnostics under.</summary>
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

        // The public answer stays generic, and it never echoes the credential.
        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized, requireNoStore: false);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
        Assert.Null(problem.Detail);

        string body = await response.Content.ReadAsStringAsync(Cancellation);
        Assert.DoesNotContain(token, body, StringComparison.Ordinal);
        Assert.DoesNotContain(token, response.Headers.WwwAuthenticate.ToString(), StringComparison.Ordinal);

        // The cause survives the production filtering, as a bounded category.
        RecordingLoggerProvider.LogEntry entry = Assert.Single(Api.Logs.Entries.Skip(before), IsBearerRefusal);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(cause, Cause(entry));
        Assert.Null(entry.Exception);
        Assert.DoesNotContain(token, entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(token, Values(entry), StringComparison.Ordinal);

        // The framework's own bearer diagnostics are Information level under
        // Microsoft.AspNetCore, which the production configuration filters out; nothing may
        // depend on them. The module's Warning event above is what survives that filter.
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
