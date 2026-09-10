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
        ILogger<BirdSmsChannel>? logger = null,
        NotificationsOptions? options = null) => new(
            new HttpClient(stub),
            Options.Create(options ?? ValidOptions()),
            TimeProvider.System,
            logger ?? NullLogger<BirdSmsChannel>.Instance);

    private static StubHandler Accepted(string id = "sms_01krdgeqcxet5s7t44vh8rt9mg") =>
        new(HttpStatusCode.Accepted, "{\"id\":\"" + id + "\",\"status\":\"scheduled\"}");

    private static StubHandler Failed(HttpStatusCode status, string code) =>
        new(status, "{\"type\":\"error\",\"code\":\"" + code + "\",\"message\":\"refused\",\"request_id\":\"req_1\"}");

    [Fact]
    public async Task Accepted_sends_the_documented_request_and_returns_the_reference()
    {
        StubHandler stub = Accepted();
        BirdSmsChannel channel = Channel(stub);

        ChannelResult result = await channel.SendAsync(Recipient, "unused-subject", Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Accepted("sms_01krdgeqcxet5s7t44vh8rt9mg"), result);
        StubHandler.RequestSnapshot snapshot = Assert.Single(stub.Snapshots);
        Assert.Equal("POST", snapshot.Method);
        Assert.Equal("https://eu1.platform.bird.com/v1/sms/messages", snapshot.Uri?.ToString());
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
    [InlineData("{}")]
    [InlineData("""{"id":""}""")]
    [InlineData("""{"id":"em_01krdgeqcxet5s7t44vh8rt9mg"}""")]
    [InlineData("not json")]
    [InlineData("")]
    public async Task Acceptance_without_a_usable_reference_retries(string body)
    {
        ChannelResult result = await Channel(new StubHandler(HttpStatusCode.Accepted, body))
            .SendAsync(Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry("sms-protocol-error"), result);
    }

    [Fact]
    public async Task An_overlong_reference_retries_instead_of_failing_the_save()
    {
        ChannelResult result = await Channel(Accepted("sms_" + new string('x', 125)))
            .SendAsync(Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry("sms-protocol-error"), result);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "anything", "sms-rejected", false)]
    [InlineData(HttpStatusCode.Unauthorized, "anything", "sms-unauthorized", false)]
    [InlineData(HttpStatusCode.Forbidden, "anything", "sms-unauthorized", false)]
    [InlineData(HttpStatusCode.PaymentRequired, "anything", "sms-insufficient-balance", true)]
    [InlineData(HttpStatusCode.UnprocessableEntity, "SMSInvalidRecipient", "sms-invalid-recipient", false)]
    [InlineData(HttpStatusCode.UnprocessableEntity, "SMSSenderNotConfigured", "sms-sender-rejected", false)]
    [InlineData(HttpStatusCode.UnprocessableEntity, "SMSNoEligibleSender", "sms-sender-rejected", false)]
    [InlineData(HttpStatusCode.UnprocessableEntity, "SenderCategoryNotPermitted", "sms-sender-rejected", false)]
    [InlineData(HttpStatusCode.UnprocessableEntity, "SomethingNew", "sms-rejected", false)]
    [InlineData(HttpStatusCode.Conflict, "request_in_progress", "sms-duplicate-inflight", true)]
    [InlineData(HttpStatusCode.Conflict, "idempotency_key_reuse", "sms-protocol-error", true)]
    [InlineData(HttpStatusCode.TooManyRequests, "anything", "sms-rate-limited", true)]
    [InlineData(HttpStatusCode.InternalServerError, "anything", "sms-unavailable", true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, "anything", "sms-unavailable", true)]
    [InlineData(HttpStatusCode.Redirect, "anything", "sms-protocol-error", true)]
    [InlineData((HttpStatusCode)418, "anything", "sms-protocol-error", true)]
    public async Task Provider_outcomes_map_to_stable_results(
        HttpStatusCode status, string code, string expected, bool retry)
    {
        ChannelResult result = await Channel(Failed(status, code))
            .SendAsync(Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(retry ? new ChannelResult.Retry(expected) : new ChannelResult.Rejected(expected), result);
    }

    [Fact]
    public async Task A_malformed_error_body_keeps_its_transient_or_permanent_class()
    {
        ChannelResult retry = await Channel(new StubHandler(HttpStatusCode.ServiceUnavailable, "<html>outage</html>"))
            .SendAsync(Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);
        ChannelResult rejected = await Channel(new StubHandler(HttpStatusCode.UnprocessableEntity, "<html>nope</html>"))
            .SendAsync(Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry("sms-unavailable"), retry);
        Assert.Equal(new ChannelResult.Rejected("sms-rejected"), rejected);
    }

    [Fact]
    public async Task A_retry_after_a_lost_response_reuses_the_key_with_one_request_per_call()
    {
        StubHandler stub = Accepted();
        BirdSmsChannel channel = Channel(stub);
        CancellationToken cancellation = TestContext.Current.CancellationToken;

        await channel.SendAsync(Recipient, null, Body, "key-1", cancellation);
        await channel.SendAsync(Recipient, null, Body, "key-1", cancellation);

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
            .SendAsync(Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry("sms-transport-error"), result);
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
            .SendAsync(Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry("sms-transport-error"), result);
    }

    [Fact]
    public async Task A_canceled_caller_propagates_without_a_result()
    {
        using CancellationTokenSource canceled = new();
        canceled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Channel(Accepted())
            .SendAsync(Recipient, null, Body, "key-1", canceled.Token));
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
        Task<ChannelResult> sending = Channel(stub).SendAsync(Recipient, null, Body, "key-1", source.Token);
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

        await Channel(stub, factory.CreateLogger<BirdSmsChannel>())
            .SendAsync(Recipient, null, Body, "key-1", TestContext.Current.CancellationToken);
        await Channel(Failed(HttpStatusCode.Unauthorized, "bad_key"), factory.CreateLogger<BirdSmsChannel>())
            .SendAsync(Recipient, null, Body, "key-2", TestContext.Current.CancellationToken);

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
