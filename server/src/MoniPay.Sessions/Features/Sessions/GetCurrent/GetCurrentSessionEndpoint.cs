using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;

namespace MoniPay.Sessions.Features.Sessions.GetCurrent;

/// <summary>
/// Reads the current session. The bearer credential is the only thing that names it: the route
/// takes no identifier, so no body or query can select another user's session. A client that
/// sends a body is answered with a 415 before it is read.
/// </summary>
internal static class GetCurrentSessionEndpoint
{
    public static IEndpointRouteBuilder MapGetCurrentSession(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapGet(SessionRoutes.Current, GetAsync)
            .WithName(SessionEndpointNames.GetCurrentSession)
            .WithSummary(SessionSummaries.GetCurrentSession)
            .WithMetadata(new JsonApiNoBody())
            .Produces<JsonApiResponse<JsonApiResponseResource<ReadSessionAttributes>>>(
                StatusCodes.Status200OK,
                MoniPayMediaTypes.JsonApi)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status406NotAcceptable)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return routes;
    }

    private static async Task<IResult> GetAsync(
        GetCurrentSessionHandler handler,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(http);

        // The ticket the active-session policy already parsed and proved.
        SessionTicket ticket = SessionIdentity.Published(http);
        CurrentSessionView view = await handler
            .HandleAsync(
                ticket.SessionId,
                ticket.UserId,
                SessionIdentity.AccessTokenExpiry(http.User),
                cancellationToken)
            .ConfigureAwait(false);

        return JsonApiResults.Json(SessionResources.Read(view));
    }
}
