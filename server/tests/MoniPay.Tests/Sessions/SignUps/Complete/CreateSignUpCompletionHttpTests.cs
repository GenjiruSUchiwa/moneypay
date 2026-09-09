using System.Net;
using System.Text.Json;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Complete;

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

    private async Task<JsonElement> ReadSessionDocumentAsync(HttpResponseMessage response)
    {
        JsonElement document = await JsonApiAssertions.ReadJsonApiAsync(response);
        Assert.Equal(SessionResourceTypes.Sessions, document.GetProperty("data").GetProperty("type").GetString());
        Assert.True(Guid.TryParse(JsonApiAssertions.IdOf(document), out _));

        return document;
    }

    private static string RefreshTokenOf(JsonElement document) =>
        document.GetProperty("data").GetProperty("attributes").GetProperty("refreshToken").GetString()
        ?? throw new Xunit.Sdk.XunitException("The refresh token was not a JSON string.");
}
