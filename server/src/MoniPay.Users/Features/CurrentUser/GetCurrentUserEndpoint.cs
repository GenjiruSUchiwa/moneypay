using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;

namespace MoniPay.Users.Features.CurrentUser;

/// <summary>
/// Reads the current user. The bearer credential is the only thing that names it: the route takes
/// no identifier, so no body, query, or route value can select another user's profile.
/// </summary>
internal static class GetCurrentUserEndpoint
{
    public static IEndpointRouteBuilder MapGetCurrentUser(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapGet(UserRoutes.Me, GetAsync)
            .WithName(UserEndpointNames.GetCurrentUser)
            .WithSummary(UserSummaries.GetCurrentUser)
            .Produces<JsonApiResponse<JsonApiResponseResource<UserAttributes>>>(
                StatusCodes.Status200OK,
                MoniPayMediaTypes.JsonApi)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status406NotAcceptable);

        return routes;
    }

    private static async Task<IResult> GetAsync(
        GetCurrentUserHandler handler,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(principal);

        // The full ticket is parsed, not just the subject: the session policy has already proven
        // the session belongs to this user, and parsing both claims keeps the one credential with
        // its one reader. Only the user identifier reaches the handler.
        UserId userId = SessionIdentity.Of(principal).UserId;
        CurrentUserView view = await handler
            .HandleAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        JsonApiResponseResource<UserAttributes> resource = UserResources.FromView(view);

        return TypedResults.Json(
            JsonApiResponses.Document(resource),
            contentType: MoniPayMediaTypes.JsonApi,
            statusCode: StatusCodes.Status200OK);
    }
}
