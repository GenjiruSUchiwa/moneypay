using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Features.Sessions.RevokeCurrent;

internal static class DeleteCurrentSessionEndpoint
{
    public static IEndpointRouteBuilder MapDeleteCurrentSession(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapDelete(SessionRoutes.Current, RevokeAsync)
            .WithName(SessionEndpointNames.DeleteCurrentSession)
            .WithSummary(SessionSummaries.DeleteCurrentSession)
            .WithMetadata(new JsonApiNoBody())
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status406NotAcceptable)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return routes;
    }

    private static async Task<IResult> RevokeAsync(
        SessionTokenService sessions,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(http);

        await sessions
            .RevokeAsync(SessionIdentity.Published(http).SessionId, cancellationToken)
            .ConfigureAwait(false);

        return TypedResults.NoContent();
    }
}
