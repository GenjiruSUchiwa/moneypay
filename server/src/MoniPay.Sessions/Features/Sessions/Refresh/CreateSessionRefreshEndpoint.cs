using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Features.Sessions.Refresh;

/// <summary>
/// The anonymous rotation of one session's credentials: the refresh token in the body is the
/// credential that authorizes the call, so no <c>Authorization</c> header is read and OpenAPI
/// attaches no security scheme. It hangs off the root, outside the authenticated session group.
/// </summary>
internal static class CreateSessionRefreshEndpoint
{
    public static IEndpointRouteBuilder MapCreateSessionRefresh(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        RouteGroupBuilder refreshes = routes
            .MapGroup(SessionRoutes.Refreshes)
            .WithTags(SessionTags.Sessions)
            .WithMetadata(MoniPayConventions.JsonApi)
            .WithMetadata(MoniPayConventions.NoStore);

        refreshes.MapPost("", RefreshAsync)
            .WithName(SessionEndpointNames.CreateSessionRefresh)
            .WithSummary(SessionSummaries.CreateSessionRefresh)
            .Accepts<JsonApiRequest<JsonApiRequestResource<CreateSessionRefreshAttributes>>>(
                MoniPayMediaTypes.JsonApi,
                MoniPayMediaTypes.AnyContentType)
            .WithMetadata(new JsonApiResourceType(SessionResourceTypes.SessionRefreshes))
            .Produces<JsonApiResponse<JsonApiResponseResource<SessionCredentialsAttributes>>>(
                StatusCodes.Status200OK,
                MoniPayMediaTypes.JsonApi)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status406NotAcceptable)
            .ProducesProblem(StatusCodes.Status413RequestEntityTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireRateLimiting(SessionRateLimitPolicies.Refresh);

        return routes;
    }

    private static async Task<IResult> RefreshAsync(
        JsonApiRequest<JsonApiRequestResource<CreateSessionRefreshAttributes>> request,
        SessionTokenService sessions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(sessions);

        CreateSessionRefreshAttributes attributes = request.Data.Attributes;
        attributes.Validate();

        SessionTokenResult session = await sessions
            .RefreshAsync(attributes.RefreshToken, attributes.DeviceId, cancellationToken)
            .ConfigureAwait(false);

        return JsonApiResults.Json(SessionResources.Credentials(session));
    }
}
