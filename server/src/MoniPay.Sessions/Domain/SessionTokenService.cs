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

/// <summary>
/// The one place a session's credentials are created, replaced, refreshed and revoked; the
/// completion, refresh and revocation slices all call it. Only a refresh token's digest is ever
/// stored, and a refused refresh names its reason to the log alone, so a probe learns nothing
/// about the token it presented.
/// </summary>
internal sealed class SessionTokenService(
    MoniPayDbContext database,
    AccessTokenIssuer accessTokens,
    RefreshTokenFactory refreshTokens,
    TimeProvider timeProvider,
    IOptions<SessionsOptions> options,
    ILogger<SessionTokenService> logger)
{
    private readonly SessionsOptions settings = options.Value;

    /// <summary>Starts a session and its refresh-token family. Joins the completion's ambient
    /// transaction when one is open; otherwise commits on its own.</summary>
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
        await PersistAsync(cancellationToken).ConfigureAwait(false);

        return Result(session, userId, rawRefresh, now);
    }

    /// <summary>Ends a bootstrap session a retry replaced, and issues its replacement in a new
    /// family. The revocation and the new session share one commit.</summary>
    public async Task<SessionTokenResult> ReplaceBootstrapAsync(
        UserId userId,
        Guid priorSessionId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        Session prior = await database.Sessions
            .SingleAsync(session => session.Id == priorSessionId, cancellationToken)
            .ConfigureAwait(false);
        prior.Revoke(SessionRevokeReason.BootstrapReplaced, timeProvider.GetUtcNow());

        return await CreateAsync(userId, deviceId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Exchanges a refresh token for a new one, in one transaction: the token row is
    /// locked by digest, so two parallel refreshes of one token serialize and the loser is
    /// answered as a replay that revokes the family.</summary>
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

    /// <summary>Ends a session at the user's request. Revoking a session that already ended, or
    /// that never existed, changes nothing.</summary>
    public async Task RevokeAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        Session? session = await database.Sessions
            .SingleOrDefaultAsync(candidate => candidate.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);
        if (session is null)
        {
            return;
        }

        session.Revoke(SessionRevokeReason.UserRequest, timeProvider.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Saves, joining the ambient transaction the completion opened when one is open — the
    /// bootstrap session then commits or rolls back with the user row it belongs to.
    /// </summary>
    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        if (database.Database.CurrentTransaction is not null)
        {
            await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await using IDbContextTransaction transaction = await database.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Rolls a refused refresh back. The reason goes to the log; the client gets one type.</summary>
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

    // Constants, so no request value can reach the SQL text: the values travel as parameters.
    private static readonly string LockTokenSql =
        $"SELECT * FROM {SessionsSchema.RefreshTokensTable} WHERE token_digest = {{0}} FOR UPDATE";

    private static readonly string LockSessionSql =
        $"SELECT * FROM {SessionsSchema.SessionsTable} WHERE id = {{0}} FOR UPDATE";

    /// <summary>
    /// Loads a refresh token by digest under a row lock held until the transaction ends, so a
    /// replay presenting the same token waits for the first refresh to commit and is then
    /// answered by the consumed check.
    /// </summary>
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
