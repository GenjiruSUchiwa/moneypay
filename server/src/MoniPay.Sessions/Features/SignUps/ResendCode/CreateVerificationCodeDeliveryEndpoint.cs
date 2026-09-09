using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions.Features.SignUps.ResendCode;

internal static class CreateVerificationCodeDeliveryEndpoint
{
    public static IEndpointRouteBuilder MapCreateVerificationCodeDelivery(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapPost(SignUpRoutes.VerificationCodeDeliveries, DeliverAsync)
            .WithName(SignUpEndpointNames.CreateVerificationCodeDelivery)
            .WithSummary(SignUpSummaries.CreateVerificationCodeDelivery)
            .WithMetadata(new JsonApiNoBody())
            .Produces<JsonApiResponse<JsonApiResponseResource<ReadSignUpResourceAttributes>>>(
                StatusCodes.Status202Accepted,
                MoniPayMediaTypes.JsonApi)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status406NotAcceptable)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status410Gone)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = SessionsSchemes.SignUp })
            .RequireRateLimiting(SignUpRateLimitPolicies.Resend);

        return routes;
    }

    private static async Task<IResult> DeliverAsync(
        Guid signUpId,
        CreateVerificationCodeDeliveryHandler handler,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(http);

        SignUpId id = new(signUpId);
        CreateVerificationCodeDeliveryResult result = await handler
            .HandleAsync(id, cancellationToken)
            .ConfigureAwait(false);

        JsonApiResponseResource<ReadSignUpResourceAttributes> resource = SignUpResources.FromResend(id, result);
        http.Response.Headers.Location = resource.Links?.Self;

        return JsonApiResults.Json(resource, StatusCodes.Status202Accepted);
    }
}
