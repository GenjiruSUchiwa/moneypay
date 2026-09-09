using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using MoniPay.Kernel;
using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Features.Sessions.RevokeCurrent;

/// <summary>
/// Revokes the current session and answers with no body at all: a successful DELETE is 204 with
/// no JSON:API envelope, because there is no representation left to describe.
/// </summary>
internal static class DeleteCurrentSessionEndpoint
{
    public static IEndpointRouteBuilder MapDeleteCurrentSession(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapDelete(SessionRoutes.Current, RevokeAsync)
            .WithName(SessionEndpointNames.DeleteCurrentSession)
            .WithSummary(SessionSummaries.DeleteCurrentSession)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status406NotAcceptable);

        return routes;
    }

    private static async Task<IResult> RevokeAsync(
        SessionTokenService sessions,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(principal);

        (Guid sessionId, UserId _) = SessionIdentity.Of(principal);
        await sessions.RevokeAsync(sessionId, cancellationToken).ConfigureAwait(false);

        return TypedResults.NoContent();
    }
}
