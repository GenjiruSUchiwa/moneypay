using Microsoft.Extensions.Localization;
using MoniPay.Kernel;
using MoniPay.Notifications;
using MoniPay.Sessions;
using MoniPay.Sessions.Providers;
using MoniPay.Users.Features.Contact;

namespace MoniPay.Api.Composition;

internal sealed class SecurityAlertDeliveryAdapter(
    UserContactLookup contacts,
    NotificationOutbox outbox,
    IStringLocalizer<SessionMessages> messages) : ISecurityAlertSender
{
    internal const string SessionRevokedKind = "SessionRevoked";

    internal const string RefreshTokenReuseDetectedKind = "RefreshTokenReuseDetected";

    private static readonly TimeZoneInfo RecipientTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Douala");

    public async Task EnqueueAsync(SecurityAlert alert, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alert);
        cancellationToken.ThrowIfCancellationRequested();

        UserContact? contact = await contacts
            .FindAsync(alert.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (contact is null)
        {
            return;
        }

        DeliveryPlan plan = DeliveryPlan.For(alert.Kind);
        if (plan.SmsBodyKey is { } smsKey)
        {
            outbox.Enqueue(new OutboundMessage(
                NotificationChannel.Sms,
                contact.Phone.Value,
                null,
                Render(contact.Locale, smsKey, alert.OccurredAt),
                plan.Kind,
                plan.Required,
                FormattableString.Invariant($"{alert.IdempotencyKey}:sms"),
                null,
                alert.UserId.Value));
        }

        if (plan is { EmailSubjectKey: { } subjectKey, EmailBodyKey: { } bodyKey })
        {
            string subject = Render(contact.Locale, subjectKey, alert.OccurredAt);
            string body = Render(contact.Locale, bodyKey, alert.OccurredAt);
            outbox.Enqueue(new OutboundMessage(
                NotificationChannel.Email,
                contact.Email.Value,
                subject,
                body,
                plan.Kind,
                plan.Required,
                FormattableString.Invariant($"{alert.IdempotencyKey}:email"),
                null,
                alert.UserId.Value));
        }
    }

    private string Render(Locale locale, string key, DateTimeOffset occurredAt)
    {
        DateTimeOffset local = TimeZoneInfo.ConvertTime(occurredAt, RecipientTimeZone);
        return locale.Invoke(() => messages[key, local.ToString("g")].Value);
    }

    private sealed record DeliveryPlan(
        string Kind,
        bool Required,
        string? SmsBodyKey,
        string? EmailSubjectKey,
        string? EmailBodyKey)
    {
        public static DeliveryPlan For(SecurityAlertKind kind) => kind switch
        {
            SecurityAlertKind.SessionRevoked => new(
                SessionRevokedKind,
                false,
                null,
                SessionMessageKeys.SessionRevokedEmailSubject,
                SessionMessageKeys.SessionRevokedEmailBody),
            SecurityAlertKind.RefreshTokenReuseDetected => new(
                RefreshTokenReuseDetectedKind,
                true,
                SessionMessageKeys.RefreshTokenReuseDetectedSms,
                SessionMessageKeys.RefreshTokenReuseDetectedEmailSubject,
                SessionMessageKeys.RefreshTokenReuseDetectedEmailBody),
            _ => throw new NotSupportedException($"No delivery is defined for security alert {kind}."),
        };
    }
}
