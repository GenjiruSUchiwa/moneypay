using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MoniPay.Kernel;

/// <summary>The composition of the primitives every other module builds on.</summary>
public static class KernelModule
{
    public static IServiceCollection AddKernelModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // A module never reads the clock directly: tests substitute a FakeTimeProvider.
        services.TryAddSingletonTimeProvider();

        return services;
    }

    private static void TryAddSingletonTimeProvider(this IServiceCollection services)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(TimeProvider)))
        {
            return;
        }

        services.AddSingleton(TimeProvider.System);
    }
}
