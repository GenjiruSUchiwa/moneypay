namespace MoniPay.Notifications;

/// <summary>
/// One message a producing module asks the outbox to carry, with its text already localized by
/// the producer: the module composes its own copy, delivery is all this module does.
/// </summary>
/// <param name="Channel">The channel the message travels by.</param>
/// <param name="Recipient">The normalized phone or email.</param>
/// <param name="Subject">The email subject; <c>null</c> for an SMS.</param>
/// <param name="Body">The already localized text.</param>
/// <param name="Kind">A stable message kind for metrics and lookups, such as <c>VerificationCode</c>.</param>
/// <param name="Required">Whether exhausted retries raise an alert.</param>
/// <param name="IdempotencyKey">One delivery per business event; unique on the table.</param>
/// <param name="ExpiresAt">
/// After this time the message is useless and is marked <see cref="NotificationStatus.Expired"/>
/// instead of sent: a verification code that lapsed at 19:05 must not arrive at 19:07.
/// </param>
/// <param name="CorrelationId">The sign-up, user, or session identifier the message belongs to.</param>
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
    /// <summary>Hides the body from any accidental log of the record itself.</summary>
    public override string ToString() => $"{Kind} {Channel}";
}
