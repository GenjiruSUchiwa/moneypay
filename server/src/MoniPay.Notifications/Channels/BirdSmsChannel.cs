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
    internal const long MaxResponseBufferBytes = 16 * 1024;

    internal static readonly TimeSpan HttpTimeout = Timeout.InfiniteTimeSpan;

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

    internal HttpClient Http => http;

    internal static SocketsHttpHandler CreatePrimaryHandler() => new() { AllowAutoRedirect = false };

    internal static string ToBirdRecipient(string recipient) =>
        recipient.StartsWith('+') ? recipient : "+" + recipient;

    public async Task<ChannelResult> SendAsync(
        Guid notificationId,
        string recipient,
        string? subject,
        string body,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (options.Value.Sms is not { } sms)
        {
            return new ChannelResult.Retry(BirdSmsCodes.ProtocolError);
        }

        long started = timeProvider.GetTimestamp();
        (ChannelResult result, Exception? failure) = await SubmitAsync(
            sms, recipient, body, idempotencyKey, cancellationToken).ConfigureAwait(false);
        double elapsed = timeProvider.GetElapsedTime(started).TotalMilliseconds;
        switch (result)
        {
            case ChannelResult.Accepted:
                NotificationsLog.BirdSmsAccepted(logger, notificationId, elapsed);
                break;
            case ChannelResult.Retry retry:
                NotificationsLog.BirdSmsRetried(logger, notificationId, retry.Code, elapsed, failure);
                break;
            case ChannelResult.Rejected rejected:
                NotificationsLog.BirdSmsRejected(logger, notificationId, rejected.Code, elapsed);
                break;
        }

        return result;
    }

    private async Task<(ChannelResult Result, Exception? Failure)> SubmitAsync(
        NotificationsOptions.SmsOptions sms,
        string recipient,
        string body,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        try
        {
            Uri uri = new(BirdSmsRoutes.MessagesRoute, UriKind.Relative);
            string payload = JsonSerializer.Serialize(
                new BirdSmsRequest(ToBirdRecipient(recipient), sms.SenderId, body, Category), BodyOptions);
            using HttpRequestMessage request = new(HttpMethod.Post, uri)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sms.ApiKey);
            request.Headers.Add(IdempotencyKeyHeader, idempotencyKey);

            using HttpResponseMessage response = await http.SendAsync(
                request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return (Map(response.StatusCode, responseBody), null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            string code = exception is HttpRequestException or IOException
                ? BirdSmsCodes.TransportError
                : BirdSmsCodes.ProtocolError;
            return (new ChannelResult.Retry(code), exception);
        }
    }

    private static ChannelResult Map(HttpStatusCode status, string responseBody)
    {
        return (int)status switch
        {
            202 => MapAccepted(responseBody),
            400 => new ChannelResult.Rejected(BirdSmsCodes.Rejected),
            401 or 403 => new ChannelResult.Retry(BirdSmsCodes.Unauthorized),
            402 => new ChannelResult.Retry(BirdSmsCodes.InsufficientBalance),
            422 => MapUnprocessable(responseBody),
            409 => MapConflict(responseBody),
            429 => new ChannelResult.Retry(BirdSmsCodes.RateLimited),
            >= 500 and < 600 => new ChannelResult.Retry(BirdSmsCodes.Unavailable),
            _ => new ChannelResult.Retry(BirdSmsCodes.ProtocolError),
        };
    }

    private static ChannelResult MapAccepted(string responseBody)
    {
        BirdSmsResponse? response = Read<BirdSmsResponse>(responseBody);
        return response?.Id is { Length: > 0 and <= ReferenceMaxLength } id && id.StartsWith(ReferencePrefix, StringComparison.Ordinal)
            ? new ChannelResult.Accepted(id)
            : new ChannelResult.Retry(BirdSmsCodes.ProtocolError);
    }

    private static ChannelResult MapUnprocessable(string responseBody)
    {
        return Read<BirdError>(responseBody)?.Code switch
        {
            InvalidRecipient => new ChannelResult.Rejected(BirdSmsCodes.InvalidRecipient),
            SenderNotConfigured or NoEligibleSender or SenderCategoryNotPermitted => new ChannelResult.Rejected(BirdSmsCodes.SenderRejected),
            _ => new ChannelResult.Rejected(BirdSmsCodes.Rejected),
        };
    }

    private static ChannelResult MapConflict(string responseBody)
    {
        return Read<BirdError>(responseBody)?.Code == DuplicateInflight
            ? new ChannelResult.Retry(BirdSmsCodes.DuplicateInflight)
            : new ChannelResult.Retry(BirdSmsCodes.ProtocolError);
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
