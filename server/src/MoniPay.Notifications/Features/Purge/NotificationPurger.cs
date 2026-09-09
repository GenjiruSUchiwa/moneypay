using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;

namespace MoniPay.Notifications.Features.Purge;

internal sealed class NotificationPurger(
    MoniPayDbContext database,
    IOptions<NotificationsOptions> options,
    TimeProvider timeProvider,
    ILogger<NotificationPurger> logger)
{
    private const string DeleteSql = $$"""
        DELETE FROM {{NotificationsSchema.NotificationsTable}}
        WHERE id IN (
            SELECT id FROM {{NotificationsSchema.NotificationsTable}}
            WHERE status IN ({1}, {2}, {3})
              AND created_at < {0}
            LIMIT {4})
        """;

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = timeProvider.GetUtcNow() - options.Value.Retention;
        int batchSize = options.Value.Worker.BatchSize;
        int total = 0;
        int deleted;
        do
        {
            deleted = await database.Database.ExecuteSqlRawAsync(
                DeleteSql,
                [cutoff, nameof(NotificationStatus.Sent), nameof(NotificationStatus.Failed), nameof(NotificationStatus.Expired), batchSize],
                cancellationToken).ConfigureAwait(false);
            total += deleted;
        }
        while (deleted == batchSize);

        NotificationsLog.Purged(logger, total, cutoff);
        return total;
    }
}
