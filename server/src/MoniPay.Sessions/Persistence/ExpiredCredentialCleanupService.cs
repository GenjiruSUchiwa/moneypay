using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Persistence;

/// <summary>
/// Removes closed sign-ups and expired refresh tokens in bounded batches, one sweep per
/// <c>Cleanup:Interval</c>. Consumed digests remain until expiry for replay detection. It never
/// deletes a <c>sessions</c> row, and never an active session's newest token, however old:
/// that token is what a replay of the previous one is detected against.
/// </summary>
internal sealed class ExpiredCredentialCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<SessionsOptions> options,
    ILogger<ExpiredCredentialCleanupService> logger) : BackgroundService
{
    private const string DeleteSignUpsSql = $$"""
        DELETE FROM {{SessionsSchema.SignUpsTable}}
        WHERE id IN (
            SELECT id FROM {{SessionsSchema.SignUpsTable}}
            WHERE expires_at < {0} AND status <> {1}
            LIMIT {2})
        """;

    // ponytail: correlated "newest token" subquery per candidate; add a (session_id, created_at)
    // index if the sweep ever shows up in pg_stat_statements.
    private const string DeleteRefreshTokensSql = $$"""
        DELETE FROM {{SessionsSchema.RefreshTokensTable}}
        WHERE id IN (
            SELECT candidate.id FROM {{SessionsSchema.RefreshTokensTable}} AS candidate
            WHERE candidate.expires_at < {0}
              AND NOT EXISTS (
                  SELECT 1 FROM {{SessionsSchema.SessionsTable}} AS session
                  WHERE session.id = candidate.session_id
                    AND session.revoked_at IS NULL
                    AND NOT EXISTS (
                        SELECT 1 FROM {{SessionsSchema.RefreshTokensTable}} AS newer
                        WHERE newer.session_id = candidate.session_id
                          AND newer.created_at > candidate.created_at))
            LIMIT {1})
        """;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CleanupOptions cleanup = options.Value.Cleanup;
        if (!cleanup.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(cleanup.Interval, timeProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                SessionsLog.CleanupFailed(logger, exception);
            }
        }
    }

    /// <summary>
    /// One sweep in a fresh scope. The cutoff keeps a closed sign-up until it has left the
    /// per-phone start window: the start limit counts rows by <c>created_at</c>.
    /// </summary>
    internal async Task<int> RunCycleAsync(CancellationToken cancellationToken)
    {
        SessionsOptions current = options.Value;
        TimeSpan retention = current.SignUpLifetime > current.StartWindow ? current.SignUpLifetime : current.StartWindow;
        DateTimeOffset cutoff = timeProvider.GetUtcNow() - retention;

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        int total = 0;
        int deleted;
        do
        {
            deleted = await DeleteBatchAsync(database, cutoff, cancellationToken).ConfigureAwait(false);
            total += deleted;
        }
        while (deleted > 0);

        SessionsLog.CredentialsCleaned(logger, total, cutoff);
        return total;
    }

    /// <summary>Deletes at most one batch of each kind; returns how many rows went.</summary>
    internal async Task<int> DeleteBatchAsync(
        MoniPayDbContext database,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken)
    {
        int batchSize = options.Value.Cleanup.BatchSize;
        int signUps = await database.Database.ExecuteSqlRawAsync(
            DeleteSignUpsSql,
            [cutoff, nameof(SignUpStatus.Completed), batchSize],
            cancellationToken).ConfigureAwait(false);
        int tokens = await database.Database.ExecuteSqlRawAsync(
            DeleteRefreshTokensSql,
            [timeProvider.GetUtcNow(), batchSize],
            cancellationToken).ConfigureAwait(false);
        return signUps + tokens;
    }
}
