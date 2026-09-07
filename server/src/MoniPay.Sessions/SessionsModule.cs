using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel.Security;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Persistence;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions;

/// <summary>
/// The composition of the sign-up and session machinery: the module's own services, registered
/// by the module itself. It maps no route yet; the endpoints arrive with their slices, and the
/// sign-up handlers are registered with them, once the host also provides the two ports they
/// depend on (<c>IVerificationCodeSender</c>, <c>IRegisteredPhoneLookup</c>). Registering the
/// handlers before their ports would fail a Development host at startup.
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
                + "the lifetimes, the attempt and resend limits and the cleanup bounds.")
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

        services.AddSingleton<ExpiredCredentialCleanupService>();
        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ExpiredCredentialCleanupService>());

        return services;
    }
}
