using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MoniPay.Notifications;
using MoniPay.Notifications.Channels;
using MoniPay.Tests.Fakes;
using Xunit;

namespace MoniPay.Tests.Notifications.Channels;

public sealed class BirdSmsChannelTests
{
    private const string Recipient = "237600000001";
    private const string Body = "Votre code MoniPay est le 041822, valable 5 minutes.";
    private const string ApiKey = "test-key-not-a-secret";
    private const string AcceptedReference = "sms_01krdgeqcxet5s7t44vh8rt9mg";

    private static readonly Guid NotificationId = Guid.CreateVersion7();

    private static NotificationsOptions ValidOptions() => new()
    {
        Sms = new NotificationsOptions.SmsOptions
        {
            BaseUrl = new Uri("https://eu1.platform.bird.com"),
            ApiKey = ApiKey,
            SenderId = "MoniPay",
        },
    };

    private static BirdSmsChannel Channel(
        StubHandler stub,
        NotificationsOptions? options = null,
        ILogger<BirdSmsChannel>? logger = null)
    {
        NotificationsOptions resolved = options ?? ValidOptions();
        HttpClient http = new(stub)
        {
            BaseAddress = resolved.Sms?.NormalizedBaseUrl,
            MaxResponseContentBufferSize = BirdSmsChannel.MaxResponseBufferBytes,
        };
        return new(http, Options.Create(resolved), TimeProvider.System, logger ?? NullLogger<BirdSmsChannel>.Instance);
    }

    private static StubHandler Accepted(string id = AcceptedReference) =>
        new(HttpStatusCode.Accepted, "{\"id\":\"" + id + "\",\"status\":\"scheduled\"}");

    private static StubHandler Failed(HttpStatusCode status, string code) =>
        new(status, "{\"type\":\"error\",\"code\":\"" + code + "\",\"message\":\"refused\",\"request_id\":\"req_1\"}");

    [Fact]
    public async Task Accepted_sends_the_documented_request_and_returns_the_reference()
    {
        StubHandler stub = Accepted();
        BirdSmsChannel channel = Channel(stub);

        ChannelResult result = await channel.SendAsync(
            NotificationId, Recipient, "unused-subject", Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Accepted(AcceptedReference), result);
        StubHandler.RequestSnapshot snapshot = Assert.Single(stub.Snapshots);
        Assert.Equal("POST", snapshot.Method);
        Assert.Equal("https://eu1.platform.bird.com/" + BirdSmsRoutes.MessagesRoute, snapshot.Uri?.ToString());
        Assert.Equal("Bearer " + ApiKey, snapshot.Authorization);
        Assert.Equal("key-1", snapshot.IdempotencyKey);
        Assert.Equal("application/json; charset=utf-8", snapshot.ContentType);
        using JsonDocument payload = JsonDocument.Parse(snapshot.Body);
        Assert.Equal("+237600000001", payload.RootElement.GetProperty("to").GetString());
        Assert.Equal("MoniPay", payload.RootElement.GetProperty("from").GetString());
        Assert.Equal(Body, payload.RootElement.GetProperty("text").GetString());
        Assert.Equal("authentication", payload.RootElement.GetProperty("category").GetString());
        Assert.False(payload.RootElement.TryGetProperty("subject", out _));
    }

    [Theory]
    [InlineData("237600000001", "+237600000001")]
    [InlineData("+237600000001", "+237600000001")]
    public void ToBirdRecipient_prefixes_a_missing_plus(string recipient, string expected)
    {
        Assert.Equal(expected, BirdSmsChannel.ToBirdRecipient(recipient));
    }

    [Fact]
    public async Task A_base_url_with_a_path_keeps_it_before_the_route()
    {
        NotificationsOptions options = new()
        {
            Sms = new NotificationsOptions.SmsOptions
            {
                BaseUrl = new Uri("https://api.bird.com/workspaces/W123/channels/C456"),
                ApiKey = ApiKey,
                SenderId = "MoniPay",
            },
        };
        StubHandler stub = Accepted();

        ChannelResult result = await Channel(stub, options)
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Accepted(AcceptedReference), result);
        StubHandler.RequestSnapshot snapshot = Assert.Single(stub.Snapshots);
        Assert.Equal(
            "https://api.bird.com/workspaces/W123/channels/C456/" + BirdSmsRoutes.MessagesRoute,
            snapshot.Uri?.ToString());
    }

    [Fact]
    public async Task Missing_sms_settings_retries_as_a_protocol_error()
    {
        ChannelResult result = await Channel(Accepted(), new NotificationsOptions())
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(BirdSmsCodes.ProtocolError), result);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"id":""}""")]
    [InlineData("""{"id":"em_01krdgeqcxet5s7t44vh8rt9mg"}""")]
    [InlineData("not json")]
    [InlineData("")]
    public async Task Acceptance_without_a_usable_reference_retries(string body)
    {
        ChannelResult result = await Channel(new StubHandler(HttpStatusCode.Accepted, body))
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(BirdSmsCodes.ProtocolError), result);
    }

    [Fact]
    public async Task An_overlong_reference_retries_instead_of_failing_the_save()
    {
        ChannelResult result = await Channel(Accepted("sms_" + new string('x', 125)))
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(BirdSmsCodes.ProtocolError), result);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "anything", BirdSmsCodes.Rejected)]
    [InlineData(HttpStatusCode.UnprocessableEntity, "SMSInvalidRecipient", BirdSmsCodes.InvalidRecipient)]
    [InlineData(HttpStatusCode.UnprocessableEntity, "SMSSenderNotConfigured", BirdSmsCodes.SenderRejected)]
    [InlineData(HttpStatusCode.UnprocessableEntity, "SMSNoEligibleSender", BirdSmsCodes.SenderRejected)]
    [InlineData(HttpStatusCode.UnprocessableEntity, "SenderCategoryNotPermitted", BirdSmsCodes.SenderRejected)]
    [InlineData(HttpStatusCode.UnprocessableEntity, "SomethingNew", BirdSmsCodes.Rejected)]
    public async Task Provider_permanent_outcomes_map_to_rejected(
        HttpStatusCode status, string code, string expected)
    {
        ChannelResult result = await Channel(Failed(status, code))
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Rejected(expected), result);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "anything", BirdSmsCodes.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, "anything", BirdSmsCodes.Unauthorized)]
    [InlineData(HttpStatusCode.PaymentRequired, "anything", BirdSmsCodes.InsufficientBalance)]
    [InlineData(HttpStatusCode.Conflict, "request_in_progress", BirdSmsCodes.DuplicateInflight)]
    [InlineData(HttpStatusCode.Conflict, "idempotency_key_reuse", BirdSmsCodes.ProtocolError)]
    [InlineData(HttpStatusCode.TooManyRequests, "anything", BirdSmsCodes.RateLimited)]
    [InlineData(HttpStatusCode.InternalServerError, "anything", BirdSmsCodes.Unavailable)]
    [InlineData(HttpStatusCode.ServiceUnavailable, "anything", BirdSmsCodes.Unavailable)]
    [InlineData(HttpStatusCode.Redirect, "anything", BirdSmsCodes.ProtocolError)]
    [InlineData((HttpStatusCode)418, "anything", BirdSmsCodes.ProtocolError)]
    public async Task Provider_transient_outcomes_map_to_retry(
        HttpStatusCode status, string code, string expected)
    {
        ChannelResult result = await Channel(Failed(status, code))
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(expected), result);
    }

    [Fact]
    public async Task A_malformed_error_body_keeps_its_transient_or_permanent_class()
    {
        ChannelResult retry = await Channel(new StubHandler(HttpStatusCode.ServiceUnavailable, "<html>outage</html>"))
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);
        ChannelResult rejected = await Channel(new StubHandler(HttpStatusCode.UnprocessableEntity, "<html>nope</html>"))
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(BirdSmsCodes.Unavailable), retry);
        Assert.Equal(new ChannelResult.Rejected(BirdSmsCodes.Rejected), rejected);
    }

    [Fact]
    public async Task A_retry_after_a_lost_response_reuses_the_key_with_one_request_per_call()
    {
        StubHandler stub = Accepted();
        BirdSmsChannel channel = Channel(stub);
        CancellationToken cancellation = TestContext.Current.CancellationToken;

        await channel.SendAsync(NotificationId, Recipient, null, Body, "key-1", cancellation);
        await channel.SendAsync(NotificationId, Recipient, null, Body, "key-1", cancellation);

        Assert.Equal(
            ["key-1", "key-1"],
            stub.Snapshots.Select(snapshot => snapshot.IdempotencyKey ?? string.Empty).ToArray());
    }

    [Fact]
    public async Task A_transport_failure_retries()
    {
        StubHandler stub = Accepted();
        stub.Behavior = (_, _) => throw new HttpRequestException("connection reset");

        ChannelResult result = await Channel(stub)
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(BirdSmsCodes.TransportError), result);
    }

    [Fact]
    public async Task A_body_read_failure_retries()
    {
        StubHandler stub = Accepted();
        stub.Behavior = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new FailingContent(),
        });

        ChannelResult result = await Channel(stub)
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(BirdSmsCodes.TransportError), result);
    }

    [Fact]
    public async Task A_channel_internal_failure_returns_a_retry()
    {
        StubHandler stub = Accepted();

        ChannelResult result = await Channel(stub)
            .SendAsync(NotificationId, Recipient, null, Body, "bad\nkey", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(BirdSmsCodes.ProtocolError), result);
        Assert.Empty(stub.Snapshots);
    }

    [Fact]
    public async Task An_oversized_response_retries_without_buffering_the_whole_body()
    {
        StubHandler stub = new(HttpStatusCode.Accepted, new string('x', (int)BirdSmsChannel.MaxResponseBufferBytes + 1));

        ChannelResult result = await Channel(stub)
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(BirdSmsCodes.TransportError), result);
    }

    [Fact]
    public async Task A_canceled_caller_propagates_without_a_result()
    {
        using CancellationTokenSource canceled = new();
        canceled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Channel(Accepted())
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", canceled.Token));
    }

    [Fact]
    public async Task An_inflight_cancel_propagates_without_a_result()
    {
        StubHandler stub = Accepted();
        stub.Behavior = async (_, cancellation) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellation).ConfigureAwait(false);
            throw new InvalidOperationException("unreached");
        };
        using CancellationTokenSource source = new();
        Task<ChannelResult> sending = Channel(stub)
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", source.Token);
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sending);
        Assert.Single(stub.Snapshots);
    }

    [Fact]
    public async Task Logs_carry_no_phone_code_body_or_key()
    {
        RecordingLoggerProvider logs = new();
        using ILoggerFactory factory = LoggerFactory.Create(logging => logging.AddProvider(logs));
        StubHandler stub = Accepted();

        await Channel(stub, logger: factory.CreateLogger<BirdSmsChannel>())
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);
        await Channel(Failed(HttpStatusCode.Unauthorized, "bad_key"), logger: factory.CreateLogger<BirdSmsChannel>())
            .SendAsync(NotificationId, Recipient, null, Body, "key-2", TestContext.Current.CancellationToken);

        Assert.NotEmpty(logs.Entries);
        string[] secrets = ["237600000001", "041822", Body, ApiKey];
        foreach (RecordingLoggerProvider.LogEntry entry in logs.Entries)
        {
            foreach (string secret in secrets)
            {
                Assert.DoesNotContain(secret, entry.Message, StringComparison.Ordinal);
                Assert.All(entry.State, pair => Assert.DoesNotContain(
                    secret, pair.Value?.ToString() ?? string.Empty, StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public async Task A_transport_retry_logs_the_notification_and_the_failure()
    {
        RecordingLoggerProvider logs = new();
        using ILoggerFactory factory = LoggerFactory.Create(logging => logging.AddProvider(logs));
        StubHandler stub = Accepted();
        stub.Behavior = (_, _) => throw new HttpRequestException("connection reset");

        await Channel(stub, logger: factory.CreateLogger<BirdSmsChannel>())
            .SendAsync(NotificationId, Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        RecordingLoggerProvider.LogEntry entry = Assert.Single(
            logs.Entries,
            entry => entry.Category == typeof(BirdSmsChannel).FullName && entry.Level == LogLevel.Warning);
        Assert.Contains(NotificationId.ToString(), entry.Message, StringComparison.Ordinal);
        Assert.Contains(BirdSmsCodes.TransportError, entry.Message, StringComparison.Ordinal);
        Assert.Equal("connection reset", entry.Exception?.Message);
    }

    private sealed class FailingContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            throw new IOException("body lost");

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
