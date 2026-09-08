using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
    /// </summary>
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

    /// <summary>
    /// Adds a <see cref="NotificationStatus.Pending"/> row for <paramref name="message"/>,
    /// validating the whole contract first — so a producer error is an argument exception at
    /// the call site, never a database error at its save — and encrypting the recipient, the
    /// subject and the body with the module's data key. Does not save: the producer's
    /// <c>SaveChangesAsync</c> commits the notification alongside its domain change.
    /// </summary>
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

    /// <summary>
    /// Detaches a row <see cref="Enqueue"/> staged that the producer will not commit — a
    /// registration that lost a race after enqueuing. Only the <c>Added</c> row with
    /// <paramref name="idempotencyKey"/> is removed; the shared change tracker is otherwise
    /// untouched, and a no-op when the row was never staged or is already saved.
    /// </summary>
    public void Discard(string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        foreach (EntityEntry<Notification> staged in database.ChangeTracker.Entries<Notification>().ToArray())
        {
            if (staged.State == EntityState.Added && staged.Entity.IdempotencyKey == idempotencyKey)
            {
                staged.State = EntityState.Detached;
            }
        }
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
