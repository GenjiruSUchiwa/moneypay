using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Persistence;
using MoniPay.Users.Features.Registration;
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
            .Validate(
                options => options.HasValidPersonalDataKey(),
                $"{UsersOptions.Keys.PersonalDataKeyBase64} must hold a base64-encoded 32-byte key.")
            .ValidateOnStart();

        // Both hold key material only; neither holds request state.
        services.AddSingleton<UserPersonalDataProtector>();
        services.AddSingleton<UserLookupDigest>();

        // The handler's dependencies are internal, so DI needs the factory: the container only
        // activates public constructors.
        services.AddScoped(provider => new RegisterUserHandler(
            provider.GetRequiredService<MoniPayDbContext>(),
            provider.GetRequiredService<UserPersonalDataProtector>(),
            provider.GetRequiredService<UserLookupDigest>()));

        return services;
    }
}
