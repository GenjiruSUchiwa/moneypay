using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Complete;

/// <summary>
/// The completion route: the registration credential authorizes it, the profile names the user,
/// and the response is the first session with its credentials. A retry replaces the session
/// instead of provisioning a second user.
/// </summary>
public sealed class CreateSignUpCompletionHttpTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task The_first_completion_returns_201_the_session_resource_and_its_location()
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);
        Profile profile = Profile.Sample();
        int usersBefore = await Api.CountUsersAsync();
        int sessionsBefore = await Api.CountActiveSessionsAsync();

        using HttpResponseMessage response = await CompletionFlow.PostCompleteAsync(
            Client,
            verified.SignUpId,
            verified.RegistrationToken,
            profile);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(SessionResources.Self, response.Headers.Location?.ToString());

        JsonElement document = await ReadSessionDocumentAsync(response);
        JsonElement attributes = document.GetProperty("data").GetProperty("attributes");
        Assert.Equal(SessionResources.BearerTokenType, attributes.GetProperty("tokenType").GetString());
        Assert.False(string.IsNullOrEmpty(attributes.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrEmpty(attributes.GetProperty("refreshToken").GetString()));
        Assert.True(attributes.GetProperty("accessTokenExpiresAt").GetDateTimeOffset() > Api.Time.GetUtcNow());

        JsonElement user = document.GetProperty("data").GetProperty("relationships").GetProperty("user");
        Assert.Equal(SessionResourceTypes.Users, user.GetProperty("data").GetProperty("type").GetString());
        Assert.Equal(SessionResources.CurrentUser, user.GetProperty("links").GetProperty("related").GetString());
        Assert.Equal(SessionResources.Self, document.GetProperty("data").GetProperty("links").GetProperty("self").GetString());

        Assert.Equal(usersBefore + 1, await Api.CountUsersAsync());
        Assert.Equal(sessionsBefore + 1, await Api.CountActiveSessionsAsync());

        // The credentials travel in the body once; nothing the host logged may carry them.
        string accessToken = document.GetProperty("data").GetProperty("attributes").GetProperty("accessToken").GetString()
            ?? throw new Xunit.Sdk.XunitException("The access token was not a JSON string.");
        Assert.DoesNotContain(
            Api.Logs.Entries,
            entry => entry.Message.Contains(accessToken, StringComparison.Ordinal)
                || entry.Message.Contains(RefreshTokenOf(document), StringComparison.Ordinal)
                || entry.Message.Contains(verified.RegistrationToken, StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_retry_returns_200_and_a_replacement_session()
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);
        Profile profile = Profile.Sample();
        int usersBefore = await Api.CountUsersAsync();
        int sessionsBefore = await Api.CountActiveSessionsAsync();

        using HttpResponseMessage created = await CompletionFlow.PostCompleteAsync(
            Client,
            verified.SignUpId,
            verified.RegistrationToken,
            profile);
        JsonElement first = await ReadSessionDocumentAsync(created);

        using HttpResponseMessage retry = await CompletionFlow.PostCompleteAsync(
            Client,
            verified.SignUpId,
            verified.RegistrationToken,
            profile);
        JsonElement second = await ReadSessionDocumentAsync(retry);

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Null(retry.Headers.Location);
        Assert.NotEqual(RefreshTokenOf(first), RefreshTokenOf(second));
        Assert.Equal(usersBefore + 1, await Api.CountUsersAsync());
        Assert.Equal(sessionsBefore + 1, await Api.CountActiveSessionsAsync());

        await AssertCredentialsAsync(first, profile.DeviceId, HttpStatusCode.Unauthorized);
        await AssertCredentialsAsync(second, profile.DeviceId, HttpStatusCode.OK);
    }

    [Fact]
    public async Task Eight_parallel_completions_provision_one_user_and_one_live_session()
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);
        Profile profile = Profile.Sample();
        int usersBefore = await Api.CountUsersAsync();
        int sessionsBefore = await Api.CountActiveSessionsAsync();

        HttpResponseMessage[] responses = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => CompletionFlow.PostCompleteAsync(
                Client,
                verified.SignUpId,
                verified.RegistrationToken,
                profile)));

        try
        {
            Assert.All(
                responses,
                response => Assert.Contains(
                    response.StatusCode,
                    new[] { HttpStatusCode.Created, HttpStatusCode.OK }));
            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
            Assert.Equal(usersBefore + 1, await Api.CountUsersAsync());
            Assert.Equal(sessionsBefore + 1, await Api.CountActiveSessionsAsync());

            JsonElement[] documents = await Task.WhenAll(responses.Select(ReadSessionDocumentAsync));
            string sessionId = (await Api.ReadSignUpRowAsync(verified.SignUpId)).BootstrapSessionId?.ToString()
                ?? throw new Xunit.Sdk.XunitException("The completed sign-up has no bootstrap session.");
            Assert.Single(documents, document => JsonApiAssertions.IdOf(document) == sessionId);
            foreach (JsonElement document in documents)
            {
                HttpStatusCode expected = JsonApiAssertions.IdOf(document) == sessionId
                    ? HttpStatusCode.OK
                    : HttpStatusCode.Unauthorized;
                await AssertCredentialsAsync(document, profile.DeviceId, expected);
            }
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task A_duplicate_email_is_409_and_provisions_nothing()
    {
        StartedAndVerified first = await Api.StartAndVerifyAsync(Client);
        Profile profile = Profile.Sample();
        int usersBefore = await Api.CountUsersAsync();
        int sessionsBefore = await Api.CountActiveSessionsAsync();
        using HttpResponseMessage created = await CompletionFlow.PostCompleteAsync(
            Client,
            first.SignUpId,
            first.RegistrationToken,
            profile);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        StartedAndVerified second = await Api.StartAndVerifyAsync(Client);
        using HttpResponseMessage refused = await CompletionFlow.PostCompleteAsync(
            Client,
            second.SignUpId,
            second.RegistrationToken,
            profile);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem = await refused.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(MoniPayErrorTypes.EmailAlreadyRegistered.Urn, problem.Type);
        Assert.Equal(usersBefore + 1, await Api.CountUsersAsync());
        Assert.Equal(sessionsBefore + 1, await Api.CountActiveSessionsAsync());

        // A corrected profile still completes: the credential is not spent by the conflict.
        using HttpResponseMessage corrected = await CompletionFlow.PostCompleteAsync(
            Client,
            second.SignUpId,
            second.RegistrationToken,
            Profile.Sample());
        Assert.Equal(HttpStatusCode.Created, corrected.StatusCode);
    }

    [Fact]
    public async Task A_bearer_or_sign_up_credential_is_401_on_the_completion_route()
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);

        foreach (string scheme in new[] { MoniPayHeaders.Bearer, SessionsSchemes.SignUp })
        {
            using HttpRequestMessage request = new(HttpMethod.Post, CompletionFlow.CompleteUrl(verified.SignUpId))
            {
                Content = CompletionFlow.CompleteBody(verified.SignUpId, Profile.Sample()),
            };
            request.Headers.Authorization = new(scheme, verified.RegistrationToken);
            using HttpResponseMessage response = await Client.SendAsync(request, Cancellation);

            Microsoft.AspNetCore.Mvc.ProblemDetails problem =
                await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
            Assert.Equal(MoniPayErrorTypes.RegistrationTokenInvalid.Urn, problem.Type);
        }
    }

    [Fact]
    public async Task A_registration_credential_for_another_sign_up_is_401()
    {
        StartedAndVerified first = await Api.StartAndVerifyAsync(Client);
        StartedAndVerified second = await Api.StartAndVerifyAsync(Client);

        using HttpResponseMessage response = await CompletionFlow.PostCompleteAsync(
            Client,
            second.SignUpId,
            first.RegistrationToken,
            Profile.Sample());

        await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_relationship_that_names_another_sign_up_is_409()
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);
        int usersBefore = await Api.CountUsersAsync();

        using HttpResponseMessage response = await CompletionFlow.PostCompleteAsync(
            Client,
            verified.SignUpId,
            verified.RegistrationToken,
            Profile.Sample(),
            CompletionFlow.CompleteBody(SignUpId.New(), Profile.Sample()));

        Microsoft.AspNetCore.Mvc.ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(MoniPayErrorTypes.ResourceIdentityMismatch.Urn, problem.Type);
        Assert.Equal(usersBefore, await Api.CountUsersAsync());
    }

    [Theory]
    [InlineData("""{"data":{"type":"signup-completions","attributes":{"firstName":"Marie","lastName":"Ngo","email":"a@example.cm","deviceId":"1207158c-15fc-446d-a28a-702c564332ef"}}}""")]
    [InlineData("""{"data":{"type":"signup-completions","attributes":{"firstName":"Marie","lastName":"Ngo","email":"a@example.cm","deviceId":"1207158c-15fc-446d-a28a-702c564332ef"},"relationships":{"signUp":null}}}""")]
    [InlineData("""{"data":{"type":"signup-completions","attributes":{"firstName":"Marie","lastName":"Ngo","email":"a@example.cm","deviceId":"1207158c-15fc-446d-a28a-702c564332ef"},"relationships":{"signUp":{"data":{"type":"completions","id":"1207158c-15fc-446d-a28a-702c564332ef"}}}}}""")]
    public async Task A_document_the_route_cannot_bind_to_is_400(string json)
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);
        using StringContent body = new(json, Encoding.UTF8);
        body.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);

        using HttpResponseMessage response = await CompletionFlow.PostCompleteAsync(
            Client,
            verified.SignUpId,
            verified.RegistrationToken,
            Profile.Sample(),
            body);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(MoniPayErrorTypes.JsonApiDocumentInvalid.Urn, problem.Type);
    }

    [Theory]
    [InlineData("passcode")]
    [InlineData("biometricsEnabled")]
    [InlineData("kycStatus")]
    [InlineData("cardProvider")]
    public async Task A_forbidden_profile_member_is_400_and_never_ignored(string member)
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);
        Profile profile = Profile.Sample();
        int usersBefore = await Api.CountUsersAsync();
        using StringContent body = new(
            $"{{\"data\":{{\"type\":\"signup-completions\",\"attributes\":{{\"firstName\":\"{profile.FirstName}\",\"lastName\":\"{profile.LastName}\",\"email\":\"{profile.Email}\",\"deviceId\":\"{profile.DeviceId}\",\"{member}\":\"x\"}},\"relationships\":{{\"signUp\":{{\"data\":{{\"type\":\"signups\",\"id\":\"{verified.SignUpId}\"}}}}}}}}}}",
            Encoding.UTF8);
        body.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);

        using HttpResponseMessage response = await CompletionFlow.PostCompleteAsync(
            Client,
            verified.SignUpId,
            verified.RegistrationToken,
            profile,
            body);

        await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(usersBefore, await Api.CountUsersAsync());
    }

    [Theory]
    [InlineData("  ", CreateSignUpCompletionPointers.FirstName)]
    [InlineData("Marie1", CreateSignUpCompletionPointers.FirstName)]
    public async Task An_invalid_name_is_422_at_its_pointer(string firstName, string pointer)
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);
        int usersBefore = await Api.CountUsersAsync();
        Profile profile = Profile.Sample() with { FirstName = firstName };

        using HttpResponseMessage response = await CompletionFlow.PostCompleteAsync(
            Client,
            verified.SignUpId,
            verified.RegistrationToken,
            profile);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(MoniPayErrorTypes.Validation.Urn, problem.Type);
        Assert.Equal(pointer, Assert.Single(problem.Pointers()));
        Assert.Equal(usersBefore, await Api.CountUsersAsync());
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    [InlineData("a b@example.cm")]
    public async Task An_invalid_email_is_422_at_its_pointer(string email)
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);
        int usersBefore = await Api.CountUsersAsync();

        using HttpResponseMessage response = await CompletionFlow.PostCompleteAsync(
            Client,
            verified.SignUpId,
            verified.RegistrationToken,
            Profile.Sample() with { Email = email });

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(
            CreateSignUpCompletionPointers.Email,
            Assert.Single(problem.Pointers()));
        Assert.Equal(usersBefore, await Api.CountUsersAsync());
    }

    [Fact]
    public async Task An_empty_device_identifier_is_422_at_its_pointer()
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);

        using HttpResponseMessage response = await CompletionFlow.PostCompleteAsync(
            Client,
            verified.SignUpId,
            verified.RegistrationToken,
            Profile.Sample() with { DeviceId = Guid.Empty });

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(
            CreateSignUpCompletionPointers.DeviceId,
            Assert.Single(problem.Pointers()));
    }

    [Fact]
    public async Task Accepting_only_problem_json_is_406()
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.ProblemJson);

        using HttpResponseMessage response = await CompletionFlow.PostCompleteAsync(
            client,
            verified.SignUpId,
            verified.RegistrationToken,
            Profile.Sample());

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.NotAcceptable);
        Assert.Equal(MoniPayErrorTypes.NotAcceptable.Urn, problem.Type);
    }

    [Fact]
    public async Task A_body_that_is_not_jsonapi_is_415()
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);
        using StringContent body = new("""{"data":{"type":"signup-completions"}}""", Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await CompletionFlow.PostCompleteAsync(
            Client,
            verified.SignUpId,
            verified.RegistrationToken,
            Profile.Sample(),
            body);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnsupportedMediaType);
        Assert.Equal(MoniPayErrorTypes.UnsupportedMediaType.Urn, problem.Type);
    }

    [Theory]
    [InlineData("en", "The registration token is invalid or expired.")]
    [InlineData("fr", "Le jeton d'enregistrement est invalide ou expiré.")]
    public async Task Problems_are_localized(string culture, string title)
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);

        using HttpResponseMessage response = await CompletionFlow.PostCompleteAsync(
            client,
            verified.SignUpId,
            "not-a-registration-token",
            Profile.Sample());

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.RegistrationTokenInvalid.Urn, problem.Type);
        Assert.Equal(title, problem.Title);
    }

    private async Task<JsonElement> ReadSessionDocumentAsync(HttpResponseMessage response)
    {
        JsonElement document = await JsonApiAssertions.ReadJsonApiAsync(response);
        Assert.Equal(SessionResourceTypes.Sessions, document.GetProperty("data").GetProperty("type").GetString());
        Assert.True(Guid.TryParse(JsonApiAssertions.IdOf(document), out _));

        return document;
    }

    private async Task AssertCredentialsAsync(JsonElement document, Guid deviceId, HttpStatusCode expected)
    {
        string accessToken = document.GetProperty("data").GetProperty("attributes").GetProperty("accessToken").GetString()
            ?? throw new Xunit.Sdk.XunitException("The access token was not a JSON string.");
        using HttpResponseMessage read = await SignUpFlow.GetCurrentSessionAsync(Client, accessToken);
        Assert.Equal(expected, read.StatusCode);

        using HttpResponseMessage refresh = await SignUpFlow.PostRefreshAsync(Client, RefreshTokenOf(document), deviceId);
        Assert.Equal(expected, refresh.StatusCode);
    }

    private static string RefreshTokenOf(JsonElement document) =>
        document.GetProperty("data").GetProperty("attributes").GetProperty("refreshToken").GetString()
        ?? throw new Xunit.Sdk.XunitException("The refresh token was not a JSON string.");

}
