using System.Buffers.Text;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using MoniPay.Api.Hosting;
using MoniPay.Kernel;
using MoniPay.Sessions;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

public sealed class AccessTokenValidationTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task An_expired_token_is_refused()
    {
        using HttpClient client = Api.CreateClient();
        SessionTokenResult issued = await CreateValidatedSessionAsync(client);
        string token = TestTokens.Bearer(issued.UserId, issued.SessionId, expires: DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5));

        using HttpResponseMessage response = await SecureAsync(client, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_from_another_issuer_is_refused()
    {
        using HttpClient client = Api.CreateClient();
        SessionTokenResult issued = await CreateValidatedSessionAsync(client);
        string token = TestTokens.Bearer(issued.UserId, issued.SessionId, issuer: "https://evil.example");

        using HttpResponseMessage response = await SecureAsync(client, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_for_another_audience_is_refused()
    {
        using HttpClient client = Api.CreateClient();
        SessionTokenResult issued = await CreateValidatedSessionAsync(client);
        string token = TestTokens.Bearer(issued.UserId, issued.SessionId, audience: "other-client");

        using HttpResponseMessage response = await SecureAsync(client, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_signed_by_an_unknown_key_is_refused()
    {
        using HttpClient client = Api.CreateClient();
        SessionTokenResult issued = await CreateValidatedSessionAsync(client);
        string token = TestTokens.Bearer(issued.UserId, issued.SessionId, key: Convert.FromBase64String(TestKeys.OtherSigning));

        using HttpResponseMessage response = await SecureAsync(client, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_unsigned_none_algorithm_token_is_refused()
    {
        using HttpClient client = Api.CreateClient();

        using HttpRequestMessage request = new(HttpMethod.Get, "/test/secure");
        SessionTokenResult issued = await CreateValidatedSessionAsync(client);
        string payload = TestTokens.Bearer(issued.UserId, issued.SessionId).Split('.')[1];
        string header = Base64Url.EncodeToString("""{"alg":"none","typ":"JWT"}"""u8);
        request.Headers.Authorization = new("Bearer", $"{header}.{payload}.");
        using HttpResponseMessage response = await client.SendAsync(request, Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_signed_by_the_previous_key_is_accepted_while_the_rotation_names_it()
    {
        SessionTokenResult issued = await CreateSessionAsync();
        using WebApplicationFactory<Program> host = Api.CreateHost(builder =>
            builder.UseSetting(SessionsOptions.Keys.PreviousSigningKeyBase64, TestKeys.OtherSigning));
        using HttpClient client = host.CreateClient();

        string token = TestTokens.Bearer(issued.UserId, issued.SessionId, key: Convert.FromBase64String(TestKeys.OtherSigning));

        using HttpResponseMessage response = await SecureAsync(client, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_token_signed_by_the_previous_key_is_refused_once_the_rotation_is_cleared()
    {
        using HttpClient client = Api.CreateClient();
        SessionTokenResult issued = await CreateValidatedSessionAsync(client);
        string token = TestTokens.Bearer(issued.UserId, issued.SessionId, key: Convert.FromBase64String(TestKeys.OtherSigning));

        using HttpResponseMessage response = await SecureAsync(client, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static Task<HttpResponseMessage> SecureAsync(HttpClient client, string token)
    {
        HttpRequestMessage request = new(HttpMethod.Get, "/test/secure");
        request.Headers.Authorization = new("Bearer", token);
        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<SessionTokenResult> CreateValidatedSessionAsync(HttpClient client)
    {
        SessionTokenResult issued = await CreateSessionAsync();
        using HttpResponseMessage control = await SecureAsync(client, TestTokens.Bearer(issued.UserId, issued.SessionId));
        Assert.Equal(HttpStatusCode.OK, control.StatusCode);
        return issued;
    }

    private Task<SessionTokenResult> CreateSessionAsync() =>
        Api.InScopeAsync<SessionTokenService, SessionTokenResult>((sessions, cancellationToken) =>
            sessions.CreateAsync(UserId.New(), Guid.CreateVersion7(), cancellationToken));
}
