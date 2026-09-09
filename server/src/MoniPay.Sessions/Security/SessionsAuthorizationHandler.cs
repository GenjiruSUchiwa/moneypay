using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MoniPay.Kernel;
using MoniPay.Persistence;
using MoniPay.Sessions.Persistence;

namespace MoniPay.Sessions.Security;

/// <summary>
/// Answers the module's two policy requirements. The active-session check queries the sessions
/// table once per request — the design's decision: a revoked session must stop working within
/// the request that revokes it, and no cache can promise that. The route binding compares the
/// credential's <c>signUpId</c> claim with the route value.
/// </summary>
internal sealed class SessionsAuthorizationHandler(MoniPayDbContext database) : IAuthorizationHandler
{
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (IAuthorizationRequirement requirement in context.PendingRequirements)
        {
            if (requirement is ActiveSessionRequirement)
            {
                await HandleActiveSessionAsync(context, requirement).ConfigureAwait(false);
            }
            else if (requirement is RegistrationRouteRequirement)
            {
                HandleRegistrationRoute(context, requirement);
            }
        }
    }

    private async Task HandleActiveSessionAsync(AuthorizationHandlerContext context, IAuthorizationRequirement requirement)
    {
        // One reader for the one credential: the ticket is parsed here and nowhere else.
        if (!SessionIdentity.TryOf(context.User, out SessionTicket ticket))
        {
            return;
        }

        HttpContext? http = context.Resource as HttpContext;
        bool active = await database.Sessions
            .AsNoTracking()
            .AnyAsync(
                session => session.Id == ticket.SessionId
                    && session.UserId == ticket.UserId
                    && session.RevokedAt == null,
                http?.RequestAborted ?? CancellationToken.None)
            .ConfigureAwait(false);
        if (!active)
        {
            return;
        }

        // Published only once the session is proven active, so an endpoint that reads it can
        // never be reading a ticket the policy refused.
        if (http is not null)
        {
            SessionIdentity.Publish(http, ticket);
        }

        context.Succeed(requirement);
    }

    private void HandleRegistrationRoute(AuthorizationHandlerContext context, IAuthorizationRequirement requirement)
    {
        if (context.Resource is not HttpContext http)
        {
            return;
        }

        string? claim = Find(context.User, MoniPayClaimTypes.SignUpId);
        if (Guid.TryParse(claim, out Guid claimId)
            && Guid.TryParse(http.Request.RouteValues[MoniPayClaimTypes.SignUpId]?.ToString(), out Guid routeId)
            && claimId == routeId)
        {
            context.Succeed(requirement);
        }
    }

    private static string? Find(ClaimsPrincipal user, string claimType) =>
        user.FindFirst(claimType)?.Value;
}
