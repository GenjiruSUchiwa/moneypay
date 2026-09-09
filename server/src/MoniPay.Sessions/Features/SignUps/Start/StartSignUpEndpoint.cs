using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;

namespace MoniPay.Sessions.Features.SignUps.Start;

/// <summary>The anonymous start of a sign-up. Validates the request at the edge, then hands one command to one handler.</summary>
internal static class StartSignUpEndpoint
{
    public static IEndpointRouteBuilder MapStartSignUp(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapPost(SignUpRoutes.Start, StartAsync)
            .WithName(SignUpEndpointNames.StartSignUp)
            .WithSummary(SignUpSummaries.StartSignUp)
            .Accepts<JsonApiRequest<JsonApiRequestResource<StartSignUpAttributes>>>(
                MoniPayMediaTypes.JsonApi,
                MoniPayMediaTypes.AnyContentType)
            .WithMetadata(new JsonApiResourceType(SignUpResourceTypes.SignUps))
            .Produces<JsonApiResponse<SignUpResource<StartSignUpResourceAttributes>>>(StatusCodes.Status202Accepted, MoniPayMediaTypes.JsonApi)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status406NotAcceptable)
            .ProducesProblem(StatusCodes.Status413RequestEntityTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireRateLimiting(SignUpRateLimitPolicies.Start);

        return routes;
    }

    private static async Task<IResult> StartAsync(
        JsonApiRequest<JsonApiRequestResource<StartSignUpAttributes>> request,
        StartSignUpHandler handler,
        IOptions<SessionsOptions> options,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(http);

        SessionsOptions settings = options.Value;
        StartSignUpCommand command = request.Data.Attributes.Validate(settings, CurrentLocale());
        StartSignUpResult result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        SignUpResource<StartSignUpResourceAttributes> resource = SignUpResources.FromStart(result);
        JsonApiResponse<SignUpResource<StartSignUpResourceAttributes>> document = new()
        {
            Data = resource,
            Links = new JsonApiLinks { Self = resource.Links?.Self },
        };
        http.Response.Headers.Location = resource.Links?.Self;

        return TypedResults.Json(document, contentType: MoniPayMediaTypes.JsonApi, statusCode: StatusCodes.Status202Accepted);
    }

    private static Locale CurrentLocale()
    {
        string tag = CultureInfo.CurrentUICulture.Name;
        return Locale.TryParse(tag, out Locale locale) ? locale : Locale.Default;
    }
}
