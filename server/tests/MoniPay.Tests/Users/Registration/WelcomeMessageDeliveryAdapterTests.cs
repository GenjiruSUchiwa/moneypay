using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Api.Composition;
using MoniPay.Kernel;
using MoniPay.Notifications;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Persistence;
using MoniPay.Notifications.Security;
using MoniPay.Persistence;
using MoniPay.Tests.Support;
using MoniPay.Users.Ports;
using Xunit;

namespace MoniPay.Tests.Users.Registration;

/// <summary>
/// The host adapter maps the Users welcome onto the Notifications outbox: email, optional, no
/// expiry, the user as correlation, and the normalized recipient — without saving or sending.
/// </summary>
public sealed class WelcomeMessageDeliveryAdapterTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task Enqueue_maps_the_welcome_without_saving()
    {
        UserId userId = UserId.New();
        EmailAddress recipient = new("marie.ngo@example.com");
        WelcomeMessage message = new(
            userId,
            recipient,
            "Bienvenue sur MoniPay",
            "Bonjour Marie, votre compte MoniPay est prêt.",
            $"welcome:{userId}");

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        WelcomeMessageDeliveryAdapter adapter = (WelcomeMessageDeliveryAdapter)
            scope.ServiceProvider.GetRequiredService<IWelcomeMessageSender>();

        await adapter.EnqueueAsync(message, Cancellation);

        await using (AsyncServiceScope verificationScope = Api.Services.CreateAsyncScope())
        {
            MoniPayDbContext persisted = verificationScope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.False(await persisted.Notifications.AnyAsync(
                row => row.CorrelationId == userId.Value, Cancellation));
        }

        await database.SaveChangesAsync(Cancellation);

        try
        {
            RecipientProtector protector = Api.Services.GetRequiredService<RecipientProtector>();
            Notification row = await database.Notifications.SingleAsync(
                candidate => candidate.CorrelationId == userId.Value, Cancellation);
            Assert.Equal(NotificationChannel.Email, row.Channel);
            Assert.Equal(WelcomeMessageDeliveryAdapter.WelcomeKind, row.Kind);
            Assert.False(row.Required);
            Assert.Null(row.ExpiresAt);
            Assert.Equal(NotificationStatus.Pending, row.Status);
            Assert.Equal(message.IdempotencyKey, row.IdempotencyKey);
            Assert.Equal(recipient.Value, protector.Unprotect(row.RecipientCiphertext));
            Assert.Equal(message.Subject, protector.Unprotect(Assert.IsType<Ciphertext>(row.SubjectCiphertext)));
            Assert.Equal(message.Body, protector.Unprotect(Assert.IsType<Ciphertext>(row.BodyCiphertext)));
        }
        finally
        {
            await database.Notifications
                .Where(row => row.CorrelationId == userId.Value)
                .ExecuteDeleteAsync(Cancellation);
        }
    }

    [Fact]
    public async Task Discard_detaches_a_staged_welcome()
    {
        UserId userId = UserId.New();
        WelcomeMessage message = new(
            userId,
            new EmailAddress("marie.ngo@example.com"),
            "Bienvenue sur MoniPay",
            "Bonjour Marie.",
            $"welcome:{userId}");

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        WelcomeMessageDeliveryAdapter adapter = (WelcomeMessageDeliveryAdapter)
            scope.ServiceProvider.GetRequiredService<IWelcomeMessageSender>();

        await adapter.EnqueueAsync(message, Cancellation);
        adapter.Discard(message);
        await database.SaveChangesAsync(Cancellation);

        Assert.False(await database.Notifications.AnyAsync(row => row.CorrelationId == userId.Value, Cancellation));
    }

    [Fact]
    public async Task Cancellation_stays_cancellation()
    {
        WelcomeMessage message = new(
            UserId.New(),
            new EmailAddress("marie.ngo@example.com"),
            "Bienvenue sur MoniPay",
            "Bonjour Marie.",
            $"welcome:{Guid.NewGuid()}");

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        WelcomeMessageDeliveryAdapter adapter = (WelcomeMessageDeliveryAdapter)
            scope.ServiceProvider.GetRequiredService<IWelcomeMessageSender>();
        using CancellationTokenSource canceled = new();
        await canceled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            adapter.EnqueueAsync(message, canceled.Token));
    }
}
