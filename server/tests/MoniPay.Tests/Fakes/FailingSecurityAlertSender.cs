using MoniPay.Sessions.Providers;

namespace MoniPay.Tests.Fakes;

public sealed class FailingSecurityAlertSender : ISecurityAlertSender
{
    public Task EnqueueAsync(SecurityAlert alert, CancellationToken cancellationToken) =>
        throw new InvalidOperationException($"Forced failure for {alert.Kind}.");
}
