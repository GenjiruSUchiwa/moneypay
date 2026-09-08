using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel;
using MoniPay.Notifications;
using MoniPay.Notifications.Channels;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Providers;
using MoniPay.Tests.Fakes;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Delivery;

/// <summary>
/// Get-sign-up reports the latest verification notification or nothing.
/// </summary>
public sealed class VerificationCodeProjectionTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task Pending_reports_queued_and_sent_reports_sent()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);

        Assert.Equal(CodeDeliveryState.Queued, (await Api.GetSignUpAsync(started.SignUpId)).CodeDelivery);

        await Api.DeliveredCodeAsync(phone);

        Assert.Equal(CodeDeliveryState.Sent, (await Api.GetSignUpAsync(started.SignUpId)).CodeDelivery);
    }

    [Fact]
    public async Task A_permanent_rejection_reports_failed()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        Api.Sms.Result = new ChannelResult.Rejected("invalid-recipient");

        try
        {
            await Api.RunNotificationCycleAsync(Cancellation);
            Assert.Equal(CodeDeliveryState.Failed, (await Api.GetSignUpAsync(started.SignUpId)).CodeDelivery);
        }
        finally
        {
            Api.Sms.Result = new ChannelResult.Accepted("sms-ref");
        }
    }

    [Fact]
    public async Task An_unexpired_pending_row_does_not_expire_by_time_alone()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        Api.Time.Advance(CodeLifetime);

        Assert.Equal(CodeDeliveryState.Queued, (await Api.GetSignUpAsync(started.SignUpId)).CodeDelivery);

        await Api.RunNotificationCycleAsync(Cancellation);

        Assert.Equal(CodeDeliveryState.Expired, (await Api.GetSignUpAsync(started.SignUpId)).CodeDelivery);
        Assert.Empty(Api.Sms.CallsFor(phone.Value));
    }

    [Fact]
    public async Task A_sent_notification_stays_sent_after_the_code_expires()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        await Api.DeliveredCodeAsync(phone);
        Api.Time.Advance(CodeLifetime + TimeSpan.FromSeconds(1));

        await Api.RunNotificationCycleAsync(Cancellation);

        Assert.Equal(CodeDeliveryState.Sent, (await Api.GetSignUpAsync(started.SignUpId)).CodeDelivery);
    }

    [Fact]
    public async Task A_resend_reports_its_own_pending_delivery()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        await Api.DeliveredCodeAsync(phone);
        Api.Time.Advance(ResendCooldown);
        await Api.ResendCodeAsync(started.SignUpId);

        Assert.Equal(CodeDeliveryState.Queued, (await Api.GetSignUpAsync(started.SignUpId)).CodeDelivery);
    }

    [Fact]
    public async Task Other_sign_ups_and_kinds_are_ignored()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        NotificationOutbox outbox = scope.ServiceProvider.GetRequiredService<NotificationOutbox>();
        outbox.Enqueue(new OutboundMessage(
            NotificationChannel.Sms,
            phone.Value,
            null,
            "MoniPay: other sign-up.",
            RetrySchedule.VerificationCodeKind,
            true,
            $"verification-code:{Guid.CreateVersion7()}:0",
            Api.Time.GetUtcNow() + CodeLifetime,
            Guid.CreateVersion7()));
        outbox.Enqueue(new OutboundMessage(
            NotificationChannel.Email,
            "other@example.com",
            "Subject",
            "Welcome.",
            "Welcome",
            false,
            $"welcome:{started.SignUpId}:0",
            null,
            started.SignUpId.Value));
        await database.SaveChangesAsync(Cancellation);

        try
        {
            Assert.Equal(CodeDeliveryState.Queued, (await Api.GetSignUpAsync(started.SignUpId)).CodeDelivery);
        }
        finally
        {
            await database.Notifications.Where(notification => notification.CorrelationId != started.SignUpId.Value).ExecuteDeleteAsync(Cancellation);
            await database.Notifications.Where(notification => notification.Kind == "Welcome").ExecuteDeleteAsync(Cancellation);
        }
    }

    [Fact]
    public async Task Equal_timestamps_resolve_by_identifier()
    {
        SignUpId signUpId = SignUpId.New();
        DateTimeOffset now = Api.Time.GetUtcNow();
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        NotificationOutbox outbox = scope.ServiceProvider.GetRequiredService<NotificationOutbox>();
        OutboundMessage first = new(
            NotificationChannel.Sms,
            TestPhones.Next(),
            null,
            "MoniPay: first.",
            RetrySchedule.VerificationCodeKind,
            true,
            $"verification-code:{signUpId}:0",
            now + CodeLifetime,
            signUpId.Value);
        OutboundMessage second = first with { IdempotencyKey = $"verification-code:{signUpId}:1", Body = "MoniPay: second." };
        outbox.Enqueue(first);
        outbox.Enqueue(second);
        await database.SaveChangesAsync(Cancellation);

        List<Notification> rows = await database.Notifications
            .AsNoTracking()
            .Where(notification => notification.CorrelationId == signUpId.Value)
            .OrderBy(notification => notification.CreatedAt)
            .ThenBy(notification => notification.Id)
            .ToListAsync(Cancellation);
        Assert.Equal(2, rows.Count);
        Assert.Equal(rows[0].CreatedAt, rows[1].CreatedAt);
        Assert.NotEqual(rows[0].Id, rows[1].Id);

        try
        {
            NotificationStatus? latest = await outbox.FindLatestStatusAsync(signUpId.Value, RetrySchedule.VerificationCodeKind, Cancellation);
            Assert.Equal(NotificationStatus.Pending, latest);
        }
        finally
        {
            await database.Notifications.Where(notification => notification.CorrelationId == signUpId.Value).ExecuteDeleteAsync(Cancellation);
        }
    }
}
