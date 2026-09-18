using MoniPay.Notifications;
using MoniPay.Sessions.Providers;

namespace MoniPay.Tests.Fakes;

public sealed class DuplicateKeySecurityAlertSender(NotificationOutbox outbox) : ISecurityAlertSender
{
    public Task EnqueueAsync(SecurityAlert alert, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alert);

        for (int copy = 0; copy < 2; copy++)
        {
            outbox.Enqueue(new OutboundMessage(
                NotificationChannel.Sms,
                "+237600000000",
                null,
                "duplicate",
                nameof(DuplicateKeySecurityAlertSender),
                true,
                alert.IdempotencyKey,
                null,
                alert.UserId.Value));
        }

        return Task.CompletedTask;
    }
}
