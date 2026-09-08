using System.Net;
using MoniPay.Kernel;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Features.SignUps.VerifyPhone;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

/// <summary>
/// The three schemes never accept each other's credentials: the scheme named in the
/// <c>Authorization</c> header and the purpose baked into the digest both have to match, and a
/// workflow credential is bound to the one sign-up its route names.
/// </summary>
public sealed class SchemeIsolationTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task Each_credential_opens_its_own_scheme_and_no_other()
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        using HttpClient client = Api.CreateClient();

        Assert.Equal(HttpStatusCode.OK, await SendAsync(client, "SignUp", started.SignUpToken, $"/test/signups/{started.SignUpId.Value}"));
        Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(client, "SignUp", started.SignUpToken, $"/test/registrations/{started.SignUpId.Value}"));
        Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(client, "SignUp", started.SignUpToken, "/test/secure"));
    }

    [Fact]
    public async Task The_sign_up_token_is_void_once_the_phone_is_verified()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        using HttpClient client = Api.CreateClient();

        Assert.Equal(HttpStatusCode.OK, await SendAsync(client, "SignUp", started.SignUpToken, $"/test/signups/{started.SignUpId.Value}"));

        await Api.VerifyPhoneAsync(started.SignUpId, Api.Sender.CodeFor(phone));

        Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(client, "SignUp", started.SignUpToken, $"/test/signups/{started.SignUpId.Value}"));
    }

    [Fact]
    public async Task The_registration_credential_is_bound_to_its_own_sign_up()
    {
        (SignUpId firstId, string firstToken) = await VerifiedSignUpAsync();
        (SignUpId secondId, string _) = await VerifiedSignUpAsync();

        using HttpClient client = Api.CreateClient();
        Assert.Equal(HttpStatusCode.OK, await SendAsync(client, "Registration", firstToken, $"/test/registrations/{firstId.Value}"));
        Assert.Equal(HttpStatusCode.OK, await SendAsync(client, "Registration", firstToken, $"/test/registrations/{firstId.Value.ToString().ToUpperInvariant()}"));
        Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(client, "Registration", firstToken, $"/test/registrations/{secondId.Value}"));
        Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(client, "Registration", firstToken, $"/test/signups/{firstId.Value}"));
        Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(client, "Registration", firstToken, "/test/secure"));
    }

    [Fact]
    public async Task A_bearer_token_does_not_open_the_workflow_routes()
    {
        SessionTokenResult issued = await CreateSessionAsync();
        using HttpClient client = Api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(client, "Bearer", TestTokens.Bearer(issued.UserId, issued.SessionId), $"/test/signups/{Guid.CreateVersion7()}"));
        Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(client, "Bearer", TestTokens.Bearer(issued.UserId, issued.SessionId), $"/test/registrations/{Guid.CreateVersion7()}"));
        Assert.Equal(HttpStatusCode.OK, await SendAsync(client, "Bearer", TestTokens.Bearer(issued.UserId, issued.SessionId), "/test/secure"));

        // A refresh token is not a bearer credential either, whatever its shape.
        Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(client, "Bearer", issued.RefreshToken, "/test/secure"));
    }

    [Theory]
    [InlineData("/test/signups/00000000-0000-0000-0000-000000000001", SessionsSchemes.SignUp)]
    [InlineData("/test/registrations/00000000-0000-0000-0000-000000000001", SessionsSchemes.Registration)]
    public async Task Missing_workflow_credentials_challenge_with_the_route_scheme(string route, string scheme)
    {
        using HttpClient client = Api.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(route, Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(scheme, response.Headers.WwwAuthenticate.ToString());
    }

    private async Task<(SignUpId SignUpId, string RegistrationToken)> VerifiedSignUpAsync()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        CreatePhoneVerificationResult verified = await Api.VerifyPhoneAsync(started.SignUpId, Api.Sender.CodeFor(phone));
        return (started.SignUpId, verified.RegistrationToken);
    }

    private static async Task<HttpStatusCode> SendAsync(HttpClient client, string scheme, string credential, string route)
    {
        HttpRequestMessage request = new(HttpMethod.Get, route);
        request.Headers.Authorization = new(scheme, credential);
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    private Task<SessionTokenResult> CreateSessionAsync() =>
        Api.InScopeAsync<SessionTokenService, SessionTokenResult>((sessions, cancellationToken) =>
            sessions.CreateAsync(UserId.New(), Guid.CreateVersion7(), cancellationToken));
}
