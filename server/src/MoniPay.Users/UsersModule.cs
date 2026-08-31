using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MoniPay.Users;

/// <summary>
/// The composition of the user profile: the module's own services, registered by the module
/// itself. It maps no route yet — this change carries the schema only.
/// </summary>
public static class UsersModule
{
    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services;
    }
}
