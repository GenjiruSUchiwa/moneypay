using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions.Features.SignUps.VerifyPhone;

/// <summary>
/// Proves control of the phone and hands out the registration credential. The sign-up credential
/// authorizes the call, the route names the sign-up, and the response is the only place the
/// registration token ever appears.
/// </summary>
internal static class CreatePhoneVerificationEndpoint
{
    public static IEndpointRouteBuilder MapCreatePhoneVerification(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapPost(SignUpRoutes.PhoneVerifications, VerifyAsync)
            .WithName(SignUpEndpointNames.CreatePhoneVerification)
            .WithSummary(SignUpSummaries.CreatePhoneVerification)
            .Accepts<JsonApiRequest<SignUpCommandResource<CreatePhoneVerificationAttributes>>>(
                MoniPayMediaTypes.JsonApi,
                MoniPayMediaTypes.AnyContentType)
            .WithMetadata(new JsonApiResourceType(SignUpResourceTypes.PhoneVerifications))
            .Produces<JsonApiResponse<JsonApiResponseResource<VerifiedSignUpResourceAttributes>>>(
                StatusCodes.Status200OK,
                MoniPayMediaTypes.JsonApi)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status406NotAcceptable)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status410Gone)
            .ProducesProblem(StatusCodes.Status413RequestEntityTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = SessionsSchemes.SignUp })
            .RequireRateLimiting(SignUpRateLimitPolicies.Verify);

        return routes;
    }

    private static async Task<IResult> VerifyAsync(
        Guid signUpId,
        JsonApiRequest<SignUpCommandResource<CreatePhoneVerificationAttributes>> request,
        CreatePhoneVerificationHandler handler,
        IOptions<SessionsOptions> options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(options);

        SignUpId id = new(signUpId);
        request.Data.RequireMatch(id);
        string code = request.Data.Attributes.Validate(options.Value);

        CreatePhoneVerificationResult result = await handler
            .HandleAsync(id, code, cancellationToken)
            .ConfigureAwait(false);

        JsonApiResponseResource<VerifiedSignUpResourceAttributes> resource = SignUpResources.FromVerification(result);

        return TypedResults.Json(
            new JsonApiResponse<JsonApiResponseResource<VerifiedSignUpResourceAttributes>>
            {
                Data = resource,
                Links = new JsonApiLinks { Self = resource.Links?.Self },
            },
            contentType: MoniPayMediaTypes.JsonApi,
            statusCode: StatusCodes.Status200OK);
    }
}
