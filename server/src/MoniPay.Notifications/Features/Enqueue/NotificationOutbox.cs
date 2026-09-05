using Microsoft.EntityFrameworkCore;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Persistence;
using MoniPay.Notifications.Security;
using MoniPay.Persistence;

namespace MoniPay.Notifications;

/// <summary>
/// The outbox a producing module enqueues into. Adding a row is part of the producer's own
/// transaction: <see cref="Enqueue"/> only adds to the caller's <c>MoniPayDbContext</c> and
/// never saves, so the domain change and its notification commit together or not at all. The
/// worker is woken after the commit by the module's interceptors, never from here.
/// </summary>
public sealed class NotificationOutbox
{
    private readonly MoniPayDbContext database;
    private readonly RecipientProtector protector;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Internal because its parameters are the module's internals: the host resolves this
    /// class, it never constructs one, and the public surface stays the five documented types.
    /// The signal is the commit nudge the module's interceptors raise; it is part of this
    /// composition so the outbox and its worker share one wake-up path.
    /// </summary>
    internal NotificationOutbox(
        MoniPayDbContext database,
        RecipientProtector protector,
        DeliverySignal signal,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(protector);
        ArgumentNullException.ThrowIfNull(signal);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.database = database;
        this.protector = protector;
        this.timeProvider = timeProvider;
    }

    /// <summary>
    /// Adds a <see cref="NotificationStatus.Pending"/> row for <paramref name="message"/>,
    /// encrypting the recipient, the subject and the body with the module's data key. Does not
    /// save: the producer's <c>SaveChangesAsync</c> commits the notification alongside its
    /// domain change.
    /// </summary>
    public void Enqueue(OutboundMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.IdempotencyKey);

        database.Notifications.Add(Notification.Pending(
            message,
            recipientCiphertext: protector.Protect(message.Recipient),
            recipientHint: RecipientProtector.Hint(message.Recipient),
            subjectCiphertext: message.Subject is { } subject ? protector.Protect(subject) : null,
            bodyCiphertext: protector.Protect(message.Body),
            now: timeProvider.GetUtcNow()));
    }

    /// <summary>
    /// The delivery state of the newest message queued for a correlation and kind — the read
    /// behind the <c>codeDelivery</c> attribute. <c>null</c> when nothing was ever enqueued.
    /// </summary>
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
