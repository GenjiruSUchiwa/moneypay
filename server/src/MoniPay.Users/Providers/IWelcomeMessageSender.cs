namespace MoniPay.Users.Providers;

public interface IWelcomeMessageSender
{
    Task EnqueueAsync(WelcomeMessage message, CancellationToken cancellationToken);
}
