using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MoniPay.Notifications.Features.Deliver;

internal sealed class NotificationWorker(
    IServiceScopeFactory scopeFactory,
    DeliverySignal signal,
    IOptions<NotificationsOptions> options,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        WorkerOptions worker = options.Value.Worker;
        if (!worker.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<NotificationProcessor>()
                    .RunCycleAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                NotificationsLog.CycleFailed(logger, exception);
            }

            await signal.WaitAsync(worker.PollInterval, stoppingToken).ConfigureAwait(false);
        }
    }
}
