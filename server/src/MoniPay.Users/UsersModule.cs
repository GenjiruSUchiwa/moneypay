using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoniPay.Kernel.Security;
using MoniPay.Persistence;
using MoniPay.Users.Features.Registration;
using MoniPay.Users.Providers;
using MoniPay.Users.Security;

namespace MoniPay.Users;

/// <summary>
/// The composition of the user profile: the module's own services, registered by the module
/// itself. It maps no route yet.
/// </summary>
public static class UsersModule
{
    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<UsersOptions>()
            .Bind(configuration.GetSection(UsersOptions.SectionName))
            .RequireKey(options => options.PersonalDataKeyBase64, UsersOptions.Keys.PersonalDataKeyBase64)
            .ValidateOnStart();

        // Both hold key material only; neither holds request state.
        services.AddSingleton<UserPersonalDataProtector>();
        services.AddSingleton<UserLookupDigest>();
        services.AddSingleton<WelcomeMessageRenderer>();

        // The handler's dependencies are internal, so DI needs the factory: the container only
        // activates public constructors.
        services.AddScoped(provider => new RegisterUserHandler(
            provider.GetRequiredService<MoniPayDbContext>(),
            provider.GetRequiredService<UserPersonalDataProtector>(),
            provider.GetRequiredService<UserLookupDigest>(),
            provider.GetRequiredService<IWelcomeMessageSender>(),
            provider.GetRequiredService<WelcomeMessageRenderer>(),
            provider.GetRequiredService<ILogger<RegisterUserHandler>>()));

        // The lookup's dependency is internal, so it needs the factory as the handler does.
        services.AddScoped(provider => new PhoneRegistrationLookup(
            provider.GetRequiredService<MoniPayDbContext>(),
            provider.GetRequiredService<UserLookupDigest>()));

        return services;
    }
}
