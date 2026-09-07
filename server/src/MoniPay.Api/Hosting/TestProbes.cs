using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;
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
    private const string SignUpRoute = "/test/signups/{signUpId:guid}";
    private const string RegistrationRoute = "/test/registrations/{signUpId:guid}";
    private const string NoStoreRoute = "/test/no-store";
    private const string LimitedStartRoute = "/test/limited/start";
    private const string LimitedRefreshRoute = "/test/limited/refresh";

    public static void MapTestProbes(this WebApplication app)
    {
        app.MapGet(SecureRoute, () => Results.Ok())
            .RequireAuthorization(MoniPayPolicies.AuthenticatedUser);

        app.MapGet(SignUpRoute, () => Results.Ok())
            .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = SessionsSchemes.SignUp });

        app.MapGet(RegistrationRoute, () => Results.Ok())
            .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = SessionsSchemes.Registration });

        app.MapGet(NoStoreRoute, () => Results.Ok())
            .WithMetadata(MoniPayConventions.NoStore)
            .AddEndpointFilter<NoStoreEndpointFilter>();

        app.MapGet(LimitedStartRoute, () => Results.Ok())
            .RequireRateLimiting(SignUpRateLimitPolicies.Start);

        app.MapGet(LimitedRefreshRoute, () => Results.Ok())
            .RequireRateLimiting(SessionRateLimitPolicies.Refresh);

    }
}
