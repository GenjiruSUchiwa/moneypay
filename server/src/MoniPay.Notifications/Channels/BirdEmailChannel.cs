using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;

namespace MoniPay.Notifications.Channels;

internal sealed class BirdEmailChannel : INotificationChannel
{
    internal const string SubjectMissing = "subject-missing";
    internal const string SubjectTooLarge = "subject-too-large";
    internal const string BodyTooLarge = "body-too-large";
    internal const string InvalidRecipient = "invalid-recipient";
    internal const string KeyInFlight = "provider-key-in-flight";
    internal const string KeyConflict = "provider-key-conflict";
    internal const string RateLimited = "provider-rate-limited";
    internal const string ServerError = "provider-5xx";
    internal const string Transport = "provider-transport";
    internal const string ClientTimeout = "provider-timeout";
    internal const string Protocol = "provider-protocol";
    internal const string Unauthorized = "provider-unauthorized";
    internal const string ProviderRejected = "provider-rejected";
    internal const string UnexpectedStatus = "provider-unexpected-status";

    internal const int SubjectMaxLength = 998;
    internal const int BodyMaxLength = 524288;
    internal const int ReferenceMaxLength = 128;

    private const string IdempotencyHeader = "Idempotency-Key";
    private const string KeyConflictCode = "E01005";
    private const string KeyInFlightCode = "E01004";
    private const string Transactional = "transactional";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient client;
    private readonly string apiKey;
    private readonly string sender;

    public BirdEmailChannel(HttpClient client, IOptions<NotificationsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        if (!EmailAddress.TryNormalize(options.Value.Email.FromAddress, out EmailAddress sender))
        {
            throw new InvalidOperationException($"{NotificationsOptions.Keys.EmailFromAddress} is invalid.");
        }

        this.client = client;
        apiKey = options.Value.Email.ApiKey;
        this.sender = sender.Value;
    }

    public async Task<ChannelResult> SendAsync(
        string recipient,
        string? subject,
        string body,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(subject))
        {
            return new ChannelResult.Rejected(SubjectMissing);
        }

        if (subject.Length > SubjectMaxLength)
        {
            return new ChannelResult.Rejected(SubjectTooLarge);
        }

        if (body.Length > BodyMaxLength)
        {
            return new ChannelResult.Rejected(BodyTooLarge);
        }

        if (!EmailAddress.TryNormalize(recipient, out EmailAddress to))
        {
            return new ChannelResult.Rejected(InvalidRecipient);
        }

        using HttpRequestMessage request = new(HttpMethod.Post, "v1/email/messages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Add(IdempotencyHeader, idempotencyKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new BirdRequest(sender, [to.Value], subject, body, Transactional), SerializerOptions),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response;
        try
        {
            response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return new ChannelResult.Retry(Transport);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ChannelResult.Retry(ClientTimeout);
        }

        using (response)
        {
            return await MapAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<ChannelResult> MapAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        switch ((int)response.StatusCode)
        {
            case 202:
                return await AcceptedAsync(response, cancellationToken).ConfigureAwait(false);
            case 409:
                return await ConflictAsync(response, cancellationToken).ConfigureAwait(false);
            case 429:
                return new ChannelResult.Retry(RateLimited);
            case >= 500:
                return new ChannelResult.Retry(ServerError);
            case 401:
            case 403:
            case 421:
                return new ChannelResult.Rejected(Unauthorized);
            case >= 300 and < 400:
                return new ChannelResult.Rejected(UnexpectedStatus);
            default:
                return new ChannelResult.Rejected(ProviderRejected);
        }
    }

    private static async Task<ChannelResult> AcceptedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("id", out JsonElement id)
                && id.ValueKind == JsonValueKind.String
                && id.GetString() is { } reference
                && !string.IsNullOrWhiteSpace(reference)
                && reference.Length <= ReferenceMaxLength)
            {
                return new ChannelResult.Accepted(reference);
            }
        }
        catch (JsonException)
        {
            return new ChannelResult.Retry(Protocol);
        }

        return new ChannelResult.Retry(Protocol);
    }

    private static async Task<ChannelResult> ConflictAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ErrorCode(payload) == KeyConflictCode
            ? new ChannelResult.Rejected(KeyConflict)
            : new ChannelResult.Retry(KeyInFlight);
    }

    private static string? ErrorCode(string payload)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out JsonElement error)
                && error.ValueKind == JsonValueKind.Object
                && error.TryGetProperty("code", out JsonElement code)
                && code.ValueKind == JsonValueKind.String)
            {
                return code.GetString();
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record BirdRequest(string From, string[] To, string Subject, string Text, string Category);
}
