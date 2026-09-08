using System.Reflection;
using MoniPay.Api.Composition;
using MoniPay.Kernel;
using MoniPay.Notifications;
using MoniPay.Persistence;
using MoniPay.Sessions;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Ports;
using MoniPay.Users;
using MoniPay.Wallet;
using MoniPay.Wallet.Endpoints;

namespace MoniPay;

/// <summary>
/// The composition of the monolith, one call per module. Each module registers its own services
/// in its <c>&lt;Module&gt;Module.cs</c> and maps its own routes from its <c>Endpoints/</c>
/// folder, so adding a module means a line in the assembly list and in the composition chain
/// below — plus a route mapping when it has endpoints — never an edit inside another module.
/// </summary>
internal static class MoniPayModules
{
    /// <summary>
    /// The assemblies whose entity configurations compose the model: every module, whether or
    /// not it maps an entity today. A module missing from this list is a silently missing table
    /// the day it gains its first configuration.
    /// </summary>
    internal static readonly Assembly[] ModuleAssemblies =
    [
        typeof(SessionsModule).Assembly,
        typeof(UsersModule).Assembly,
        typeof(WalletModule).Assembly,
        typeof(NotificationsModule).Assembly,
    ];

    /// <summary>The services, in dependency order.</summary>
    public static IServiceCollection AddMoniPayModules(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddKernelModule(configuration)
            .AddDataModule(configuration, ModuleAssemblies)
            .AddSessionsModule(configuration)
            .AddUsersModule(configuration)
            .AddWalletModule(configuration)
            .AddNotificationsModule(configuration)
            // The cross-module composition the modules cannot do themselves: the only host code naming two modules.
            .AddScoped<IUserProvisioning, UserProvisioningAdapter>()
            .AddScoped<IRegisteredPhoneLookup, RegisteredPhoneLookupAdapter>();
    /// <summary>The routes. Health belongs to the host; everything else to its module.</summary>
    public static WebApplication MapMoniPayModules(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapWalletEndpoints();

        return app;
    }
}
