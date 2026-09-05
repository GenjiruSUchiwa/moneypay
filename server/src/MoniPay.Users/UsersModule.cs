using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel.Security;
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

        return services;
    }
}
