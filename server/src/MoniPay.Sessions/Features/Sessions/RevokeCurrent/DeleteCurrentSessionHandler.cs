using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Features.Sessions.RevokeCurrent;

/// <summary>
/// Ends the session the caller's ticket names. Revocation is the token service's rule; the slice
/// is the HTTP edge that names which session to end.
/// </summary>
internal sealed class DeleteCurrentSessionHandler(SessionTokenService sessions)
{
    public Task HandleAsync(Guid sessionId, CancellationToken cancellationToken) =>
        sessions.RevokeAsync(sessionId, cancellationToken);
}
