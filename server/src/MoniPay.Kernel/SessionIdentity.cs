using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;

namespace MoniPay.Kernel;

public readonly record struct SessionTicket(Guid SessionId, UserId UserId);

public static class SessionIdentity
{
    public static SessionTicket Of(ClaimsPrincipal principal) =>
        TryOf(principal, out SessionTicket ticket)
            ? ticket
            : throw new RefusalException(MoniPayErrorTypes.SessionInvalid);

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

    public static void Publish(HttpContext context, SessionTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Items[MoniPayHttpContextItems.SessionTicket] = ticket;
    }

    public static SessionTicket Published(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items.TryGetValue(MoniPayHttpContextItems.SessionTicket, out object? value)
            && value is SessionTicket ticket
            ? ticket
            : throw new RefusalException(MoniPayErrorTypes.SessionInvalid);
    }

    public static DateTimeOffset AccessTokenExpiry(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return long.TryParse(principal.FindFirst("exp")?.Value, out long seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : throw new RefusalException(MoniPayErrorTypes.SessionInvalid);
    }
}
