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
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Features.SignUps.Get;
using MoniPay.Sessions.Features.SignUps.ResendCode;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Features.SignUps.VerifyPhone;
using MoniPay.Sessions.Persistence;
using MoniPay.Sessions.Providers;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions;

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
            .PostConfigure(options => options.MapCountryRules())
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
                options => string.IsNullOrEmpty(options.PreviousSigningKeyBase64)
                    || Base64Key.IsValid(options.PreviousSigningKeyBase64),
                $"{SessionsOptions.Keys.PreviousSigningKeyBase64} must be empty or a base64-encoded 32-byte key.")
            .ValidateOnStart();

        services.AddSingleton<VerificationCodeGenerator>();
        services.AddSingleton<VerificationCodeDigest>();
        services.AddSingleton<SignUpTokens>();
        services.AddSingleton<SignUpPersonalDataProtector>();
        services.AddSingleton<PhoneLookupDigest>();
        services.AddSingleton<AccessTokenIssuer>();
        services.AddSingleton<RefreshTokenFactory>();

        services.AddScoped<SessionTokenService>();
        services.AddSingleton<VerificationCodeRenderer>();

        services.AddScoped<StartSignUpHandler>();
        services.AddScoped<GetSignUpHandler>();
        services.AddScoped<CreateVerificationCodeDeliveryHandler>();
        services.AddScoped<CreatePhoneVerificationHandler>();
        services.AddScoped<CreateSignUpCompletionHandler>();
        services.AddScoped<GetCurrentSessionHandler>();

        services.AddSingleton<ExpiredCredentialCleanupService>();
        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ExpiredCredentialCleanupService>());

        AddAuthentication(services);
        AddAuthorization(services);

        return services;
    }

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
        signUps.MapCreateSignUpCompletion();

        routes.MapCreateSessionRefresh();

        RouteGroupBuilder sessions = routes
            .MapGroup(SessionRoutes.Group)
            .WithTags(SessionTags.Sessions)
            .WithMetadata(MoniPayConventions.JsonApi)
            .WithMetadata(MoniPayConventions.NoStore)
            .RequireAuthorization(MoniPayPolicies.AuthenticatedUser)
            .RequireRateLimiting(MoniPayRateLimitPolicies.AuthenticatedRead);

        sessions.MapGetCurrentSession();
        sessions.MapDeleteCurrentSession();

        return routes;
    }
}
