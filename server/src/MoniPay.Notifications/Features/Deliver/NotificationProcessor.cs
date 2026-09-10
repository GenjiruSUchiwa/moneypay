using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Notifications.Channels;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Persistence;
using MoniPay.Notifications.Security;
using MoniPay.Persistence;

namespace MoniPay.Notifications.Features.Deliver;

internal sealed class NotificationProcessor(
    MoniPayDbContext database,
    IServiceProvider services,
    RecipientProtector protector,
    IOptions<NotificationsOptions> options,
    TimeProvider timeProvider,
    ILogger<NotificationProcessor> logger)
{
    public const string ChannelNotConfigured = "channel-not-configured";
    public const string BodyMissing = "body-missing";

    private const string ClaimSql = $$"""
        WITH claimed AS (
            UPDATE {{NotificationsSchema.NotificationsTable}}
            SET lease_until = {1}
            WHERE id IN (
                SELECT id FROM {{NotificationsSchema.NotificationsTable}}
                WHERE status = {2}
                  AND next_attempt_at <= {0}
                  AND (lease_until IS NULL OR lease_until < {0})
                ORDER BY next_attempt_at
                LIMIT {3}
                FOR UPDATE SKIP LOCKED)
            RETURNING *)
        SELECT * FROM claimed
        """;

    private bool channelWarningLogged;

    public async Task<int> RunCycleAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        List<Notification> claimed = await ClaimAsync(now, cancellationToken).ConfigureAwait(false);

        foreach (Notification notification in claimed)
        {
            await DeliverAsync(notification, cancellationToken).ConfigureAwait(false);
            await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return claimed.Count;
    }

    private async Task<List<Notification>> ClaimAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        WorkerOptions worker = options.Value.Worker;
        await using IDbContextTransaction transaction = await database.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        List<Notification> claimed = await database.Notifications
            .FromSqlRaw(ClaimSql, now, now + worker.LeaseDuration, nameof(NotificationStatus.Pending), worker.BatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return claimed;
    }

    private async Task DeliverAsync(Notification notification, CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        if (notification.ExpiresAt is { } expiresAt && expiresAt <= now)
        {
            notification.Expire();
            NotificationsLog.Expired(logger, notification.Id, notification.Kind, notification.Channel, notification.CorrelationId, notification.RecipientHint);
            return;
        }

        INotificationChannel? channel = services.GetKeyedService<INotificationChannel>(notification.Channel);
        if (channel is null)
        {
            if (!channelWarningLogged)
            {
                channelWarningLogged = true;
                NotificationsLog.ChannelNotConfigured(logger, notification.Channel);
            }

            notification.Skip(ChannelNotConfigured);
            return;
        }

        ChannelResult result = await SendAsync(channel, notification, cancellationToken).ConfigureAwait(false);
        Record(notification, result, timeProvider.GetUtcNow());
    }

    private async Task<ChannelResult> SendAsync(INotificationChannel channel, Notification notification, CancellationToken cycleToken)
    {
        if (notification.BodyCiphertext is not { } bodyCiphertext)
        {
            return new ChannelResult.Rejected(BodyMissing);
        }

        string recipient = protector.Unprotect(notification.RecipientCiphertext);
        string body = protector.Unprotect(bodyCiphertext);
        string? subject = notification.SubjectCiphertext is { } subjectCiphertext ? protector.Unprotect(subjectCiphertext) : null;

        using CancellationTokenSource timeout = new(options.Value.ProviderTimeout, timeProvider);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cycleToken, timeout.Token);
        try
        {
            return await channel.SendAsync(notification.Id, recipient, subject, body, notification.IdempotencyKey, linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cycleToken.IsCancellationRequested)
        {
            return new ChannelResult.Retry(ChannelResult.ProviderTimeout);
        }
    }

    private void Record(Notification notification, ChannelResult result, DateTimeOffset now)
    {
        switch (result)
        {
            case ChannelResult.Accepted accepted:
                notification.MarkSent(accepted.ProviderReference, now);
                NotificationsLog.Sent(logger, notification.Id, notification.Kind, notification.Channel, notification.CorrelationId, notification.RecipientHint, notification.Attempts);
                break;

            case ChannelResult.Rejected rejected:
                notification.Fail(rejected.Code);
                Failed(notification, rejected.Code);
                break;

            case ChannelResult.Retry retry when !notification.RecordRetry(retry.Code, now):
                Failed(notification, retry.Code);
                break;

            case ChannelResult.Retry retry:
                NotificationsLog.Retried(logger, notification.Id, notification.Kind, notification.Channel, notification.CorrelationId, notification.RecipientHint, notification.Attempts, retry.Code);
                break;

            default:
                throw new InvalidOperationException($"Unknown channel result {result}.");
        }
    }

    private void Failed(Notification notification, string code)
    {
        NotificationsLog.Failed(logger, notification.Id, notification.Kind, notification.Channel, notification.CorrelationId, notification.RecipientHint, notification.Attempts, code);
        if (notification.Required)
        {
            NotificationsLog.RequiredFailed(logger, notification.Id, notification.Kind, notification.Channel, notification.CorrelationId, notification.RecipientHint, code);
        }
    }
}
