using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MoniPay.Notifications.Features.Purge;

/// <summary>Runs one <see cref="NotificationPurger"/> sweep an hour, gated with the delivery worker.</summary>
internal sealed class NotificationPurgeWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationsOptions> options,
    TimeProvider timeProvider,
    ILogger<NotificationPurgeWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Worker.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<NotificationPurger>().RunAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(Period, timeProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                NotificationsLog.PurgeFailed(logger, exception);
            }
        }
    }
}
