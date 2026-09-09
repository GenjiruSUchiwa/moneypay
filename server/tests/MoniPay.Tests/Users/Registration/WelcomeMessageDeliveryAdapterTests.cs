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
using MoniPay.Users.Providers;
using Xunit;

namespace MoniPay.Tests.Users.Registration;

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
                candidate => candidate.CorrelationId == userId.Value
                    && candidate.Kind == WelcomeMessageDeliveryAdapter.WelcomeKind, Cancellation);
            Assert.Equal(NotificationChannel.Email, row.Channel);
            Assert.False(row.Required);
            Assert.Null(row.ExpiresAt);
            Assert.Equal(NotificationStatus.Pending, row.Status);
            Assert.Equal(message.IdempotencyKey, row.IdempotencyKey);
            Assert.Equal(recipient.Value, protector.Unprotect(row.RecipientCiphertext));
        }
        finally
        {
            await database.Notifications
                .Where(row => row.CorrelationId == userId.Value)
                .ExecuteDeleteAsync(Cancellation);
        }
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
