using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions.Features.SignUps.Complete;

/// <summary>
/// Completes a sign-up: the registration credential in the header authorizes it, the profile in
/// the body names the user, and the response is the first session. A safe retry with the same
/// credential returns a replacement session instead of a second user.
/// </summary>
internal static class CreateSignUpCompletionEndpoint
{
    public static IEndpointRouteBuilder MapCreateSignUpCompletion(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapPost(SignUpRoutes.Completions, CompleteAsync)
            .WithName(SignUpEndpointNames.CreateSignUpCompletion)
            .WithSummary(SignUpSummaries.CreateSignUpCompletion)
            .Accepts<JsonApiRequest<SignUpCommandResource<CreateSignUpCompletionAttributes>>>(
                MoniPayMediaTypes.JsonApi,
                MoniPayMediaTypes.AnyContentType)
            .WithMetadata(new JsonApiResourceType(SignUpResourceTypes.SignUpCompletions))
            .Produces<JsonApiResponse<JsonApiResponseResource<SessionCredentialsAttributes>>>(
                StatusCodes.Status201Created,
                MoniPayMediaTypes.JsonApi)
            .Produces<JsonApiResponse<JsonApiResponseResource<SessionCredentialsAttributes>>>(
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
            .RequireAuthorization(new AuthorizeAttribute
            {
                Policy = MoniPayPolicies.Registration,
                AuthenticationSchemes = SessionsSchemes.Registration,
            })
            .RequireRateLimiting(SignUpRateLimitPolicies.Complete);

        return routes;
    }

    private static async Task<IResult> CompleteAsync(
        Guid signUpId,
        JsonApiRequest<SignUpCommandResource<CreateSignUpCompletionAttributes>> request,
        CreateSignUpCompletionHandler handler,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(http);

        SignUpId id = new(signUpId);
        request.Data.RequireMatch(id);
        CreateSignUpCompletionCommand command = request.Data.Attributes.Validate();

        CreateSignUpCompletionResult result = await handler
            .HandleAsync(id, RegistrationCredential(http), command, cancellationToken)
            .ConfigureAwait(false);

        return result.Created ? Created(result.Session, http) : Replaced(result.Session);
    }

    /// <summary>The credential is the header the scheme authenticated. A body never carries it.</summary>
    private static string RegistrationCredential(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);

        string token = WorkflowAuthenticationHandler.PresentedCredential(http.Request, SessionsSchemes.Registration);

        return token.Length > 0
            ? token
            : throw new RefusalException(MoniPayErrorTypes.RegistrationTokenInvalid);
    }

    private static IResult Replaced(SessionTokenResult session) => TypedResults.Json(
        SessionDocument(session),
        contentType: MoniPayMediaTypes.JsonApi,
        statusCode: StatusCodes.Status200OK);

    private static IResult Created(SessionTokenResult session, HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        http.Response.Headers.Location = SessionResources.Self;

        return TypedResults.Json(
            SessionDocument(session),
            contentType: MoniPayMediaTypes.JsonApi,
            statusCode: StatusCodes.Status201Created);
    }

    private static JsonApiResponse<JsonApiResponseResource<SessionCredentialsAttributes>> SessionDocument(
        SessionTokenResult session)
    {
        JsonApiResponseResource<SessionCredentialsAttributes> resource = SessionResources.Credentials(session);

        return new()
        {
            Data = resource,
            Links = new JsonApiLinks { Self = resource.Links?.Self },
        };
    }
}
