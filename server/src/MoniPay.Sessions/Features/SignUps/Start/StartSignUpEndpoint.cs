using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Kernel.Validation;

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
            .Produces<JsonApiResponse<SignUpResource>>(StatusCodes.Status202Accepted, MoniPayMediaTypes.JsonApi)
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
        ValidationFailures failures = request.Data.Attributes.Validate(settings);
        if (failures.Any())
        {
            throw new ValidationException(failures);
        }

        if (!PhoneNumber.TryNormalize(
                request.Data.Attributes.Phone,
                settings.SupportedCountries,
                out PhoneNumber phone,
                out _))
        {
            ValidationFailures retry = new();
            retry.Require(false, StartSignUpPointers.Phone, ValidationCodes.PhoneFormatInvalid);
            throw new ValidationException(retry);
        }

        Locale locale = CurrentLocale();
        StartSignUpCommand command = new(
            phone,
            locale,
            request.Data.Attributes.TermsVersion,
            request.Data.Attributes.PrivacyVersion);
        StartSignUpResult result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        SignUpResource resource = SignUpResources.FromStart(result);
        JsonApiResponse<SignUpResource> document = new()
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
