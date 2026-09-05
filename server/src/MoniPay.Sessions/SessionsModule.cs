using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel.Security;
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
                + "the lifetimes and the attempt and resend limits.")
            .RequireKey(options => options.VerificationCodeKeyBase64, SessionsOptions.Keys.VerificationCodeKeyBase64)
            .RequireKey(options => options.PersonalDataKeyBase64, SessionsOptions.Keys.PersonalDataKeyBase64)
            .ValidateOnStart();

        // All hold key material only; none holds request state.
        services.AddSingleton<VerificationCodeGenerator>();
        services.AddSingleton<VerificationCodeDigest>();
        services.AddSingleton<SignUpTokens>();
        services.AddSingleton<SignUpPersonalDataProtector>();
        services.AddSingleton<PhoneLookupDigest>();

        return services;
    }
}
