using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Security;
using Xunit;

namespace MoniPay.Tests.Support;

/// <summary>The profile a completion submits. Every sample owns an email no other test uses.</summary>
internal sealed record Profile(string FirstName, string LastName, string Email, Guid DeviceId)
{
    private static int sequence;

    public static Profile Sample()
    {
        int unique = Interlocked.Increment(ref sequence);
        return new Profile("Marie", "Ngo", $"complete.marie{unique}@example.com", Guid.CreateVersion7());
    }
}

/// <summary>A sign-up whose phone is proven by the routes themselves.</summary>
internal sealed record StartedAndVerified(SignUpId SignUpId, string RegistrationToken, PhoneNumber Phone);

/// <summary>
/// Drives the completion route the way a client does: start and verify over HTTP, then complete.
/// The registration credential only ever exists in a response, so it is read from one.
/// </summary>
internal static class CompletionFlow
{
    /// <summary>
    /// Starts a sign-up and verifies its phone through the routes, and returns the registration
    /// credential the verification response issued — the only place it ever appears.
    /// </summary>
    public static async Task<StartedAndVerified> StartAndVerifyAsync(this MoniPayApi api, HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentNullException.ThrowIfNull(client);

        PhoneNumber phone = new(TestPhones.Next());
        using HttpResponseMessage start = await SignUpFlow.PostStartAsync(client, SignUpFlow.StartBody(phone.Value));
        JsonElement started = await start.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        SignUpId signUpId = new(Guid.Parse(IdOf(started)));
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

    /// <summary>The completion route for one sign-up, built from its constant.</summary>
    public static string CompleteUrl(SignUpId signUpId) =>
        FormattableString.Invariant($"{SignUpResources.Self(signUpId)}/completions");

    /// <summary>A completion document for the given profile and sign-up.</summary>
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

    /// <summary>Posts a completion with the registration credential that authorizes it.</summary>
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

    /// <summary>How many users the completions provisioned, read straight from PostgreSQL.</summary>
    public static async Task<int> CountUsersAsync(this MoniPayApi api) =>
        Convert.ToInt32(Assert.Single(
            await api.QueryAsync("SELECT count(*) FROM users;", reader => reader.GetValue(0))));

    /// <summary>How many sessions are still live, read straight from PostgreSQL.</summary>
    public static async Task<int> CountActiveSessionsAsync(this MoniPayApi api) =>
        Convert.ToInt32(Assert.Single(
            await api.QueryAsync("SELECT count(*) FROM sessions WHERE revoked_at IS NULL;", reader => reader.GetValue(0))));

    /// <summary>A documentation-range address no other test uses, so each completion owns its IP budget.</summary>
    private static string IsolatedIp() =>
        FormattableString.Invariant($"198.51.100.{Interlocked.Increment(ref ipCounter) % 250 + 1}");

    private static int ipCounter;

    private static string IdOf(JsonElement document) =>
        document.GetProperty("data").GetProperty("id").GetString()
        ?? throw new InvalidOperationException("The resource identifier was not a JSON string.");
}
