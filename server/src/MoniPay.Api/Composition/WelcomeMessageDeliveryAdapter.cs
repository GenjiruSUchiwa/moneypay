using MoniPay.Notifications;
using MoniPay.Users.Providers;

namespace MoniPay.Api.Composition;

internal sealed class WelcomeMessageDeliveryAdapter(NotificationOutbox outbox) : IWelcomeMessageSender
{
    internal const string WelcomeKind = "Welcome";

    public Task EnqueueAsync(WelcomeMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        outbox.Enqueue(new OutboundMessage(
            NotificationChannel.Email,
            message.Recipient.Value,
            message.Subject,
            message.Body,
            WelcomeKind,
            false,
            message.IdempotencyKey,
            null,
            message.UserId.Value));

        return Task.CompletedTask;
    }
}
