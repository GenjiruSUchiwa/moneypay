using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;

namespace MoniPay.Users.Features.CurrentUser;

/// <summary>
/// Reads the current user. The bearer credential is the only thing that names it: the route takes
/// no identifier, so no body, query, or route value can select another user's profile. A client
/// that sends a body is answered with a 415 before it is read.
/// </summary>
internal static class GetCurrentUserEndpoint
{
    public static IEndpointRouteBuilder MapGetCurrentUser(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapGet(MoniPayRoutes.CurrentUser, GetAsync)
            .WithName(UserEndpointNames.GetCurrentUser)
            .WithSummary(UserSummaries.GetCurrentUser)
            .WithTags(UserTags.Users)
            .WithMetadata(MoniPayConventions.JsonApi)
            .WithMetadata(MoniPayConventions.NoStore)
            .WithMetadata(new JsonApiNoBody())
            .Produces<JsonApiResponse<JsonApiResponseResource<UserAttributes>>>(
                StatusCodes.Status200OK,
                MoniPayMediaTypes.JsonApi)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status406NotAcceptable)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            // Stored contact data that will not decrypt is a data fault, not a client one: the
            // read fails as a 500 and the document is never half-built.
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(MoniPayPolicies.AuthenticatedUser)
            .RequireRateLimiting(MoniPayRateLimitPolicies.AuthenticatedRead);

        return routes;
    }

    private static async Task<IResult> GetAsync(
        GetCurrentUserHandler handler,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(http);

        // The ticket the active-session policy already parsed and proved. Only the user
        // identifier reaches the handler.
        UserId userId = SessionIdentity.Published(http).UserId;
        CurrentUserView view = await handler
            .HandleAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return JsonApiResults.Json(UserResources.FromView(view));
    }
}
