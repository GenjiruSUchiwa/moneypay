using Microsoft.EntityFrameworkCore;
using MoniPay.Kernel;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Persistence;

namespace MoniPay.Sessions.Features.Sessions.GetCurrent;

/// <summary>
/// Reads the session the caller's ticket names. The active-session policy has already proven the
/// session exists and is not revoked, so the read is one query scoped to both identifiers.
/// </summary>
internal sealed class GetCurrentSessionHandler(MoniPayDbContext database)
{
    public async Task<CurrentSessionView> HandleAsync(
        Guid sessionId,
        UserId userId,
        DateTimeOffset accessTokenExpiresAt,
        CancellationToken cancellationToken)
    {
        Session session = await database.Sessions
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == sessionId && candidate.UserId == userId,
                cancellationToken)
            .ConfigureAwait(false);

        return new CurrentSessionView(
            session.Id,
            session.UserId,
            session.CreatedAt,
            session.LastSeenAt,
            accessTokenExpiresAt);
    }
}
