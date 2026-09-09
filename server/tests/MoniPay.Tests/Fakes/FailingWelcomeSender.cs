using MoniPay.Users.Providers;

namespace MoniPay.Tests.Fakes;

public sealed class FailingWelcomeSender : IWelcomeMessageSender
{
    public Task EnqueueAsync(WelcomeMessage message, CancellationToken cancellationToken) =>
        throw new InvalidOperationException($"Forced failure for {message.Recipient.Value}.");
}
