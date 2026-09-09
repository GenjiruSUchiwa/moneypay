using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Sessions.Persistence;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions.Domain;

internal sealed class SessionTokenService(
    MoniPayDbContext database,
    AccessTokenIssuer accessTokens,
    RefreshTokenFactory refreshTokens,
    TimeProvider timeProvider,
    IOptions<SessionsOptions> options,
    ILogger<SessionTokenService> logger)
{
    private readonly SessionsOptions settings = options.Value;

    public async Task<SessionTokenResult> CreateAsync(
        UserId userId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        Session session = Session.Create(userId, deviceId, now);
        (string rawRefresh, byte[] digest) = refreshTokens.Create();

        database.Sessions.Add(session);
        database.RefreshTokens.Add(RefreshToken.Issue(session.Id, digest, now, settings.RefreshTokenLifetime));
        await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result(session, userId, rawRefresh, now);
    }

    public async Task<SessionTokenResult> ReplaceBootstrapAsync(
        UserId userId,
        Guid priorSessionId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction = database.Database.CurrentTransaction is null
            ? await database.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : null;
        Session prior = await database.Sessions
            .FromSqlRaw(LockSessionSql, priorSessionId)
            .SingleAsync(session => session.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        prior.Revoke(SessionRevokeReason.BootstrapReplaced, timeProvider.GetUtcNow());

        SessionTokenResult result = await CreateAsync(userId, deviceId, cancellationToken).ConfigureAwait(false);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<SessionTokenResult> RefreshAsync(
        string refreshToken,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);
        DateTimeOffset now = timeProvider.GetUtcNow();

        await using IDbContextTransaction transaction = await database.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        RefreshToken? token = await LockTokenAsync(refreshTokens.Digest(refreshToken), cancellationToken)
            .ConfigureAwait(false);
        if (token is null || token.IsExpired(now))
        {
            await RefuseAsync("unknown-or-expired", transaction, cancellationToken).ConfigureAwait(false);
            throw new RefusalException(MoniPayErrorTypes.SessionInvalid);
        }

        Session session = await LockSessionAsync(token.SessionId, cancellationToken).ConfigureAwait(false);
        if (token.IsConsumed)
        {
            await RevokeFamilyAsync(session.TokenFamilyId, now, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            SessionsLog.FamilyRevoked(logger, session.TokenFamilyId);
            throw new RefusalException(MoniPayErrorTypes.RefreshTokenReused);
        }

        if (!session.IsActive || session.DeviceId != deviceId)
        {
            await RefuseAsync("revoked-or-foreign-device", transaction, cancellationToken).ConfigureAwait(false);
            throw new RefusalException(MoniPayErrorTypes.SessionInvalid);
        }

        (string rawRefresh, byte[] digest) = refreshTokens.Create();
        RefreshToken replacement = RefreshToken.Issue(session.Id, digest, now, settings.RefreshTokenLifetime);
        token.Consume(replacement.Id, now);
        database.RefreshTokens.Add(replacement);
        session.Touch(now);
        await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result(session, session.UserId, rawRefresh, now);
    }

    public async Task RevokeAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction = database.Database.CurrentTransaction is null
            ? await database.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : null;
        Session? session = await database.Sessions
            .FromSqlRaw(LockSessionSql, sessionId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (session is null)
        {
            return;
        }

        session.Revoke(SessionRevokeReason.UserRequest, timeProvider.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RefuseAsync(
        string reason,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        SessionsLog.RefreshRefused(logger, reason);
    }

    private async Task RevokeFamilyAsync(Guid tokenFamilyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        List<Session> family = await database.Sessions
            .Where(candidate => candidate.TokenFamilyId == tokenFamilyId && candidate.RevokedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (Session member in family)
        {
            member.Revoke(SessionRevokeReason.RefreshTokenReuse, now);
        }

        await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static readonly string LockTokenSql =
        $"SELECT * FROM {SessionsSchema.RefreshTokensTable} WHERE token_digest = {{0}} FOR UPDATE";

    private static readonly string LockSessionSql =
        $"SELECT * FROM {SessionsSchema.SessionsTable} WHERE id = {{0}} FOR UPDATE";

    private Task<RefreshToken?> LockTokenAsync(byte[] digest, CancellationToken cancellationToken) =>
        database.RefreshTokens
            .FromSqlRaw(LockTokenSql, digest)
            .SingleOrDefaultAsync(cancellationToken);

    private Task<Session> LockSessionAsync(Guid sessionId, CancellationToken cancellationToken) =>
        database.Sessions
            .FromSqlRaw(LockSessionSql, sessionId)
            .SingleAsync(cancellationToken);

    private SessionTokenResult Result(Session session, UserId userId, string rawRefresh, DateTimeOffset now) =>
        new(
            session.Id,
            userId,
            accessTokens.Issue(userId, session.Id, now),
            now + settings.AccessTokenLifetime,
            rawRefresh,
            now + settings.RefreshTokenLifetime);
}
