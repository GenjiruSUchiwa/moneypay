using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MoniPay.Wallet;

/// <summary>
/// The composition of the FCFA ledger: the module's own services, registered by the module
/// itself. The host calls this and <c>MapWalletEndpoints</c>; it never registers a wallet
/// service directly, so a service added here needs no edit to <c>Program.cs</c>.
/// </summary>
public static class WalletModule
{
    public static IServiceCollection AddWalletModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services;
    }
}
