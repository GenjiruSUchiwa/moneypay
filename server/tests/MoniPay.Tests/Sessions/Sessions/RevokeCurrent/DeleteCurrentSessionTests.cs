using System.Net;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Sessions.RevokeCurrent;

/// <summary>
/// The revocation of the session the caller's ticket names, and what stops working afterwards.
/// </summary>
public sealed class DeleteCurrentSessionTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task Revoking_returns_204_with_no_body_at_all()
    {
        OpenedSession issued = await Api.CreateSessionAsync();

        using HttpResponseMessage response = await SignUpFlow.RevokeCurrentSessionAsync(
            Client,
            AccessToken(issued));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentType);
        Assert.Equal(0, response.Content.Headers.ContentLength ?? 0);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task A_revoked_session_refuses_both_its_credentials()
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        await SignUpFlow.RevokeCurrentSessionAsync(Client, AccessToken(issued));

        using HttpResponseMessage read = await SignUpFlow.GetCurrentSessionAsync(
            Client,
            AccessToken(issued));
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = await read.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);

        using HttpResponseMessage refresh = await SignUpFlow.PostRefreshAsync(Client, issued);
        await refresh.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_second_revocation_is_401_because_the_credential_no_longer_authenticates()
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        await SignUpFlow.RevokeCurrentSessionAsync(Client, AccessToken(issued));

        using HttpResponseMessage again = await SignUpFlow.RevokeCurrentSessionAsync(
            Client,
            AccessToken(issued));

        await again.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoking_one_session_leaves_another_of_the_same_user_usable()
    {
        UserId user = UserId.New();
        OpenedSession first = await Api.CreateSessionAsync(user);
        OpenedSession second = await Api.CreateSessionAsync(user);
        await SignUpFlow.RevokeCurrentSessionAsync(Client, AccessToken(first));

        using HttpResponseMessage read = await SignUpFlow.GetCurrentSessionAsync(
            Client,
            AccessToken(second));

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        using HttpResponseMessage refresh = await SignUpFlow.PostRefreshAsync(Client, second);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
    }

    [Fact]
    public async Task A_missing_credential_is_401_with_the_bearer_challenge()
    {
        using HttpRequestMessage request = new(HttpMethod.Delete, SignUpFlow.CurrentSessionUrl());
        using HttpResponseMessage response = await Client.SendAsync(request, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal(MoniPayHeaders.Bearer, response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task Accepting_only_problem_json_is_406()
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.ProblemJson);

        using HttpResponseMessage response = await SignUpFlow.RevokeCurrentSessionAsync(
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
        using HttpRequestMessage request = new(HttpMethod.Delete, SignUpFlow.CurrentSessionUrl());

        using HttpResponseMessage response = await client.SendAsync(request, Cancellation);

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
