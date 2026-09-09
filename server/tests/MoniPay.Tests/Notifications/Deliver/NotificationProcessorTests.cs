using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoniPay.Api;
using MoniPay.Notifications;
using MoniPay.Notifications.Channels;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Features.Deliver;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using MoniPay.Tests.Fakes;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Notifications.Deliver;

public sealed class NotificationProcessorTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private const string Body = "Your MoniPay code is 482913, valid 5 minutes.";

    private static readonly string[] ContentKeys = ["Recipient", "Body", "Code"];

    [Fact]
    public async Task Two_parallel_cycles_on_one_pending_row_send_exactly_one_message()
    {
        string phone = TestPhones.Next();
        Api.Sms.Result = new ChannelResult.Accepted("ref-parallel");
        Guid id = await EnqueueAsync(Message(phone));

        await Task.WhenAll(Api.RunNotificationCycleAsync(Cancellation), Api.RunNotificationCycleAsync(Cancellation));

        Assert.Single(Api.Sms.CallsFor(phone));
        Notification row = await RowAsync(id);
        Assert.Equal(NotificationStatus.Sent, row.Status);
        Assert.Equal("ref-parallel", row.ProviderReference);
        Assert.Equal(Api.Time.GetUtcNow(), row.SentAt);
        Assert.Null(row.LeaseUntil);
        Assert.Equal("482913", Api.Sms.CodeFor(phone));
    }

    [Fact]
    public async Task Accepted_clears_the_body_ciphertext_so_a_later_read_reveals_no_code()
    {
        string phone = TestPhones.Next();
        Api.Sms.Result = new ChannelResult.Accepted("ref-body");
        Guid id = await EnqueueAsync(Message(phone));

        await Api.RunNotificationCycleAsync(Cancellation);

        IReadOnlyList<bool> bodyIsNull = await Api.QueryAsync(
            "SELECT body_ciphertext IS NULL FROM notifications WHERE id = @id::uuid;",
            reader => reader.GetBoolean(0),
            ("id", id.ToString()));
        Assert.Equal([true], bodyIsNull);
    }

    [Fact]
    public async Task A_retry_follows_the_verification_code_schedule_and_then_ends_failed()
    {
        string phone = TestPhones.Next();
        Api.Sms.Result = new ChannelResult.Retry("provider-5xx");
        Guid id = await EnqueueAsync(Message(phone) with { Required = false });
        DateTimeOffset start = Api.Time.GetUtcNow();

        TimeSpan[] schedule = [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2)];
        DateTimeOffset expectedNext = start;
        for (int attempt = 1; attempt <= schedule.Length; attempt++)
        {
            Api.Time.Set(expectedNext);
            await Api.RunNotificationCycleAsync(Cancellation);

            Notification row = await RowAsync(id);
            expectedNext += schedule[attempt - 1];
            Assert.Equal(NotificationStatus.Pending, row.Status);
            Assert.Equal(attempt, row.Attempts);
            Assert.Equal(expectedNext, row.NextAttemptAt);
            Assert.Equal("provider-5xx", row.LastErrorCode);
            Assert.Null(row.LeaseUntil);
        }

        Api.Time.Set(expectedNext - TimeSpan.FromSeconds(1));
        await Api.RunNotificationCycleAsync(Cancellation);
        Assert.Equal(schedule.Length, Api.Sms.CallsFor(phone).Count);

        Api.Time.Set(expectedNext);
        await Api.RunNotificationCycleAsync(Cancellation);

        Notification failed = await RowAsync(id);
        Assert.Equal(NotificationStatus.Failed, failed.Status);
        Assert.Equal(schedule.Length + 1, failed.Attempts);
        Assert.Equal(schedule.Length + 1, Api.Sms.CallsFor(phone).Count);
    }

    [Fact]
    public void Other_kinds_follow_the_long_schedule()
    {
        Assert.Equal(TimeSpan.FromMinutes(1), RetrySchedule.NextDelay("Welcome", 1));
        Assert.Equal(TimeSpan.FromMinutes(5), RetrySchedule.NextDelay("Welcome", 2));
        Assert.Equal(TimeSpan.FromMinutes(15), RetrySchedule.NextDelay("Welcome", 3));
        Assert.Equal(TimeSpan.FromHours(1), RetrySchedule.NextDelay("Welcome", 4));
        Assert.Null(RetrySchedule.NextDelay("Welcome", 5));
        Assert.Null(RetrySchedule.NextDelay(RetrySchedule.VerificationCodeKind, 4));
    }

    [Fact]
    public async Task A_rejected_result_ends_failed_at_once()
    {
        string phone = TestPhones.Next();
        Api.Sms.Result = new ChannelResult.Rejected("invalid-recipient");
        Guid id = await EnqueueAsync(Message(phone) with { Required = false });

        await Api.RunNotificationCycleAsync(Cancellation);

        Notification row = await RowAsync(id);
        Assert.Equal(NotificationStatus.Failed, row.Status);
        Assert.Equal("invalid-recipient", row.LastErrorCode);
        Assert.Null(row.LeaseUntil);
        Assert.Single(Api.Sms.CallsFor(phone));
    }

    [Fact]
    public async Task An_expired_message_is_marked_expired_and_never_reaches_the_channel()
    {
        string phone = TestPhones.Next();
        Guid id = await EnqueueAsync(Message(phone) with { ExpiresAt = Api.Time.GetUtcNow() + TimeSpan.FromMinutes(5) });
        Api.Time.Advance(TimeSpan.FromMinutes(5));

        await Api.RunNotificationCycleAsync(Cancellation);

        Assert.Equal(NotificationStatus.Expired, (await RowAsync(id)).Status);
        Assert.Empty(Api.Sms.CallsFor(phone));
    }

    [Fact]
    public async Task A_lease_left_by_a_dead_cycle_expires_and_the_next_cycle_reclaims_the_row()
    {
        string phone = TestPhones.Next();
        Api.Sms.Result = new ChannelResult.Accepted("ref-lease");
        Guid id = await EnqueueAsync(Message(phone));
        TimeSpan lease = Api.Services.GetRequiredService<IOptions<NotificationsOptions>>().Value.Worker.LeaseDuration;
        DateTimeOffset leaseUntil = Api.Time.GetUtcNow() + lease;

        using (IServiceScope scope = Api.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<MoniPayDbContext>().Notifications
                .Where(row => row.Id == id)
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.LeaseUntil, leaseUntil), Cancellation);
        }

        await Api.RunNotificationCycleAsync(Cancellation);
        Assert.Empty(Api.Sms.CallsFor(phone));
        Assert.Equal(NotificationStatus.Pending, (await RowAsync(id)).Status);

        Api.Time.Advance(lease + TimeSpan.FromSeconds(1));
        await Api.RunNotificationCycleAsync(Cancellation);

        Assert.Single(Api.Sms.CallsFor(phone));
        Assert.Equal(NotificationStatus.Sent, (await RowAsync(id)).Status);
    }

    [Fact]
    public async Task The_provider_timeout_reaches_the_channel_through_the_linked_token()
    {
        string phone = TestPhones.Next();
        Api.Sms.SlowRecipient = phone;
        Guid id = await EnqueueAsync(Message(phone) with { Required = false });
        TimeSpan providerTimeout = Api.Services.GetRequiredService<IOptions<NotificationsOptions>>().Value.ProviderTimeout;

        try
        {
            Task<int> cycle = Api.RunNotificationCycleAsync(Cancellation);
            await UntilCalledAsync(phone);
            Assert.False(Api.Sms.CallsFor(phone)[0].Token.IsCancellationRequested);

            Api.Time.Advance(providerTimeout);
            await cycle;
        }
        finally
        {
            Api.Sms.SlowRecipient = null;
        }

        Assert.True(Api.Sms.CallsFor(phone)[0].Token.IsCancellationRequested);
        Notification row = await RowAsync(id);
        Assert.Equal(NotificationStatus.Pending, row.Status);
        Assert.Equal(1, row.Attempts);
        Assert.Equal(NotificationProcessor.ProviderTimeout, row.LastErrorCode);
    }

    [Fact]
    public async Task The_cycle_cancellation_reaches_the_channel_through_the_linked_token()
    {
        string phone = TestPhones.Next();
        Api.Sms.SlowRecipient = phone;
        await EnqueueAsync(Message(phone) with { Required = false });

        try
        {
            using CancellationTokenSource cycleSource = new();
            Task<int> cycle = Api.RunNotificationCycleAsync(cycleSource.Token);
            await UntilCalledAsync(phone);

            cycleSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cycle);
        }
        finally
        {
            Api.Sms.SlowRecipient = null;
        }

        Assert.True(Api.Sms.CallsFor(phone)[0].Token.IsCancellationRequested);
    }

    [Fact]
    public async Task Without_a_channel_the_row_stays_pending_and_one_warning_is_logged_per_cycle()
    {
        RecordingLoggerProvider logs = new();
        using WebApplicationFactory<Program> factory = ChannelLessHost(logs);
        Guid[] ids = await EnqueueWithoutChannelAsync(factory.Services, "NoChannelA", "NoChannelB");

        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            int claimed = await scope.ServiceProvider.GetRequiredService<NotificationProcessor>().RunCycleAsync(Cancellation);
            Assert.True(claimed >= 2);
        }

        await AssertWaitingWithoutChannelAsync(factory.Services, ids);
        Assert.Single(logs.Entries, entry => entry.Level == LogLevel.Warning && entry.Category == typeof(NotificationProcessor).FullName);
    }

    private WebApplicationFactory<Program> ChannelLessHost(RecordingLoggerProvider logs) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(MoniPayEnvironments.Testing);
            builder.UseSetting("ConnectionStrings:MoniPay", Api.ConnectionString);
            builder.UseTestKeys();
            MoniPayApi.UseNotificationTestSettings(builder);
            builder.ConfigureServices(services => services.AddLogging(logging => logging.AddProvider(logs)));
        });

    private async Task<Guid[]> EnqueueWithoutChannelAsync(IServiceProvider services, params string[] kinds)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        NotificationOutbox outbox = scope.ServiceProvider.GetRequiredService<NotificationOutbox>();
        foreach (string kind in kinds)
        {
            outbox.Enqueue(Message(TestPhones.Next()) with { Kind = kind });
        }

        await database.SaveChangesAsync(Cancellation);
        return await database.Notifications.Where(row => kinds.Contains(row.Kind)).Select(row => row.Id).ToArrayAsync(Cancellation);
    }

    private async Task AssertWaitingWithoutChannelAsync(IServiceProvider services, Guid[] ids)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Notification[] rows = await database.Notifications.AsNoTracking().Where(row => ids.Contains(row.Id)).ToArrayAsync(Cancellation);
        Assert.Equal(ids.Length, rows.Length);
        Assert.All(rows, row =>
        {
            Assert.Equal(NotificationStatus.Pending, row.Status);
            Assert.Equal(NotificationProcessor.ChannelNotConfigured, row.LastErrorCode);
            Assert.Equal(0, row.Attempts);
            Assert.Null(row.LeaseUntil);
        });

        await database.Notifications.Where(row => ids.Contains(row.Id)).ExecuteDeleteAsync(Cancellation);
    }

    [Fact]
    public async Task A_required_message_that_exhausts_its_retries_raises_one_critical_event()
    {
        string phone = TestPhones.Next();
        Api.Sms.Result = new ChannelResult.Rejected("blocked");
        int criticalBefore = Api.Logs.Entries.Count(entry => entry.Level == LogLevel.Critical);
        Guid id = await EnqueueAsync(Message(phone) with { Required = true });

        await Api.RunNotificationCycleAsync(Cancellation);

        Assert.Equal(NotificationStatus.Failed, (await RowAsync(id)).Status);
        RecordingLoggerProvider.LogEntry critical = Assert.Single(
            Api.Logs.Entries.Where(entry => entry.Level == LogLevel.Critical).Skip(criticalBefore));
        Assert.Contains(critical.State, pair => pair.Key == "NotificationId" && Equals(pair.Value, id));
        Assert.Contains(critical.State, pair => pair.Key == "RecipientHint");
        Assert.DoesNotContain(phone, critical.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("482913", critical.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_optional_welcome_that_fails_emits_no_critical_event()
    {
        string email = $"welcome.{Guid.NewGuid():N}@example.com";
        Api.Email.Result = new ChannelResult.Rejected("blocked");
        int before = Api.Logs.Entries.Count;
        Guid id = await EnqueueAsync(Message(TestPhones.Next()) with
        {
            Channel = NotificationChannel.Email,
            Recipient = email,
            Subject = "Bienvenue sur MoniPay",
            Kind = "Welcome",
            Required = false,
        });

        await Api.RunNotificationCycleAsync(Cancellation);

        Assert.Equal(NotificationStatus.Failed, (await RowAsync(id)).Status);
        Assert.DoesNotContain(
            Api.Logs.Entries.Skip(before),
            entry => entry.Level == LogLevel.Critical);
    }

    [Fact]
    public async Task Log_events_carry_no_recipient_body_or_code()
    {
        string phone = TestPhones.Next();
        Api.Sms.Result = new ChannelResult.Accepted("ref-logs");
        await EnqueueAsync(Message(phone));
        int before = Api.Logs.Entries.Count;

        await Api.RunNotificationCycleAsync(Cancellation);

        IReadOnlyList<RecordingLoggerProvider.LogEntry> written = Api.Logs.Entries.Skip(before)
            .Where(entry => entry.Category?.StartsWith("MoniPay.Notifications", StringComparison.Ordinal) == true)
            .ToArray();
        Assert.NotEmpty(written);
        foreach (RecordingLoggerProvider.LogEntry entry in written)
        {
            Assert.DoesNotContain(phone, entry.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("482913", entry.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("ref-logs", entry.Message, StringComparison.Ordinal);
            Assert.All(entry.State, pair => Assert.DoesNotContain(pair.Key, ContentKeys));
        }
    }

    private static OutboundMessage Message(string phone) => new(
        Channel: NotificationChannel.Sms,
        Recipient: phone,
        Subject: null,
        Body: Body,
        Kind: RetrySchedule.VerificationCodeKind,
        Required: true,
        IdempotencyKey: $"deliver-{Guid.CreateVersion7()}",
        ExpiresAt: null,
        CorrelationId: Guid.CreateVersion7());

    private async Task<Guid> EnqueueAsync(OutboundMessage message)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        scope.ServiceProvider.GetRequiredService<NotificationOutbox>().Enqueue(message);
        await database.SaveChangesAsync(Cancellation);
        return await database.Notifications
            .Where(row => row.IdempotencyKey == message.IdempotencyKey)
            .Select(row => row.Id)
            .SingleAsync(Cancellation);
    }

    private async Task<Notification> RowAsync(Guid id)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<MoniPayDbContext>().Notifications
            .AsNoTracking()
            .SingleAsync(row => row.Id == id, Cancellation);
    }

    private async Task UntilCalledAsync(string phone)
    {
        for (int i = 0; i < 400 && Api.Sms.CallsFor(phone).Count == 0; i++)
        {
            await Task.Delay(25, Cancellation);
        }

        Assert.NotEmpty(Api.Sms.CallsFor(phone));
    }
}
