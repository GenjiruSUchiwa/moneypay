using MoniPay.Users.Providers;

namespace MoniPay.Tests.Fakes;

/// <summary>Fails every welcome enqueue, the way a renderer or sender error would.</summary>
public sealed class FailingWelcomeSender : IWelcomeMessageSender
{
    public Task EnqueueAsync(WelcomeMessage message, CancellationToken cancellationToken) =>
        throw new InvalidOperationException($"Forced failure for {message.Recipient.Value}.");
}
