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
        if (!Guid.TryParse(Find(context.User, MoniPayClaimTypes.Subject), out Guid userIdValue)
            || !Guid.TryParse(Find(context.User, MoniPayClaimTypes.SessionId), out Guid sessionIdValue))
        {
            return;
        }

        CancellationToken cancellation = context.Resource is HttpContext http
            ? http.RequestAborted
            : CancellationToken.None;
        bool active = await database.Sessions
            .AsNoTracking()
            .AnyAsync(
                session => session.Id == sessionIdValue
                    && session.UserId == new UserId(userIdValue)
                    && session.RevokedAt == null,
                cancellation)
            .ConfigureAwait(false);
        if (active)
        {
            context.Succeed(requirement);
        }
    }

    private void HandleRegistrationRoute(AuthorizationHandlerContext context, IAuthorizationRequirement requirement)
    {
        if (context.Resource is not HttpContext http)
        {
            return;
        }

        string? claim = Find(context.User, MoniPayClaimTypes.SignUpId);
        if (claim is not null && claim == http.Request.RouteValues[MoniPayClaimTypes.SignUpId]?.ToString())
        {
            context.Succeed(requirement);
        }
    }

    private static string? Find(ClaimsPrincipal user, string claimType) =>
        user.FindFirst(claimType)?.Value;
}
