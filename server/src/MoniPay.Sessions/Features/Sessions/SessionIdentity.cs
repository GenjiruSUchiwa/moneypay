using System.Security.Claims;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;

namespace MoniPay.Sessions.Features.Sessions;

/// <summary>
/// The caller's identity as the session routes read it: the <c>sid</c> and <c>sub</c> claims the
/// bearer ticket carries, and the moment the presented access token expires. Nothing else in the
/// ticket is trusted, and a ticket that does not name both a session and its user names no
/// session at all — it is refused as an invalid credential, never answered for another one.
/// </summary>
internal static class SessionIdentity
{
    public static (Guid SessionId, UserId UserId) Of(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (!Guid.TryParse(principal.FindFirst(MoniPayClaimTypes.SessionId)?.Value, out Guid sessionId)
            || !Guid.TryParse(principal.FindFirst(MoniPayClaimTypes.Subject)?.Value, out Guid userIdValue))
        {
            throw new RefusalException(MoniPayErrorTypes.SessionInvalid);
        }

        return (sessionId, new UserId(userIdValue));
    }

    /// <summary>
    /// When the presented access token expires, read from its own <c>exp</c> claim rather than
    /// recomputed from the clock: the read form reports the credential the client holds.
    /// </summary>
    public static DateTimeOffset AccessTokenExpiry(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return long.TryParse(principal.FindFirst("exp")?.Value, out long seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : throw new RefusalException(MoniPayErrorTypes.SessionInvalid);
    }
}
