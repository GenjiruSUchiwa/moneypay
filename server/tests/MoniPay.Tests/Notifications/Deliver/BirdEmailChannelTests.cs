using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MoniPay.Notifications;
using MoniPay.Notifications.Channels;
using MoniPay.Tests.Fakes;
using Xunit;

namespace MoniPay.Tests.Notifications.Deliver;

public sealed class BirdEmailChannelTests
{
    private const string BaseUrl = "https://email.tests";
    private const string ApiKey = "test-email-api-key";
    private const string Sender = "no-reply@tests.monipay.example";
    private const string Recipient = "marie.ngo@example.cm";
    private const string Subject = "Bienvenue sur MoniPay";
    private const string Body = "Bonjour Marie,\nVotre compte est prêt : utilisez le code reçu par SMS.";
    private const string Key = "email-test-key";

    [Fact]
    public async Task An_accepted_send_posts_the_allowlisted_payload_once()
    {
        StubHandler stub = new(HttpStatusCode.Accepted, """{"id":"em_01test","status":"accepted"}""");
        BirdEmailChannel channel = Channel(stub);

        ChannelResult result = await channel.SendAsync(Guid.NewGuid(), Recipient, Subject, Body, Key, TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Accepted("em_01test"), result);
        HttpRequestMessage request = Assert.Single(stub.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"{BaseUrl}/v1/email/messages", request.RequestUri?.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal(ApiKey, request.Headers.Authorization?.Parameter);
        Assert.Equal([Key], request.Headers.GetValues("Idempotency-Key"));
        using JsonDocument payload = JsonDocument.Parse(Assert.Single(stub.Snapshots).Body);
        Assert.Equal(Sender, payload.RootElement.GetProperty("from").GetString());
        string[] recipients = payload.RootElement.GetProperty("to").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
        Assert.Equal([Recipient], recipients);
        Assert.Equal(Subject, payload.RootElement.GetProperty("subject").GetString());
        Assert.Equal(Body, payload.RootElement.GetProperty("text").GetString());
        Assert.Equal("transactional", payload.RootElement.GetProperty("category").GetString());
        Assert.False(payload.RootElement.TryGetProperty("html", out _));
        Assert.False(payload.RootElement.TryGetProperty("tags", out _));
        Assert.False(payload.RootElement.TryGetProperty("metadata", out _));
        Assert.False(payload.RootElement.TryGetProperty("scheduled_at", out _));
    }

    [Fact]
    public async Task The_same_outbox_key_is_forwarded_on_every_attempt()
    {
        StubHandler stub = new(HttpStatusCode.Accepted, """{"id":"em_01retry","status":"accepted"}""");
        BirdEmailChannel channel = Channel(stub);
        CancellationToken cancellation = TestContext.Current.CancellationToken;

        await channel.SendAsync(Guid.NewGuid(), Recipient, Subject, Body, Key, cancellation);
        await channel.SendAsync(Guid.NewGuid(), Recipient, Subject, Body, Key, cancellation);

        Assert.Equal(2, stub.Requests.Count);
        Assert.All(stub.Requests, request => Assert.Equal([Key], request.Headers.GetValues("Idempotency-Key")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_missing_subject_is_rejected_without_an_http_attempt(string? subject)
    {
        StubHandler stub = new(HttpStatusCode.Accepted, """{"id":"em_01never","status":"accepted"}""");

        ChannelResult result = await Channel(stub).SendAsync(Guid.NewGuid(), Recipient, subject, Body, Key, TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Rejected(BirdEmailChannel.SubjectMissing), result);
        Assert.Empty(stub.Requests);
    }

    [Fact]
    public async Task An_oversized_subject_is_rejected_without_an_http_attempt()
    {
        StubHandler stub = new(HttpStatusCode.Accepted, """{"id":"em_01never","status":"accepted"}""");

        ChannelResult result = await Channel(stub)
            .SendAsync(Guid.NewGuid(), Recipient, new string('s', BirdEmailChannel.SubjectMaxLength + 1), Body, Key, TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Rejected(BirdEmailChannel.SubjectTooLarge), result);
        Assert.Empty(stub.Requests);
    }

    [Fact]
    public async Task An_oversized_body_is_rejected_without_an_http_attempt()
    {
        StubHandler stub = new(HttpStatusCode.Accepted, """{"id":"em_01never","status":"accepted"}""");

        ChannelResult result = await Channel(stub)
            .SendAsync(Guid.NewGuid(), Recipient, Subject, new string('b', BirdEmailChannel.BodyMaxLength + 1), Key, TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Rejected(BirdEmailChannel.BodyTooLarge), result);
        Assert.Empty(stub.Requests);
    }

    [Fact]
    public async Task An_invalid_recipient_is_rejected_without_an_http_attempt()
    {
        StubHandler stub = new(HttpStatusCode.Accepted, """{"id":"em_01never","status":"accepted"}""");

        ChannelResult result = await Channel(stub).SendAsync(Guid.NewGuid(), "not-an-email", Subject, Body, Key, TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Rejected(BirdEmailChannel.InvalidRecipient), result);
        Assert.Empty(stub.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("""{"status":"accepted"}""")]
    [InlineData("""{"id":"","status":"accepted"}""")]
    [InlineData("""{"id":"   ","status":"accepted"}""")]
    public async Task A_202_without_a_usable_reference_is_a_protocol_retry(string responseBody)
    {
        StubHandler stub = new(HttpStatusCode.Accepted, responseBody);

        ChannelResult result = await Channel(stub).SendAsync(Guid.NewGuid(), Recipient, Subject, Body, Key, TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(BirdEmailChannel.Protocol), result);
        Assert.Single(stub.Requests);
    }

    [Fact]
    public async Task A_202_with_an_oversized_reference_is_a_protocol_retry_before_persistence()
    {
        StubHandler stub = new(HttpStatusCode.Accepted, $$"""{"id":"{{new string('r', BirdEmailChannel.ReferenceMaxLength + 1)}}","status":"accepted"}""");

        ChannelResult result = await Channel(stub).SendAsync(Guid.NewGuid(), Recipient, Subject, Body, Key, TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(BirdEmailChannel.Protocol), result);
    }

    [Theory]
    [InlineData("""{"error":{"type":"conflict_error","code":"E01004","name":"RequestInProgress"}}""", BirdEmailChannel.KeyInFlight, true)]
    [InlineData("""{"error":{"type":"conflict_error","code":"E01005","name":"IdempotencyKeyReuse"}}""", BirdEmailChannel.KeyConflict, false)]
    [InlineData("not json", BirdEmailChannel.Protocol, true)]
    [InlineData("""{"error":{"type":"conflict_error","code":"E09999","name":"Unknown"}}""", BirdEmailChannel.Protocol, true)]
    public async Task A_409_maps_the_key_state_without_minting_a_new_key(string responseBody, string code, bool retry)
    {
        StubHandler stub = new(HttpStatusCode.Conflict, responseBody);

        ChannelResult result = await Channel(stub).SendAsync(Guid.NewGuid(), Recipient, Subject, Body, Key, TestContext.Current.CancellationToken);

        Assert.Equal(retry ? new ChannelResult.Retry(code) : new ChannelResult.Rejected(code), result);
        Assert.Single(stub.Requests);
    }

    public static TheoryData<HttpStatusCode, string, string, bool> Statuses => new()
    {
        { HttpStatusCode.TooManyRequests, "not json", BirdEmailChannel.RateLimited, true },
        { HttpStatusCode.InternalServerError, "not json", BirdEmailChannel.ServerError, true },
        { HttpStatusCode.BadGateway, Error("E04008", "EmailBackendUnavailable"), BirdEmailChannel.ServerError, true },
        { HttpStatusCode.Unauthorized, "not json", BirdEmailChannel.Unauthorized, false },
        { HttpStatusCode.Forbidden, Error("MS40301", "Forbidden"), BirdEmailChannel.Unauthorized, false },
        { (HttpStatusCode)421, Error("E01010", "Misdirected"), BirdEmailChannel.Unauthorized, false },
        { HttpStatusCode.UnprocessableEntity, Error("E04006", "DomainNotVerified"), BirdEmailChannel.ProviderRejected, false },
        { HttpStatusCode.BadRequest, "not json", BirdEmailChannel.ProviderRejected, false },
        { HttpStatusCode.NotFound, Error("E04021", "EmailTemplateNotFound"), BirdEmailChannel.ProviderRejected, false },
        { HttpStatusCode.PaymentRequired, Error("E04069", "OverageUnfunded"), BirdEmailChannel.ProviderRejected, false },
        { HttpStatusCode.Found, string.Empty, BirdEmailChannel.UnexpectedStatus, false },
        { HttpStatusCode.Created, """{"id":"em_01wrong","status":"accepted"}""", BirdEmailChannel.ProviderRejected, false },
    };

    [Theory]
    [MemberData(nameof(Statuses))]
    public async Task One_http_attempt_maps_to_one_stable_result(HttpStatusCode status, string responseBody, string code, bool retry)
    {
        StubHandler stub = new(status, responseBody);

        ChannelResult result = await Channel(stub).SendAsync(Guid.NewGuid(), Recipient, Subject, Body, Key, TestContext.Current.CancellationToken);

        Assert.Equal(retry ? new ChannelResult.Retry(code) : new ChannelResult.Rejected(code), result);
        Assert.Single(stub.Requests);
    }

    [Fact]
    public async Task A_network_failure_is_a_transport_retry()
    {
        BirdEmailChannel channel = new(
            new HttpClient(new ThrowingHandler(new HttpRequestException("dns"))) { BaseAddress = new Uri(BaseUrl) },
            TestOptions());

        ChannelResult result = await channel.SendAsync(Guid.NewGuid(), Recipient, Subject, Body, Key, TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(BirdEmailChannel.Transport), result);
    }

    [Fact]
    public async Task A_client_timeout_is_a_timeout_retry_while_the_supplied_token_stays_active()
    {
        DelayHandler delayed = new(TimeSpan.FromSeconds(5), Accepted("em_01slow"));
        BirdEmailChannel channel = new(
            new HttpClient(delayed) { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromMilliseconds(50) },
            TestOptions());
        CancellationToken cancellation = TestContext.Current.CancellationToken;

        ChannelResult result = await channel.SendAsync(Guid.NewGuid(), Recipient, Subject, Body, Key, cancellation);

        Assert.Equal(new ChannelResult.Retry(ChannelResult.ProviderTimeout), result);
        Assert.False(cancellation.IsCancellationRequested);
    }

    [Fact]
    public async Task A_202_without_content_is_a_protocol_retry()
    {
        BirdEmailChannel channel = Channel(new NullContentHandler(HttpStatusCode.Accepted));

        ChannelResult result = await channel.SendAsync(Guid.NewGuid(), Recipient, Subject, Body, Key, TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(BirdEmailChannel.Protocol), result);
    }

    [Fact]
    public async Task A_409_without_content_is_a_protocol_retry()
    {
        BirdEmailChannel channel = Channel(new NullContentHandler(HttpStatusCode.Conflict));

        ChannelResult result = await channel.SendAsync(Guid.NewGuid(), Recipient, Subject, Body, Key, TestContext.Current.CancellationToken);

        Assert.Equal(new ChannelResult.Retry(BirdEmailChannel.Protocol), result);
    }

    [Fact]
    public async Task An_already_cancelled_call_sends_nothing_and_propagates()
    {
        StubHandler stub = new(HttpStatusCode.Accepted, Accepted("em_01never"));
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Channel(stub).SendAsync(Guid.NewGuid(), Recipient, Subject, Body, Key, cancelled.Token));

        Assert.Empty(stub.Requests);
    }

    [Fact]
    public async Task A_cancellation_during_the_send_propagates()
    {
        DelayHandler delayed = new(TimeSpan.FromSeconds(5), Accepted("em_01never"));
        using CancellationTokenSource cancelled = new();
        Task<ChannelResult> sending = Channel(delayed).SendAsync(Guid.NewGuid(), Recipient, Subject, Body, Key, cancelled.Token);

        await delayed.Entered.WaitAsync(TestContext.Current.CancellationToken);

        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sending);
    }

    private static BirdEmailChannel Channel(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) }, TestOptions());

    private static IOptions<NotificationsOptions> TestOptions() =>
        Options.Create(new NotificationsOptions
        {
            Email = new EmailOptions { BaseUrl = BaseUrl, ApiKey = ApiKey, FromAddress = Sender },
        });

    private static string Accepted(string id) => $$"""{"id":"{{id}}","status":"accepted"}""";

    private static string Error(string code, string name) =>
        "{\"error\":{\"type\":\"error\",\"code\":\"" + code + "\",\"name\":\"" + name + "\"}}";

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw exception;
    }

    private sealed class DelayHandler(TimeSpan delay, string responseBody) : HttpMessageHandler
    {
        private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Entered => entered.Task;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            entered.TrySetResult();
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class NullContentHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
