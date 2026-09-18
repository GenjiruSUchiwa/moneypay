namespace MoniPay.Sessions.Providers;

public interface ISecurityAlertSender
{
    Task EnqueueAsync(SecurityAlert alert, CancellationToken cancellationToken);
}
