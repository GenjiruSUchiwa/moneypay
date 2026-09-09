using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MoniPay.Notifications.Channels;

internal sealed class BirdSmsChannel(
    HttpClient http,
    IOptions<NotificationsOptions> options,
    TimeProvider timeProvider,
    ILogger<BirdSmsChannel> logger) : INotificationChannel
{
    internal const string IdempotencyKeyHeader = "Idempotency-Key";

    private const string Category = "authentication";
    private const string ReferencePrefix = "sms_";
    private const int ReferenceMaxLength = 128;

    private const string InvalidRecipient = "SMSInvalidRecipient";
    private const string SenderNotConfigured = "SMSSenderNotConfigured";
    private const string NoEligibleSender = "SMSNoEligibleSender";
    private const string SenderCategoryNotPermitted = "SenderCategoryNotPermitted";
    private const string DuplicateInflight = "request_in_progress";

    private static readonly JsonSerializerOptions BodyOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<ChannelResult> SendAsync(
        string recipient,
        string? subject,
        string body,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        NotificationsOptions.SmsOptions sms = options.Value.Sms;
        long started = timeProvider.GetTimestamp();
        ChannelResult result = await SubmitAsync(sms, recipient, body, idempotencyKey, cancellationToken).ConfigureAwait(false);
        double elapsed = timeProvider.GetElapsedTime(started).TotalMilliseconds;
        switch (result)
        {
            case ChannelResult.Accepted:
                NotificationsLog.BirdSmsAccepted(logger, elapsed);
                break;
            case ChannelResult.Retry retry:
                NotificationsLog.BirdSmsRetried(logger, retry.Code, elapsed);
                break;
            case ChannelResult.Rejected rejected:
                NotificationsLog.BirdSmsRejected(logger, rejected.Code, elapsed);
                break;
        }

        return result;
    }

    private async Task<ChannelResult> SubmitAsync(
        NotificationsOptions.SmsOptions sms,
        string recipient,
        string body,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        Uri uri = new(sms.BaseUrl, "v1/sms/messages");
        string payload = JsonSerializer.Serialize(
            new BirdSmsRequest("+" + recipient, sms.SenderId, body, Category), BodyOptions);
        using HttpRequestMessage request = new(HttpMethod.Post, uri)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sms.ApiKey);
        request.Headers.Add(IdempotencyKeyHeader, idempotencyKey);

        HttpStatusCode status;
        string responseBody;
        try
        {
            using HttpResponseMessage response = await http.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            status = response.StatusCode;
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            return new ChannelResult.Retry("sms-transport-error");
        }

        return Map(status, responseBody);
    }

    private static ChannelResult Map(HttpStatusCode status, string responseBody)
    {
        return (int)status switch
        {
            202 => MapAccepted(responseBody),
            400 => new ChannelResult.Rejected("sms-rejected"),
            401 or 403 => new ChannelResult.Rejected("sms-unauthorized"),
            402 => new ChannelResult.Retry("sms-insufficient-balance"),
            422 => MapUnprocessable(responseBody),
            409 => MapConflict(responseBody),
            429 => new ChannelResult.Retry("sms-rate-limited"),
            >= 500 and < 600 => new ChannelResult.Retry("sms-unavailable"),
            _ => new ChannelResult.Retry("sms-protocol-error"),
        };
    }

    private static ChannelResult MapAccepted(string responseBody)
    {
        BirdSmsResponse? response = Read<BirdSmsResponse>(responseBody);
        return response?.Id is { Length: > 0 and <= ReferenceMaxLength } id && id.StartsWith(ReferencePrefix, StringComparison.Ordinal)
            ? new ChannelResult.Accepted(id)
            : new ChannelResult.Retry("sms-protocol-error");
    }

    private static ChannelResult MapUnprocessable(string responseBody)
    {
        return Read<BirdError>(responseBody)?.Code switch
        {
            InvalidRecipient => new ChannelResult.Rejected("sms-invalid-recipient"),
            SenderNotConfigured or NoEligibleSender or SenderCategoryNotPermitted => new ChannelResult.Rejected("sms-sender-rejected"),
            _ => new ChannelResult.Rejected("sms-rejected"),
        };
    }

    private static ChannelResult MapConflict(string responseBody)
    {
        return Read<BirdError>(responseBody)?.Code == DuplicateInflight
            ? new ChannelResult.Retry("sms-duplicate-inflight")
            : new ChannelResult.Retry("sms-protocol-error");
    }

    private static T? Read<T>(string responseBody) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(responseBody, ReadOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record BirdSmsRequest(string To, string From, string Text, string Category);

    private sealed record BirdSmsResponse([property: JsonPropertyName("id")] string? Id);

    private sealed record BirdError([property: JsonPropertyName("code")] string? Code);
}
