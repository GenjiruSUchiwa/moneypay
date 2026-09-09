using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MoniPay.Notifications;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Notifications.Purge;

public sealed class DeliveredNotificationPurgeTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task Terminal_rows_older_than_retention_are_deleted_across_batches_and_pending_rows_survive()
    {
        NotificationsOptions options = Api.Services.GetRequiredService<IOptions<NotificationsOptions>>().Value;
        int batchSize = options.Worker.BatchSize;
        Guid correlationId = Guid.CreateVersion7();
        DateTimeOffset old = Api.Time.GetUtcNow() - options.Retention - TimeSpan.FromDays(1);
        await Api.QueryAsync("DELETE FROM notifications;", reader => 0);

        await using (AsyncServiceScope scope = Api.Services.CreateAsyncScope())
        {
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            NotificationOutbox outbox = scope.ServiceProvider.GetRequiredService<NotificationOutbox>();
            NotificationStatus[] terminal = [NotificationStatus.Sent, NotificationStatus.Failed, NotificationStatus.Expired];
            for (int i = 0; i < batchSize + 2; i++)
            {
                outbox.Enqueue(Message($"purge-old-{i}-{correlationId}", correlationId));
            }

            outbox.Enqueue(Message($"purge-old-pending-{correlationId}", correlationId));
            outbox.Enqueue(Message($"purge-recent-sent-{correlationId}", correlationId));
            await database.SaveChangesAsync(Cancellation);

            for (int i = 0; i < batchSize + 2; i++)
            {
                NotificationStatus status = terminal[i % terminal.Length];
                await database.Notifications
                    .Where(row => row.IdempotencyKey == $"purge-old-{i}-{correlationId}")
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(row => row.Status, status)
                        .SetProperty(row => row.CreatedAt, old), Cancellation);
            }

            await database.Notifications
                .Where(row => row.IdempotencyKey == $"purge-old-pending-{correlationId}")
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.CreatedAt, old), Cancellation);
            await database.Notifications
                .Where(row => row.IdempotencyKey == $"purge-recent-sent-{correlationId}")
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, NotificationStatus.Sent), Cancellation);
        }

        Assert.Equal(batchSize + 2, await Api.RunNotificationPurgeAsync(Cancellation));
        Assert.Equal(0, await Api.RunNotificationPurgeAsync(Cancellation));

        await using AsyncServiceScope check = Api.Services.CreateAsyncScope();
        MoniPayDbContext reader = check.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        string[] remaining = await reader.Notifications.AsNoTracking()
            .Where(row => row.CorrelationId == correlationId)
            .Select(row => row.IdempotencyKey)
            .ToArrayAsync(Cancellation);
        Assert.Equal(
            new[] { $"purge-old-pending-{correlationId}", $"purge-recent-sent-{correlationId}" }.Order(),
            remaining.Order());
    }

    private static OutboundMessage Message(string idempotencyKey, Guid correlationId) => new(
        Channel: NotificationChannel.Sms,
        Recipient: "+237670123456",
        Subject: null,
        Body: "Your MoniPay code is 482913, valid 5 minutes.",
        Kind: "Welcome",
        Required: false,
        IdempotencyKey: idempotencyKey,
        ExpiresAt: null,
        CorrelationId: correlationId);
}
