using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Kernel.Security;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Sessions.Features.Sessions.GetCurrent;
using MoniPay.Sessions.Features.Sessions.Refresh;
using MoniPay.Sessions.Features.Sessions.RevokeCurrent;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Features.SignUps.Get;
using MoniPay.Sessions.Features.SignUps.ResendCode;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Features.SignUps.VerifyPhone;
using MoniPay.Sessions.Persistence;
using MoniPay.Sessions.Providers;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions;

/// <summary>
/// The composition of the sign-up and session machinery: the module's own services, registered
/// by the module itself. The start and read slices are registered here because the host provides
/// the delivery port they need; the remaining slices stay test-only until their routes land.
/// </summary>
public static class SessionsModule
{
    public static IServiceCollection AddSessionsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SessionsOptions>()
            .Bind(configuration.GetSection(SessionsOptions.SectionName))
            .Validate(
                options => options.IsWithinBounds(),
                "The MoniPay:Sessions bounds are invalid: check the country rules, the code length, "
                + "the lifetimes, the attempt and resend limits, the legal versions and the cleanup bounds.")
            .Validate(
                options => options.HasWorkableTokenIssuer(),
                $"The MoniPay:Sessions token issuer is invalid: {SessionsOptions.Keys.Issuer} "
                + $"and {SessionsOptions.Keys.Audience} are required.")
            .RequireKey(options => options.VerificationCodeKeyBase64, SessionsOptions.Keys.VerificationCodeKeyBase64)
            .RequireKey(options => options.PersonalDataKeyBase64, SessionsOptions.Keys.PersonalDataKeyBase64)
            .RequireKey(options => options.SigningKeyBase64, SessionsOptions.Keys.SigningKeyBase64)
            .Validate(
                options => options.PreviousSigningKeyBase64 is null
                    || Base64Key.IsValid(options.PreviousSigningKeyBase64),
                $"{SessionsOptions.Keys.PreviousSigningKeyBase64} must be empty or a base64-encoded 32-byte key.")
            .ValidateOnStart();

        // All hold key material only; none holds request state.
        services.AddSingleton<VerificationCodeGenerator>();
        services.AddSingleton<VerificationCodeDigest>();
        services.AddSingleton<SignUpTokens>();
        services.AddSingleton<SignUpPersonalDataProtector>();
        services.AddSingleton<PhoneLookupDigest>();
        services.AddSingleton<AccessTokenIssuer>();
        services.AddSingleton<RefreshTokenFactory>();

        // Holds request state: the context it saves through.
        services.AddScoped<SessionTokenService>();
        services.AddSingleton<VerificationCodeRenderer>();

        // The slices the routes need. The host provides their ports, so they resolve here;
        // the remaining slices stay registered by the tests until their routes land.
        services.AddScoped<StartSignUpHandler>();
        services.AddScoped<GetSignUpHandler>();
        services.AddScoped<CreateVerificationCodeDeliveryHandler>();
        services.AddScoped<CreatePhoneVerificationHandler>();
        services.AddScoped<CreateSessionRefreshHandler>();
        services.AddScoped<GetCurrentSessionHandler>();
        services.AddScoped<DeleteCurrentSessionHandler>();

        services.AddSingleton<ExpiredCredentialCleanupService>();
        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ExpiredCredentialCleanupService>());

        AddAuthentication(services);
        AddAuthorization(services);

        return services;
    }

    /// <summary>
    /// The three schemes. The bearer scheme validates the access JWT — issuer, audience,
    /// signature, lifetime, HS256 only, inbound claim mapping off so the claim names are the
    /// ones the issuer stamped — accepting the previous signing key while a rotation is in
    /// flight. The two workflow schemes share one handler shape, one per purpose, and never
    /// accept each other's tokens: the purpose baked into the digest sees to that.
    /// </summary>
    private static void AddAuthentication(IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, SignUpAuthenticationHandler>(SessionsSchemes.SignUp, null)
            .AddScheme<AuthenticationSchemeOptions, RegistrationAuthenticationHandler>(SessionsSchemes.Registration, null)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<SessionsOptions>, TimeProvider, ILogger<SessionMessages>>(
                (bearer, sessions, clock, logger) =>
            {
                SessionsOptions settings = sessions.Value;
                bearer.TimeProvider = clock;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKeys = IssuerSigningKeys(settings),
                    ValidateLifetime = true,
                    ClockSkew = settings.ClockSkew,
                    RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                };
                bearer.Events = new JwtBearerEvents
                {
                    // The challenge body stays generic; the cause stays in this module's log.
                    OnAuthenticationFailed = failure =>
                    {
                        SessionsLog.BearerTokenRefused(logger, CauseOf(failure.Exception));
                        return Task.CompletedTask;
                    },
                    OnChallenge = challenge =>
                    {
                        challenge.HttpContext.Items[MoniPayHttpContextItems.AuthenticationProblemCode] =
                            MoniPayErrorTypes.SessionInvalid.Code;
                        challenge.Error = null;
                        challenge.ErrorDescription = null;
                        return Task.CompletedTask;
                    },
                };
            });
    }

    /// <summary>
    /// A bounded cause for a rejected access JWT. The framework's own diagnostic carries the
    /// validation exception, whose message can quote token content, so it is never logged here.
    /// </summary>
    private static string CauseOf(Exception? exception) => exception switch
    {
        SecurityTokenExpiredException => "expired",
        SecurityTokenNotYetValidException => "not-yet-valid",
        SecurityTokenInvalidSignatureException => "invalid-signature",
        SecurityTokenMalformedException => "malformed",
        null => "unknown",
        _ => "rejected",
    };

    private static IEnumerable<SecurityKey> IssuerSigningKeys(SessionsOptions sessions)
    {
        yield return new SymmetricSecurityKey(sessions.SigningKey);
        if (sessions.PreviousSigningKey is { } previous)
        {
            yield return new SymmetricSecurityKey(previous);
        }
    }

    /// <summary>
    /// The two named policies. <see cref="MoniPayPolicies.AuthenticatedUser"/> demands an active
    /// session behind the ticket; <see cref="MoniPayPolicies.Registration"/> demands the
    /// registration scheme's credential bound to the route's sign-up. Both answer a refusal of
    /// an authenticated principal with a 401, through the shared result handler.
    /// </summary>
    private static void AddAuthorization(IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(MoniPayPolicies.AuthenticatedUser, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new ActiveSessionRequirement()))
            .AddPolicy(MoniPayPolicies.Registration, policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(SessionsSchemes.Registration)
                .AddRequirements(new RegistrationRouteRequirement()));

        services.AddScoped<IAuthorizationHandler, SessionsAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, PolicyFailureResultHandler>();
    }

    /// <summary>The sign-up routes. The group carries the JSON:API and no-store markers; the start owns its IP limit.</summary>
    public static IEndpointRouteBuilder MapSessionsModule(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        RouteGroupBuilder signUps = routes
            .MapGroup(SignUpRoutes.Group)
            .WithTags(SignUpTags.SignUps)
            .WithMetadata(MoniPayConventions.JsonApi)
            .WithMetadata(MoniPayConventions.NoStore);

        signUps.MapStartSignUp();
        signUps.MapGetSignUp();
        signUps.MapCreateVerificationCodeDelivery();
        signUps.MapCreatePhoneVerification();

        // The refresh hangs off the root: its credential travels in the body, and the group it
        // would otherwise inherit is authenticated.
        routes.MapCreateSessionRefresh();

        RouteGroupBuilder sessions = routes
            .MapGroup(SessionRoutes.Group)
            .WithTags(SessionTags.Sessions)
            .WithMetadata(MoniPayConventions.JsonApi)
            .WithMetadata(MoniPayConventions.NoStore)
            .RequireAuthorization(MoniPayPolicies.AuthenticatedUser);

        sessions.MapGetCurrentSession();
        sessions.MapDeleteCurrentSession();

        return routes;
    }
}
