using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions.Features.SignUps.Get;

/// <summary>Reads one sign-up. Accepts either workflow credential bound to the route; never returns a token.</summary>
internal static class GetSignUpEndpoint
{
    public static IEndpointRouteBuilder MapGetSignUp(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapGet(SignUpRoutes.ById, GetAsync)
            .WithName(SignUpEndpointNames.GetSignUp)
            .WithSummary(SignUpSummaries.GetSignUp)
            .Produces<JsonApiResponse<SignUpResource<ReadSignUpResourceAttributes>>>(StatusCodes.Status200OK, MoniPayMediaTypes.JsonApi)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status406NotAcceptable)
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = SessionsSchemes.Registration + "," + SessionsSchemes.SignUp,
            });

        return routes;
    }

    private static async Task<IResult> GetAsync(
        Guid signUpId,
        GetSignUpHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        SignUpId id = new(signUpId);
        SignUpView view = await handler.HandleAsync(id, cancellationToken).ConfigureAwait(false);

        SignUpResource<ReadSignUpResourceAttributes> resource = SignUpResources.FromView(id, view);
        JsonApiResponse<SignUpResource<ReadSignUpResourceAttributes>> document = new()
        {
            Data = resource,
            Links = new JsonApiLinks { Self = resource.Links?.Self },
        };

        return TypedResults.Json(document, contentType: MoniPayMediaTypes.JsonApi, statusCode: StatusCodes.Status200OK);
    }
}
