namespace MoniPay.Notifications;

public sealed record OutboundMessage(
    NotificationChannel Channel,
    string Recipient,
    string? Subject,
    string Body,
    string Kind,
    bool Required,
    string IdempotencyKey,
    DateTimeOffset? ExpiresAt,
    Guid CorrelationId)
{
    public override string ToString() => $"{Kind} {Channel}";
}
