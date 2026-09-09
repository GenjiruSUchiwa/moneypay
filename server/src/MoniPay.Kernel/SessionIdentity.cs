using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;

namespace MoniPay.Kernel;

/// <summary>
/// The caller's identity as the bearer routes read it: the <c>sid</c> and <c>sub</c> claims the
/// ticket carries. A ticket that does not name both a session and its user names nothing at all.
/// </summary>
public readonly record struct SessionTicket(Guid SessionId, UserId UserId);

/// <summary>
/// The one reader of the bearer ticket. It lives in Kernel rather than Sessions because both
/// Sessions and Users read the same credential, and two parsers for one ticket would drift. The
/// authorization handler parses it once per request and publishes the result on the request; an
/// endpoint reads what was published rather than parsing the principal again.
/// </summary>
public static class SessionIdentity
{
    /// <summary>
    /// The ticket, or a refusal. Nothing else in the credential is trusted: a ticket missing
    /// either claim is refused as an invalid credential, never answered for another one.
    /// </summary>
    public static SessionTicket Of(ClaimsPrincipal principal) =>
        TryOf(principal, out SessionTicket ticket)
            ? ticket
            : throw new RefusalException(MoniPayErrorTypes.SessionInvalid);

    /// <summary>The ticket, or <see langword="false"/> when either claim is missing or malformed.</summary>
    public static bool TryOf(ClaimsPrincipal principal, out SessionTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (!Guid.TryParse(principal.FindFirst(MoniPayClaimTypes.SessionId)?.Value, out Guid sessionId)
            || !Guid.TryParse(principal.FindFirst(MoniPayClaimTypes.Subject)?.Value, out Guid userId))
        {
            ticket = default;
            return false;
        }

        ticket = new SessionTicket(sessionId, new UserId(userId));
        return true;
    }

    /// <summary>
    /// Records the parsed ticket on the request, so the endpoint behind the policy reads the very
    /// identity the policy proved active instead of parsing the principal a second time.
    /// </summary>
    public static void Publish(HttpContext context, SessionTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Items[MoniPayHttpContextItems.SessionTicket] = ticket;
    }

    /// <summary>
    /// The ticket the authorization handler published. A route reaches its endpoint only behind
    /// <see cref="MoniPayPolicies.AuthenticatedUser"/>, so an absent ticket means the credential
    /// never proved itself, and the caller is refused rather than answered.
    /// </summary>
    public static SessionTicket Published(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items.TryGetValue(MoniPayHttpContextItems.SessionTicket, out object? value)
            && value is SessionTicket ticket
            ? ticket
            : throw new RefusalException(MoniPayErrorTypes.SessionInvalid);
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
