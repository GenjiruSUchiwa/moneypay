using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Sessions.GetCurrent;

/// <summary>
/// The current-session read: a bearer route that names no identifier of its own, so a ticket is
/// the only thing that can select the session it describes.
/// </summary>
public sealed class GetCurrentSessionTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task Reading_returns_the_timing_and_the_user_but_never_a_credential()
    {
        OpenedSession issued = await Api.CreateSessionAsync();

        using HttpResponseMessage response = await SignUpFlow.GetCurrentSessionAsync(
            Client,
            AccessToken(issued));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());

        JsonElement document = await JsonApiAssertions.ReadJsonApiAsync(
            response,
            SessionResourceTypes.Sessions,
            issued.Session.SessionId.ToString());
        JsonElement attributes = document.GetProperty("data").GetProperty("attributes");
        Assert.True(attributes.TryGetProperty("createdAt", out _));
        Assert.True(attributes.TryGetProperty("lastSeenAt", out _));
        Assert.True(attributes.TryGetProperty("accessTokenExpiresAt", out _));
        Assert.False(attributes.TryGetProperty("accessToken", out _));
        Assert.False(attributes.TryGetProperty("refreshToken", out _));
        Assert.False(attributes.TryGetProperty("tokenType", out _));

        JsonElement user = document.GetProperty("data").GetProperty("relationships").GetProperty("user");
        Assert.Equal(SessionResourceTypes.Users, user.GetProperty("data").GetProperty("type").GetString());
        Assert.Equal(issued.Session.UserId.Value.ToString(), user.GetProperty("data").GetProperty("id").GetString());
        Assert.Equal(SessionResources.CurrentUser, user.GetProperty("links").GetProperty("related").GetString());
        Assert.Equal(SessionResources.Self, document.GetProperty("data").GetProperty("links").GetProperty("self").GetString());

        string raw = await response.Content.ReadAsStringAsync(Cancellation);
        Assert.DoesNotContain(AccessToken(issued), raw, StringComparison.Ordinal);
        Assert.DoesNotContain(issued.Session.RefreshToken, raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_expiry_is_the_one_the_presented_token_carries()
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        DateTimeOffset expires = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(7);
        string accessToken = TestTokens.Bearer(issued.Session.UserId, issued.Session.SessionId, expires: expires);

        using HttpResponseMessage response = await SignUpFlow.GetCurrentSessionAsync(Client, accessToken);

        JsonElement document = await JsonApiAssertions.ReadJsonApiAsync(response);
        DateTimeOffset reported = document
            .GetProperty("data")
            .GetProperty("attributes")
            .GetProperty("accessTokenExpiresAt")
            .GetDateTimeOffset();
        Assert.Equal(expires.ToUnixTimeSeconds(), reported.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task Reading_keeps_the_creation_time_and_follows_the_last_seen_time()
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        JsonElement first = await JsonApiAssertions.ReadJsonApiAsync(
            await SignUpFlow.GetCurrentSessionAsync(Client, AccessToken(issued)));

        Api.Time.Advance(TimeSpan.FromMinutes(3));
        using HttpResponseMessage refresh = await SignUpFlow.PostRefreshAsync(Client, issued);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        JsonElement second = await JsonApiAssertions.ReadJsonApiAsync(
            await SignUpFlow.GetCurrentSessionAsync(Client, AccessToken(issued)));

        JsonElement createdAt = first.GetProperty("data").GetProperty("attributes").GetProperty("createdAt");
        JsonElement lastSeenAt = first.GetProperty("data").GetProperty("attributes").GetProperty("lastSeenAt");
        Assert.Equal(createdAt.GetDateTimeOffset(), second.GetProperty("data").GetProperty("attributes").GetProperty("createdAt").GetDateTimeOffset());
        Assert.True(second.GetProperty("data").GetProperty("attributes").GetProperty("lastSeenAt").GetDateTimeOffset()
            > lastSeenAt.GetDateTimeOffset());
    }

    [Fact]
    public async Task A_read_does_not_consume_the_refresh_credential()
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        await SignUpFlow.GetCurrentSessionAsync(Client, AccessToken(issued));

        using HttpResponseMessage refresh = await SignUpFlow.PostRefreshAsync(Client, issued);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
    }

    [Fact]
    public async Task A_missing_credential_is_401_with_the_bearer_challenge()
    {
        using HttpResponseMessage response = await Client.GetAsync(SignUpFlow.CurrentSessionUrl(), Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal(MoniPayHeaders.Bearer, response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task An_expired_credential_is_401()
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        string expired = TestTokens.Bearer(
            issued.Session.UserId,
            issued.Session.SessionId,
            expires: DateTimeOffset.UtcNow - TimeSpan.FromMinutes(1));

        using HttpResponseMessage response = await SignUpFlow.GetCurrentSessionAsync(Client, expired);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task A_workflow_credential_does_not_open_a_session_route()
    {
        OpenedSession issued = await Api.CreateSessionAsync();

        using HttpRequestMessage request = new(HttpMethod.Get, SignUpFlow.CurrentSessionUrl());
        request.Headers.Authorization = new(MoniPayHeaders.SignUp, "not-a-token");
        using HttpResponseMessage response = await Client.SendAsync(request, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task A_ticket_for_another_session_does_not_read_this_one()
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        string foreign = TestTokens.Bearer(issued.Session.UserId, Guid.CreateVersion7());

        using HttpResponseMessage response = await SignUpFlow.GetCurrentSessionAsync(Client, foreign);

        await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Accepting_only_problem_json_is_406()
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.ProblemJson);

        using HttpResponseMessage response = await SignUpFlow.GetCurrentSessionAsync(
            client,
            AccessToken(issued));

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.NotAcceptable);
        Assert.Equal(MoniPayErrorTypes.NotAcceptable.Urn, problem.Type);
    }

    [Theory]
    [InlineData("en", "The session token is invalid or expired.")]
    [InlineData("fr", "Le jeton de session est invalide ou expiré.")]
    public async Task Problems_are_localized(string culture, string title)
    {
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);

        using HttpResponseMessage response = await client.GetAsync(SignUpFlow.CurrentSessionUrl(), Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal(title, problem.Title);
    }

    /// <summary>
    /// An access credential for the session: the harness's bearer validation runs on the system
    /// clock, so the JWT a session was issued with — stamped by the fake clock — is not the one
    /// under test here. The claims are what matter, and they name the real session.
    /// </summary>
    private static string AccessToken(OpenedSession session) =>
        TestTokens.Bearer(session.Session.UserId, session.Session.SessionId);
}
