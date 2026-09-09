using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;

namespace MoniPay.Sessions.Features.Sessions.GetCurrent;

/// <summary>
/// Reads the current session. The bearer credential is the only thing that names it: the route
/// takes no identifier, so no body or query can select another user's session.
/// </summary>
internal static class GetCurrentSessionEndpoint
{
    public static IEndpointRouteBuilder MapGetCurrentSession(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapGet(SessionRoutes.Current, GetAsync)
            .WithName(SessionEndpointNames.GetCurrentSession)
            .WithSummary(SessionSummaries.GetCurrentSession)
            .Produces<JsonApiResponse<JsonApiResponseResource<ReadSessionAttributes>>>(
                StatusCodes.Status200OK,
                MoniPayMediaTypes.JsonApi)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status406NotAcceptable);

        return routes;
    }

    private static async Task<IResult> GetAsync(
        GetCurrentSessionHandler handler,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(principal);

        (Guid sessionId, UserId userId) = SessionIdentity.Of(principal);
        CurrentSessionView view = await handler
            .HandleAsync(sessionId, userId, SessionIdentity.AccessTokenExpiry(principal), cancellationToken)
            .ConfigureAwait(false);

        JsonApiResponseResource<ReadSessionAttributes> resource = SessionResources.Read(view);

        return TypedResults.Json(
            new JsonApiResponse<JsonApiResponseResource<ReadSessionAttributes>>
            {
                Data = resource,
                Links = new JsonApiLinks { Self = resource.Links?.Self },
            },
            contentType: MoniPayMediaTypes.JsonApi,
            statusCode: StatusCodes.Status200OK);
    }
}
