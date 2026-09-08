using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Api.Composition;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Notifications;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using MoniPay.Sessions.Providers;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Delivery;

/// <summary>
/// The host adapter carries the rendered message and reports the latest status.
/// </summary>
public sealed class VerificationCodeDeliveryAdapterTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task Enqueue_maps_the_message_without_saving()
    {
        SignUpId signUpId = SignUpId.New();
        PhoneNumber recipient = new(TestPhones.Next());
        VerificationCodeMessage message = new(
            signUpId,
            recipient,
            "001234",
            TimeSpan.FromMinutes(2),
            Locale.FrenchCameroon,
            "MoniPay : votre code de vérification est 001234. Il expire dans 2 minutes.",
            Api.Time.GetUtcNow() + TimeSpan.FromMinutes(2),
            $"verification-code:{signUpId}:0");

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        VerificationCodeDeliveryAdapter adapter = (VerificationCodeDeliveryAdapter)scope.ServiceProvider.GetRequiredService<IVerificationCodeSender>();

        await adapter.EnqueueAsync(message, Cancellation);
        await database.SaveChangesAsync(Cancellation);

        try
        {
            Notification row = await database.Notifications.SingleAsync(
                notification => notification.CorrelationId == signUpId.Value, Cancellation);
            Assert.Equal(NotificationChannel.Sms, row.Channel);
            Assert.Equal(RetrySchedule.VerificationCodeKind, row.Kind);
            Assert.True(row.Required);
            Assert.Equal(message.ExpiresAt, row.ExpiresAt);
            Assert.Equal(message.IdempotencyKey, row.IdempotencyKey);
            Assert.Equal(NotificationStatus.Pending, row.Status);
        }
        finally
        {
            await database.Notifications.Where(notification => notification.CorrelationId == signUpId.Value).ExecuteDeleteAsync(Cancellation);
        }
    }

    [Fact]
    public async Task Enqueue_failures_become_unavailable_without_plaintext()
    {
        SignUpId signUpId = SignUpId.New();
        VerificationCodeMessage message = new(
            signUpId,
            new PhoneNumber(TestPhones.Next()),
            "001234",
            TimeSpan.FromMinutes(2),
            Locale.FrenchCameroon,
            "",
            Api.Time.GetUtcNow() + TimeSpan.FromMinutes(2),
            $"verification-code:{signUpId}:0");

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        VerificationCodeDeliveryAdapter adapter = (VerificationCodeDeliveryAdapter)scope.ServiceProvider.GetRequiredService<IVerificationCodeSender>();

        ProviderUnavailableException failure = await Assert.ThrowsAsync<ProviderUnavailableException>(() =>
            adapter.EnqueueAsync(message, Cancellation));

        Assert.NotNull(failure.InnerException);
        Assert.DoesNotContain("001234", failure.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancellation_stays_cancellation()
    {
        SignUpId signUpId = SignUpId.New();
        VerificationCodeMessage message = new(
            signUpId,
            new PhoneNumber(TestPhones.Next()),
            "001234",
            TimeSpan.FromMinutes(2),
            Locale.FrenchCameroon,
            "MoniPay : body.",
            Api.Time.GetUtcNow() + TimeSpan.FromMinutes(2),
            $"verification-code:{signUpId}:0");

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        VerificationCodeDeliveryAdapter adapter = (VerificationCodeDeliveryAdapter)scope.ServiceProvider.GetRequiredService<IVerificationCodeSender>();
        using CancellationTokenSource canceled = new();
        await canceled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            adapter.EnqueueAsync(message, canceled.Token));
    }

    [Fact]
    public async Task A_missing_row_reports_no_delivery()
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        VerificationCodeDeliveryAdapter adapter = (VerificationCodeDeliveryAdapter)scope.ServiceProvider.GetRequiredService<IVerificationCodeSender>();

        Assert.Null(await adapter.GetLatestDeliveryAsync(SignUpId.New(), Cancellation));
    }

    [Fact]
    public async Task A_duplicate_key_fails_on_save()
    {
        SignUpId signUpId = SignUpId.New();
        string phone = TestPhones.Next();
        DateTimeOffset expiresAt = Api.Time.GetUtcNow() + TimeSpan.FromMinutes(2);
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        VerificationCodeDeliveryAdapter adapter = (VerificationCodeDeliveryAdapter)scope.ServiceProvider.GetRequiredService<IVerificationCodeSender>();
        VerificationCodeMessage message = new(
            signUpId,
            new PhoneNumber(phone),
            "001234",
            TimeSpan.FromMinutes(2),
            Locale.FrenchCameroon,
            "MoniPay : body.",
            expiresAt,
            $"verification-code:{signUpId}:0");

        await adapter.EnqueueAsync(message, Cancellation);
        await adapter.EnqueueAsync(message, Cancellation);

        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync(Cancellation));
    }
}
