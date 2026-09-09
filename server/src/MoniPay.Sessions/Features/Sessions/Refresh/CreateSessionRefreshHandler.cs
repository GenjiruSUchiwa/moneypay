using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Features.Sessions.Refresh;

/// <summary>
/// Rotates one session's credentials. The rotation rules — expiry before replay, replay before
/// device matching — belong to the token service; this slice is the HTTP edge that asks for it.
/// </summary>
internal sealed class CreateSessionRefreshHandler(SessionTokenService sessions)
{
    public Task<SessionTokenResult> HandleAsync(
        string refreshToken,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);

        return sessions.RefreshAsync(refreshToken, deviceId, cancellationToken);
    }
}
