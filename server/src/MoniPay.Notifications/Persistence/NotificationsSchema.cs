using MoniPay.Notifications.Domain;

namespace MoniPay.Notifications.Persistence;

/// <summary>
/// The names PostgreSQL knows this module's table, keys and indexes by, and the column lengths
/// the enqueue contract mirrors. Each name is a constant here rather than a literal at the
/// mapping site: a renamed index breaks the build instead of a running client.
/// </summary>
internal static class NotificationsSchema
{
    public const string NotificationsTable = "notifications";

    public const string NotificationsPrimaryKey = "pk_notifications";

    /// <summary>The worker's claim scan: pending rows by earliest eligible attempt.</summary>
    public const string ClaimIndex = "ix_notifications_status_next_attempt_at";

    /// <summary>One delivery per business event.</summary>
    public const string IdempotencyKeyUnique = "ix_notifications_idempotency_key";

    /// <summary>The support lookup behind <c>FindLatestStatusAsync</c>.</summary>
    public const string CorrelationIdIndex = "ix_notifications_correlation_id";

    /// <summary>The longest <see cref="Notification.Kind"/> the table accepts.</summary>
    public const int KindMaxLength = 48;

    /// <summary>The longest <see cref="Notification.IdempotencyKey"/> the table accepts.</summary>
    public const int IdempotencyKeyMaxLength = 128;
}
