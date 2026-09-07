using System.Net;
using MoniPay.Kernel;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

/// <summary>
/// The <see cref="MoniPayPolicies.AuthenticatedUser"/> policy: a valid access token whose
/// session has ended no longer opens anything, and the check it makes is one database query per
/// request, never a cache.
/// </summary>
public sealed class ActiveSessionPolicyTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task An_active_session_opens_a_protected_route()
    {
        using HttpClient client = Api.CreateClient();
        string token = await TestTokensForNewSessionAsync();
        Api.Queries.Reset();

        using HttpRequestMessage request = Bearer(token);
        using HttpResponseMessage response = await client.SendAsync(request, Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, Api.Queries.SessionQueries);
    }

    [Fact]
    public async Task A_revoked_session_is_refused_despite_a_still_valid_token()
    {
        using HttpClient client = Api.CreateClient();
        SessionTokenResult issued = await CreateSessionAsync();
        await RevokeAsync(issued.SessionId);
        Api.Queries.Reset();

        using HttpRequestMessage request = Bearer(TestTokens.Bearer(issued.UserId, issued.SessionId));
        using HttpResponseMessage response = await client.SendAsync(request, Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, Api.Queries.SessionQueries);
    }

    [Fact]
    public async Task A_request_without_a_token_is_challenged()
    {
        using HttpClient client = Api.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/test/secure", Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    /// <summary>One request, one query: the active-session check is answered from the database.</summary>
    [Fact]
    public async Task The_active_session_check_hits_the_database_once_per_request()
    {
        using HttpClient client = Api.CreateClient();
        string token = await TestTokensForNewSessionAsync();
        Api.Queries.Reset();

        using HttpRequestMessage request = Bearer(token);
        await client.SendAsync(request, Cancellation);

        Assert.Equal(1, Api.Queries.SessionQueries);
    }

    private async Task<SessionTokenResult> CreateSessionAsync() =>
        await Api.InScopeAsync<SessionTokenService, SessionTokenResult>(
            (sessions, cancellationToken) => sessions.CreateAsync(UserId.New(), Guid.CreateVersion7(), cancellationToken));

    private async Task<string> TestTokensForNewSessionAsync()
    {
        SessionTokenResult issued = await CreateSessionAsync();
        return TestTokens.Bearer(issued.UserId, issued.SessionId);
    }

    private Task RevokeAsync(Guid sessionId) =>
        Api.InScopeAsync<SessionTokenService, ValueTuple>((sessions, cancellationToken) =>
            RevokeInScopeAsync(sessions, sessionId, cancellationToken));

    private static async Task<ValueTuple> RevokeInScopeAsync(
        SessionTokenService sessions,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        await sessions.RevokeAsync(sessionId, cancellationToken);
        return default;
    }

    private static HttpRequestMessage Bearer(string token)
    {
        HttpRequestMessage request = new(HttpMethod.Get, "/test/secure");
        request.Headers.Authorization = new("Bearer", token);
        return request;
    }
}
