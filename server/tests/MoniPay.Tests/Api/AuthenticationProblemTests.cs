using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Api;

/// <summary>
/// The challenge body names the credential the scheme refused, server-side: a token presented to
/// the wrong workflow scheme, and a bearer token the JWT handler rejects, each get their own
/// stable type and their own localized title. The submitted authorization scheme never decides it.
/// </summary>
public sealed class AuthenticationProblemTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Theory]
    [InlineData(Locale.EnglishTag, "The registration token is invalid or expired.")]
    [InlineData(Locale.FrenchTag, "Le jeton d'enregistrement est invalide ou expiré.")]
    public async Task A_sign_up_token_on_the_registration_route_names_the_registration_credential(
        string culture,
        string title)
    {
        VerifiedSignUp verified = await Api.StartVerifiedAsync();

        using HttpResponseMessage response = await SendAsync(
            $"SignUp {verified.Started.SignUpToken}",
            $"/test/registrations/{verified.Started.SignUpId.Value}",
            culture);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized, requireNoStore: false);

        Assert.Equal(MoniPayErrorTypes.RegistrationTokenInvalid.Urn, problem.Type);
        Assert.Equal("Registration", response.Headers.WwwAuthenticate.ToString());
        Assert.Equal(title, problem.Title);
    }

    [Theory]
    [InlineData(Locale.EnglishTag, "The sign-up token is invalid or expired.")]
    [InlineData(Locale.FrenchTag, "Le jeton d'inscription est invalide ou expiré.")]
    public async Task A_registration_token_on_the_sign_up_route_names_the_sign_up_credential(
        string culture,
        string title)
    {
        VerifiedSignUp verified = await Api.StartVerifiedAsync();

        using HttpResponseMessage response = await SendAsync(
            $"Registration {verified.Verified.RegistrationToken}",
            $"/test/signups/{verified.Started.SignUpId.Value}",
            culture);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized, requireNoStore: false);

        Assert.Equal(MoniPayErrorTypes.SignUpTokenInvalid.Urn, problem.Type);
        Assert.Equal("SignUp", response.Headers.WwwAuthenticate.ToString());
        Assert.Equal(title, problem.Title);
    }

    [Theory]
    [InlineData(Locale.EnglishTag, "The session token is invalid or expired.")]
    [InlineData(Locale.FrenchTag, "Le jeton de session est invalide ou expiré.")]
    public async Task A_rejected_bearer_names_the_session_credential(string culture, string title)
    {
        using HttpResponseMessage response = await SendAsync(
            $"Bearer {TestTokens.Unsigned()}",
            "/test/secure",
            culture);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized, requireNoStore: false);

        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
        Assert.Equal(title, problem.Title);
    }

    private async Task<HttpResponseMessage> SendAsync(string authorization, string route, string culture)
    {
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);

        using HttpRequestMessage request = new(HttpMethod.Get, route)
        {
            Headers = { Authorization = AuthenticationHeaderValue.Parse(authorization) },
        };

        return await client.SendAsync(request, Cancellation);
    }
}
