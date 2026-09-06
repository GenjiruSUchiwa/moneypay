using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MoniPay.Notifications.Features.Deliver;

/// <summary>
/// The delivery loop: wakes on the commit signal or after <c>PollInterval</c>, runs one cycle in
/// a fresh scope, and keeps going through its own failures. A cycle already running finishes its
/// batch on shutdown — the stopping token gates the loop and the wait, not the batch.
/// </summary>
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
