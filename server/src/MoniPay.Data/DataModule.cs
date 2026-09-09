using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MoniPay.Kernel;

namespace MoniPay.Persistence;

public static class DataModule
{
    public const string ConnectionStringName = "MoniPay";

    public static IServiceCollection AddDataModule(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] moduleAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(moduleAssemblies);

        services.TryAddSingleton(new ModelAssemblies(moduleAssemblies));
        services.AddDbContext<MoniPayDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString(ConnectionStringName));

            foreach (IDbContextOptionsContributor contributor
                in serviceProvider.GetServices<IDbContextOptionsContributor>())
            {
                contributor.Contribute(options);
            }
        });

        return services;
    }
}
