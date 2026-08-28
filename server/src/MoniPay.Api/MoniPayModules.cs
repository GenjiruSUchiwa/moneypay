using System.Reflection;
using MoniPay.Kernel;
using MoniPay.Persistence;
using MoniPay.Wallet;
using MoniPay.Wallet.Endpoints;

namespace MoniPay;

/// <summary>
/// The composition of the monolith, one call per module. Each module registers its own services
/// in its <c>&lt;Module&gt;Module.cs</c> and maps its own routes from its <c>Endpoints/</c>
/// folder, so adding a module means adding a line to each of the three lists below — never an
/// edit inside another module.
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
        typeof(WalletModule).Assembly,
    ];

    /// <summary>The services, in dependency order.</summary>
    public static IServiceCollection AddMoniPayModules(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddKernelModule(configuration)
            .AddDataModule(configuration, ModuleAssemblies)
            .AddWalletModule(configuration);

    /// <summary>The routes. Health belongs to the host; everything else to its module.</summary>
    public static WebApplication MapMoniPayModules(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapWalletEndpoints();

        return app;
    }
}
