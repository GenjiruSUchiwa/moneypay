using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using MoniPay.Kernel.Errors;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Api;

/// <summary>
/// The challenge body names the credential the scheme refused, server-side: a token presented to
/// the wrong workflow scheme, and a bearer token the JWT handler rejects, each get their own
/// stable type. The submitted authorization scheme never decides it.
/// </summary>
public sealed class AuthenticationProblemTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task A_sign_up_token_on_the_registration_route_names_the_registration_credential()
    {
        VerifiedSignUp verified = await Api.StartVerifiedAsync();

        using HttpResponseMessage response = await SendAsync(
            $"SignUp {verified.Started.SignUpToken}",
            $"/test/registrations/{verified.Started.SignUpId.Value}");

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized, requireNoStore: false);

        Assert.Equal(MoniPayErrorTypes.RegistrationTokenInvalid.Urn, problem.Type);
        Assert.Equal("Registration", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task A_registration_token_on_the_sign_up_route_names_the_sign_up_credential()
    {
        VerifiedSignUp verified = await Api.StartVerifiedAsync();

        using HttpResponseMessage response = await SendAsync(
            $"Registration {verified.Verified.RegistrationToken}",
            $"/test/signups/{verified.Started.SignUpId.Value}");

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized, requireNoStore: false);

        Assert.Equal(MoniPayErrorTypes.SignUpTokenInvalid.Urn, problem.Type);
        Assert.Equal("SignUp", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task A_rejected_bearer_names_the_session_credential()
    {
        using HttpResponseMessage response = await SendAsync(
            $"Bearer {TestTokens.Unsigned()}",
            "/test/secure");

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized, requireNoStore: false);

        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    private async Task<HttpResponseMessage> SendAsync(string authorization, string route)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, route)
        {
            Headers = { Authorization = AuthenticationHeaderValue.Parse(authorization) },
        };

        return await Client.SendAsync(request, Cancellation);
    }
}
