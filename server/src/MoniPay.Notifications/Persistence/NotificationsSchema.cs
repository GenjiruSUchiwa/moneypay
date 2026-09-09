using MoniPay.Notifications.Domain;

namespace MoniPay.Notifications.Persistence;

internal static class NotificationsSchema
{
    public const string NotificationsTable = "notifications";

    public const string NotificationsPrimaryKey = "pk_notifications";

    public const string ClaimIndex = "ix_notifications_status_next_attempt_at";

    public const string IdempotencyKeyUnique = "ix_notifications_idempotency_key";

    public const string CorrelationIdIndex = "ix_notifications_correlation_id";

    public const int KindMaxLength = 48;

    public const int IdempotencyKeyMaxLength = 128;
}
