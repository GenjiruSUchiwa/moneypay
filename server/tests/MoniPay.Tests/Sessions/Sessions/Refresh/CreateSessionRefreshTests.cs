using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Sessions.Features.Sessions.Refresh;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Sessions.Refresh;

/// <summary>
/// The refresh route: an anonymous rotation whose credential travels in the body. A token is
/// returned once, consumed by the rotation, and a second use of it revokes the whole family.
/// </summary>
public sealed class CreateSessionRefreshTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task A_refresh_returns_new_credentials_and_consumes_the_presented_token()
    {
        OpenedSession issued = await Api.CreateSessionAsync();

        using HttpResponseMessage response = await SignUpFlow.PostRefreshAsync(Client, issued);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());

        JsonElement document = await JsonApiAssertions.ReadJsonApiAsync(
            response,
            SessionResourceTypes.Sessions,
            issued.Session.SessionId.ToString());
        JsonElement attributes = document.GetProperty("data").GetProperty("attributes");
        Assert.Equal(SessionResources.BearerTokenType, attributes.GetProperty("tokenType").GetString());
        Assert.False(string.IsNullOrEmpty(attributes.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrEmpty(attributes.GetProperty("refreshToken").GetString()));
        Assert.NotEqual(issued.Session.RefreshToken, attributes.GetProperty("refreshToken").GetString());
        Assert.True(attributes.GetProperty("accessTokenExpiresAt").GetDateTimeOffset() > Api.Time.GetUtcNow());
        Assert.True(attributes.GetProperty("refreshTokenExpiresAt").GetDateTimeOffset()
            > attributes.GetProperty("accessTokenExpiresAt").GetDateTimeOffset());

        JsonElement user = document.GetProperty("data").GetProperty("relationships").GetProperty("user");
        Assert.Equal(SessionResourceTypes.Users, user.GetProperty("data").GetProperty("type").GetString());
        Assert.Equal(issued.Session.UserId.Value.ToString(), user.GetProperty("data").GetProperty("id").GetString());
        Assert.Equal(MoniPayRoutes.CurrentUser, user.GetProperty("links").GetProperty("related").GetString());
        Assert.Equal(SessionResources.Self, document.GetProperty("data").GetProperty("links").GetProperty("self").GetString());
    }

    [Fact]
    public async Task A_replayed_token_is_refused_and_revokes_the_family()
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        string replacement = await RefreshTokenOfAsync(issued);

        using HttpResponseMessage replay = await SignUpFlow.PostRefreshAsync(
            Client,
            issued.Session.RefreshToken,
            issued.DeviceId);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await replay.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.RefreshTokenReused.Urn, problem.Type);

        // The family is revoked, so the credentials the successful refresh just handed out are
        // refused too: a replay closes the door, it does not mint a third token.
        using HttpResponseMessage afterReplay = await SignUpFlow.PostRefreshAsync(
            Client,
            replacement,
            issued.DeviceId);
        await afterReplay.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(HttpStatusCode.Unauthorized, await SecureProbeAsync(issued.Session.AccessToken.Value));
    }

    [Fact]
    public async Task A_well_formed_token_nobody_issued_is_refused_without_saying_why()
    {
        string unknown = new RefreshTokenFactory().Create().Raw;

        using HttpResponseMessage response = await SignUpFlow.PostRefreshAsync(Client, unknown, Guid.CreateVersion7());

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task A_null_or_empty_token_is_422_at_its_pointer(string? token)
    {
        using HttpResponseMessage response = await SignUpFlow.PostRefreshAsync(Client, token, Guid.CreateVersion7());

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(MoniPayErrorTypes.Validation.Urn, problem.Type);
        Assert.Equal(CreateSessionRefreshPointers.RefreshToken, Assert.Single(problem.Pointers()));
    }

    [Theory]
    [InlineData(42)]
    [InlineData(44)]
    public async Task A_token_of_the_wrong_length_is_422_at_its_pointer(int length)
    {
        using HttpResponseMessage response = await SignUpFlow.PostRefreshAsync(
            Client,
            new string('a', length),
            Guid.CreateVersion7());

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(MoniPayErrorTypes.Validation.Urn, problem.Type);
        Assert.Equal(CreateSessionRefreshPointers.RefreshToken, Assert.Single(problem.Pointers()));
    }

    [Theory]
    [InlineData('=')]
    [InlineData(' ')]
    [InlineData('+')]
    [InlineData('/')]
    [InlineData('é')]
    public async Task A_token_outside_the_base64url_alphabet_is_422_at_its_pointer(char last)
    {
        string token = new string('a', RefreshTokenFactory.RawLength - 1) + last;

        using HttpResponseMessage response = await SignUpFlow.PostRefreshAsync(Client, token, Guid.CreateVersion7());

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(CreateSessionRefreshPointers.RefreshToken, Assert.Single(problem.Pointers()));
    }

    [Fact]
    public async Task A_refresh_token_that_is_not_a_string_is_a_document_failure()
    {
        using StringContent body = new(
            """{"data":{"type":"session-refreshes","attributes":{"refreshToken":42,"deviceId":"1207158c-15fc-446d-a28a-702c564332ef"}}}""",
            Encoding.UTF8);
        body.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);
        using HttpRequestMessage request = new(HttpMethod.Post, SignUpFlow.RefreshUrl()) { Content = body };

        using HttpResponseMessage response = await Client.SendAsync(request, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(MoniPayErrorTypes.JsonApiDocumentInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task An_empty_device_identifier_is_422_at_its_pointer()
    {
        OpenedSession issued = await Api.CreateSessionAsync();

        using HttpResponseMessage response = await SignUpFlow.PostRefreshAsync(
            Client,
            issued.Session.RefreshToken,
            Guid.Empty);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(CreateSessionRefreshPointers.DeviceId, Assert.Single(problem.Pointers()));

        // A refused refresh consumes nothing: the token still works from the right device.
        Assert.NotEmpty(await RefreshTokenOfAsync(issued));
    }

    [Fact]
    public async Task A_foreign_device_is_401_and_leaves_the_token_usable()
    {
        OpenedSession issued = await Api.CreateSessionAsync();

        using HttpResponseMessage response = await SignUpFlow.PostRefreshAsync(
            Client,
            issued.Session.RefreshToken,
            Guid.CreateVersion7());

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);

        Assert.NotEmpty(await RefreshTokenOfAsync(issued));
    }

    [Fact]
    public async Task Accepting_only_problem_json_is_406()
    {
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.ProblemJson);
        using HttpRequestMessage request = new(HttpMethod.Post, SignUpFlow.RefreshUrl())
        {
            Content = SignUpFlow.RefreshBody(new string('a', RefreshTokenFactory.RawLength), Guid.CreateVersion7()),
        };

        using HttpResponseMessage response = await client.SendAsync(request, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.NotAcceptable);
        Assert.Equal(MoniPayErrorTypes.NotAcceptable.Urn, problem.Type);
    }

    [Fact]
    public async Task A_body_that_is_not_jsonapi_is_415()
    {
        using StringContent body = new("""{"data":{"type":"session-refreshes"}}""", Encoding.UTF8, "application/json");
        using HttpRequestMessage request = new(HttpMethod.Post, SignUpFlow.RefreshUrl()) { Content = body };

        using HttpResponseMessage response = await Client.SendAsync(request, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnsupportedMediaType);
        Assert.Equal(MoniPayErrorTypes.UnsupportedMediaType.Urn, problem.Type);
    }

    [Theory]
    [InlineData("en", "The refresh token has already been used.")]
    [InlineData("fr", "Le jeton de rafraîchissement a déjà été utilisé.")]
    public async Task Problems_are_localized(string culture, string title)
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        await RefreshTokenOfAsync(issued);

        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        using HttpRequestMessage request = new(HttpMethod.Post, SignUpFlow.RefreshUrl())
        {
            Content = SignUpFlow.RefreshBody(issued.Session.RefreshToken, issued.DeviceId),
        };

        using HttpResponseMessage response = await client.SendAsync(request, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.RefreshTokenReused.Urn, problem.Type);
        Assert.Equal(title, problem.Title);
    }

    private async Task<string> RefreshTokenOfAsync(OpenedSession issued)
    {
        using HttpResponseMessage response = await SignUpFlow.PostRefreshAsync(Client, issued);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement document = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);

        return document
            .GetProperty("data")
            .GetProperty("attributes")
            .GetProperty("refreshToken")
            .GetString() ?? throw new Xunit.Sdk.XunitException("The refresh token was not a JSON string.");
    }

    private async Task<HttpStatusCode> SecureProbeAsync(string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/test/secure");
        request.Headers.Authorization = new(MoniPayHeaders.Bearer, accessToken);
        using HttpResponseMessage response = await Client.SendAsync(request, Cancellation);

        return response.StatusCode;
    }

}
