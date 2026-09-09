using Microsoft.EntityFrameworkCore;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Persistence;
using MoniPay.Notifications.Security;
using MoniPay.Persistence;

namespace MoniPay.Notifications;

public sealed class NotificationOutbox
{
    private readonly MoniPayDbContext database;
    private readonly RecipientProtector protector;
    private readonly TimeProvider timeProvider;

    internal NotificationOutbox(
        MoniPayDbContext database,
        RecipientProtector protector,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(protector);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.database = database;
        this.protector = protector;
        this.timeProvider = timeProvider;
    }

    public void Enqueue(OutboundMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Body);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.IdempotencyKey);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(message.Kind.Length, NotificationsSchema.KindMaxLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            message.IdempotencyKey.Length, NotificationsSchema.IdempotencyKeyMaxLength);

        if (!Enum.IsDefined(message.Channel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(message), message.Channel, "The notification channel is not defined.");
        }

        if (message.CorrelationId == default)
        {
            throw new ArgumentException(
                "The correlation identifier must be set.", nameof(message));
        }

        database.Notifications.Add(Notification.Pending(
            message,
            recipientCiphertext: protector.Protect(message.Recipient),
            recipientHint: RecipientProtector.Hint(message.Recipient),
            subjectCiphertext: message.Subject is { } subject ? protector.Protect(subject) : null,
            bodyCiphertext: protector.Protect(message.Body),
            now: timeProvider.GetUtcNow()));
    }

    public async Task<NotificationStatus?> FindLatestStatusAsync(
        Guid correlationId,
        string kind,
        CancellationToken cancellationToken)
    {
        return await database.Notifications
            .AsNoTracking()
            .Where(notification => notification.CorrelationId == correlationId && notification.Kind == kind)
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Select(notification => (NotificationStatus?)notification.Status)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
