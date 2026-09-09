using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Get;

public sealed class GetSignUpHttpTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task Reading_returns_200_with_state_and_timing_but_no_token()
    {
        PhoneNumber phone = new(TestPhones.Next());
        MoniPay.Sessions.Features.SignUps.Start.StartSignUpResult started = await Api.StartSignUpAsync(phone);

        using HttpResponseMessage response = await SignUpFlow.GetSignUpHttpAsync(
            Client,
            started.SignUpId,
            SessionsSchemes.SignUp,
            started.SignUpToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MoniPayMediaTypes.JsonApi, response.Content.Headers.ContentType?.ToString());
        AssertNoStore(response);

        JsonElement document = await JsonApiAssertions.ReadJsonApiAsync(response);
        JsonElement data = document.GetProperty("data");
        Assert.Equal(SignUpResourceTypes.SignUps, data.GetProperty("type").GetString());
        Assert.Equal(started.SignUpId.Value.ToString(), data.GetProperty("id").GetString());

        JsonElement attributes = data.GetProperty("attributes");
        Assert.Equal(JsonApiAssertions.Wire(SignUpStatusValue.CodePending), attributes.GetProperty("status").GetString());
        Assert.Equal(JsonApiAssertions.Wire(CodeDeliveryValue.Queued), attributes.GetProperty("codeDelivery").GetString());
        Assert.False(attributes.TryGetProperty("signUpToken", out _));
        Assert.False(attributes.TryGetProperty("registrationToken", out _));
        Assert.False(attributes.TryGetProperty("phone", out _));
        Assert.False(attributes.TryGetProperty("id", out _));

        string self = SignUpResources.Self(started.SignUpId);
        Assert.Equal(self, data.GetProperty("links").GetProperty("self").GetString());

        await Api.RunNotificationCycleAsync(Cancellation);

        using HttpResponseMessage sent = await SignUpFlow.GetSignUpHttpAsync(
            Client,
            started.SignUpId,
            SessionsSchemes.SignUp,
            started.SignUpToken);
        JsonElement resent = await JsonApiAssertions.ReadJsonApiAsync(sent);
        Assert.Equal(
            JsonApiAssertions.Wire(CodeDeliveryValue.Sent),
            resent.GetProperty("data").GetProperty("attributes").GetProperty("codeDelivery").GetString());
    }

    [Fact]
    public async Task Reading_accepts_the_registration_credential_after_verification()
    {
        VerifiedSignUp verified = await Api.StartVerifiedAsync();

        using HttpResponseMessage response = await SignUpFlow.GetSignUpHttpAsync(
            Client,
            verified.Started.SignUpId,
            SessionsSchemes.Registration,
            verified.Verified.RegistrationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement document = await JsonApiAssertions.ReadJsonApiAsync(response);
        JsonElement attributes = document.GetProperty("data").GetProperty("attributes");
        Assert.Equal(JsonApiAssertions.Wire(SignUpStatusValue.PhoneVerified), attributes.GetProperty("status").GetString());
        Assert.False(attributes.TryGetProperty("signUpToken", out _));
        Assert.False(attributes.TryGetProperty("registrationToken", out _));
    }

    [Fact]
    public async Task A_token_for_another_sign_up_is_401()
    {
        MoniPay.Sessions.Features.SignUps.Start.StartSignUpResult first =
            await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        MoniPay.Sessions.Features.SignUps.Start.StartSignUpResult second =
            await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));

        using HttpResponseMessage response = await SignUpFlow.GetSignUpHttpAsync(
            Client,
            second.SignUpId,
            SessionsSchemes.SignUp,
            first.SignUpToken);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SignUpTokenInvalid.Urn, problem.Type);
        Assert.Equal(SessionsSchemes.SignUp, response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task A_missing_credential_is_401_without_revealing_existence()
    {
        using HttpResponseMessage response = await Client.GetAsync(
            SignUpFlow.ReadUrl(SignUpId.New()),
            Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SignUpTokenInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task A_bearer_credential_does_not_open_the_route()
    {
        MoniPay.Sessions.Features.SignUps.Start.StartSignUpResult started =
            await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));

        HttpRequestMessage request = new(HttpMethod.Get, SignUpFlow.ReadUrl(started.SignUpId));
        request.Headers.Authorization = new("Bearer", TestTokens.Bearer(UserId.New(), Guid.CreateVersion7()));
        using HttpResponseMessage response = await Client.SendAsync(request, Cancellation);

        await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reading_an_expired_sign_up_is_401()
    {
        PhoneNumber phone = new(TestPhones.Next());
        MoniPay.Sessions.Features.SignUps.Start.StartSignUpResult started = await Api.StartSignUpAsync(phone);
        Api.Time.Advance(TimeSpan.FromMinutes(16));

        using HttpResponseMessage response = await SignUpFlow.GetSignUpHttpAsync(
            Client,
            started.SignUpId,
            SessionsSchemes.SignUp,
            started.SignUpToken);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SignUpTokenInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task Accepting_only_problem_json_is_406()
    {
        MoniPay.Sessions.Features.SignUps.Start.StartSignUpResult started =
            await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));

        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.ProblemJson);
        HttpRequestMessage request = new(HttpMethod.Get, SignUpFlow.ReadUrl(started.SignUpId));
        request.Headers.Authorization = new(SessionsSchemes.SignUp, started.SignUpToken);

        using HttpResponseMessage response = await client.SendAsync(request, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.NotAcceptable);
        Assert.Equal(MoniPayErrorTypes.NotAcceptable.Urn, problem.Type);
    }

    [Theory]
    [InlineData("en", "The sign-up token is invalid or expired.")]
    [InlineData("fr", "Le jeton d'inscription est invalide ou expiré.")]
    public async Task Authentication_problems_are_localized(string culture, string title)
    {
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);

        using HttpResponseMessage response = await client.GetAsync(SignUpFlow.ReadUrl(SignUpId.New()), Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SignUpTokenInvalid.Urn, problem.Type);
        Assert.Equal(title, problem.Title);
    }

    private static void AssertNoStore(HttpResponseMessage response)
    {
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.True(response.Headers.TryGetValues("Pragma", out IEnumerable<string>? pragma));
        Assert.Equal("no-cache", Assert.Single(pragma!));
    }

    private static async Task<JsonElement> ReadSuccessAsync(HttpResponseMessage response)
    {
        JsonElement document = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(JsonApiVersion.Current, document.GetProperty("jsonapi").GetProperty("version").GetString());

        return document;
    }
}
