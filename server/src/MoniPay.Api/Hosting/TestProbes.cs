using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Kernel.Validation;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Security;

namespace MoniPay.Api.Hosting;

/// <summary>
/// The throwaway endpoints the authentication schemes, the named policies, the IP rate limits
/// and the no-store convention are proven on. They exist in the Testing environment only and
/// vanish when the real routes land (#97 to #100): they answer 200 with no body semantics, and
/// a test reads the status the host answers a credential with.
/// </summary>
internal static class TestProbes
{
    private const string SecureRoute = "/test/secure";
    private const string RoleRoute = "/test/role";
    private const string SignUpRoute = "/test/signups/{signUpId:guid}";
    private const string RegistrationRoute = "/test/registrations/{signUpId:guid}";
    private const string NoStoreRoute = "/test/no-store";
    private const string LimitedStartRoute = "/test/limited/start";
    private const string LimitedRefreshRoute = "/test/limited/refresh";
    private const string ErrorRoute = "/test/errors/{kind}";
    private const string WidgetRoute = "/test/jsonapi/widgets";
    private const string WidgetReadRoute = "/test/jsonapi/widgets/read";
    private const string WidgetResourceType = "widgets";

    /// <summary>The endpoint name the error probe logs under.</summary>
    public const string ErrorProbeName = nameof(TestProbes) + ".Errors";

    public static void MapTestProbes(this WebApplication app)
    {
        app.MapGet(SecureRoute, () => Results.Ok())
            .RequireAuthorization(MoniPayPolicies.AuthenticatedUser)
            .WithMetadata(nameof(TestProbes));

        app.MapGet(RoleRoute, () => Results.Ok())
            .RequireAuthorization(policy => policy.RequireRole(nameof(TestProbes)));

        app.MapGet(SignUpRoute, () => Results.Ok())
            .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = SessionsSchemes.SignUp })
            .WithMetadata(MoniPayConventions.NoStore);

        app.MapGet(RegistrationRoute, () => Results.Ok())
            .RequireAuthorization(MoniPayPolicies.Registration);

        app.MapGet(NoStoreRoute, () => Results.Ok())
            .WithMetadata(MoniPayConventions.NoStore);

        app.MapGet(LimitedStartRoute, () => Results.Ok())
            .RequireRateLimiting(SignUpRateLimitPolicies.Start)
            .WithMetadata(MoniPayConventions.NoStore);

        app.MapGet(LimitedRefreshRoute, () => Results.Ok())
            .RequireRateLimiting(SessionRateLimitPolicies.Refresh);

        app.MapGet(ErrorRoute, ThrowProbe)
            .WithName(ErrorProbeName)
            .WithMetadata(MoniPayConventions.NoStore);

        app.MapPost(WidgetRoute, AcceptWidget)
            .WithMetadata(MoniPayConventions.JsonApi)
            .WithMetadata(new JsonApiResourceType(WidgetResourceType))
            .WithMetadata(MoniPayConventions.NoStore);

        app.MapGet(WidgetReadRoute, () => Results.Ok())
            .WithMetadata(MoniPayConventions.JsonApi)
            .WithMetadata(MoniPayConventions.NoStore);
    }

    /// <summary>
    /// The test-only strict records that prove the slice integration: the expected resource type
    /// travels as endpoint metadata, and the attribute record rejects its own unknown members
    /// because the envelope's annotation is not recursive.
    /// </summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed record WidgetAttributes
    {
        public required string Name { get; init; }
    }

    private static IResult AcceptWidget(JsonApiRequest<JsonApiRequestResource<WidgetAttributes>> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Results.Ok();
    }

    private static IResult ThrowProbe(string kind) => throw ErrorFor(kind);

    private static Exception ErrorFor(string kind) => kind switch
    {
        "validation" => ValidationExceptionProbe(),
        "refusal" => new RefusalException(
            MoniPayErrorTypes.RateLimited,
            TimeSpan.FromSeconds(1.5),
            ["/data/attributes/phone"]),
        "refusal-no-pointers" => new RefusalException(MoniPayErrorTypes.SignUpStateInvalid),
        "unknown" => new RefusalException(new ProblemType("mystery", HttpStatusCode.BadRequest)),
        "provider" => new ProviderUnavailableException("campay", "E123"),
        "concurrency" => new DbUpdateConcurrencyException("lost update"),
        "json" => new JsonException("syntax"),
        "cancelled" => new OperationCanceledException("an application cancellation"),
        _ => new InvalidOperationException("boom"),
    };

    private static ValidationException ValidationExceptionProbe()
    {
        ValidationFailures failures = new();
        failures.Require(false, "/data/attributes/phone", ValidationCodes.PhoneFormatInvalid);
        failures.Require(false, "/data/attributes/name~1first", ValidationCodes.PersonNameInvalid);
        failures.Require(false, "/data/attributes/email~0local", ValidationCodes.EmailInvalid);

        return new ValidationException(failures);
    }
}
