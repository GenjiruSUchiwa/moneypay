using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;

namespace MoniPay.Users.Features.CurrentUser;

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

        UserId userId = SessionIdentity.Published(http).UserId;
        CurrentUserView view = await handler
            .HandleAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return JsonApiResults.Json(UserResources.FromView(view));
    }
}
