using MoniPay.Notifications;
using MoniPay.Users.Providers;

namespace MoniPay.Api.Composition;

/// <summary>
/// Maps the Users welcome port onto the Notifications outbox. It carries the already rendered
/// text, forwards the caller's cancellation, and never saves, commits or resolves a provider:
/// the handler's own save commits the welcome in the caller's ambient transaction.
/// </summary>
internal sealed class WelcomeMessageDeliveryAdapter(NotificationOutbox outbox) : IWelcomeMessageSender
{
    /// <summary>The stable kind; its value selects the optional message's retry schedule.</summary>
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
