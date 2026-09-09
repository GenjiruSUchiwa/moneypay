using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Security;
using Xunit;

namespace MoniPay.Tests.Support;

internal sealed record Profile(string FirstName, string LastName, string Email, Guid DeviceId)
{
    public static Profile Sample()
    {
        CreateSignUpCompletionCommand command = SignUpFlow.CompletionCommand();
        return new Profile(command.FirstName.Value, command.LastName.Value, command.Email.Value, command.DeviceId);
    }
}

internal sealed record StartedAndVerified(SignUpId SignUpId, string RegistrationToken, PhoneNumber Phone);

internal static class CompletionFlow
{
    public static async Task<StartedAndVerified> StartAndVerifyAsync(this MoniPayApi api, HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentNullException.ThrowIfNull(client);

        PhoneNumber phone = new(TestPhones.Next());
        using HttpResponseMessage start = await SignUpFlow.PostStartAsync(client, SignUpFlow.StartBody(phone.Value));
        JsonElement started = await start.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        SignUpId signUpId = new(Guid.Parse(JsonApiAssertions.IdOf(started)));
        string signUpToken = started
            .GetProperty("data")
            .GetProperty("attributes")
            .GetProperty("signUpToken")
            .GetString() ?? throw new InvalidOperationException("The start response carried no sign-up token.");

        using HttpResponseMessage verified = await SignUpFlow.PostVerifyAsync(
            client,
            signUpId,
            signUpToken,
            await api.DeliveredCodeAsync(phone));
        JsonElement document = await verified.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        return new StartedAndVerified(
            signUpId,
            document.GetProperty("data").GetProperty("attributes").GetProperty("registrationToken").GetString()
                ?? throw new InvalidOperationException("The verification response carried no registration token."),
            phone);
    }

    public static string CompleteUrl(SignUpId signUpId) =>
        FormattableString.Invariant($"{SignUpResources.Self(signUpId)}/completions");

    public static StringContent CompleteBody(SignUpId signUpId, Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        string json = JsonSerializer.Serialize(new
        {
            data = new
            {
                type = SignUpResourceTypes.SignUpCompletions,
                attributes = new
                {
                    firstName = profile.FirstName,
                    lastName = profile.LastName,
                    email = profile.Email,
                    deviceId = profile.DeviceId,
                },
                relationships = new
                {
                    signUp = new
                    {
                        data = new
                        {
                            type = SignUpResourceTypes.SignUps,
                            id = signUpId.Value.ToString(),
                        },
                    },
                },
            },
        });

        StringContent body = new(json, Encoding.UTF8);
        body.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);

        return body;
    }

    public static Task<HttpResponseMessage> PostCompleteAsync(
        HttpClient client,
        SignUpId signUpId,
        string registrationToken,
        Profile? profile = null,
        StringContent? body = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(registrationToken);

        HttpRequestMessage request = new(HttpMethod.Post, CompleteUrl(signUpId))
        {
            Content = body ?? CompleteBody(signUpId, profile ?? Profile.Sample()),
        };
        request.Headers.Authorization = new(SessionsSchemes.Registration, registrationToken);
        request.Headers.Add("X-Forwarded-For", IsolatedIp());

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static async Task<int> CountUsersAsync(this MoniPayApi api) =>
        Convert.ToInt32(Assert.Single(
            await api.QueryAsync("SELECT count(*) FROM users;", reader => reader.GetValue(0))));

    public static async Task<int> CountActiveSessionsAsync(this MoniPayApi api) =>
        Convert.ToInt32(Assert.Single(
            await api.QueryAsync("SELECT count(*) FROM sessions WHERE revoked_at IS NULL;", reader => reader.GetValue(0))));

    private static string IsolatedIp() =>
        FormattableString.Invariant($"198.51.100.{Interlocked.Increment(ref ipCounter) % 250 + 1}");

    private static int ipCounter;
}
