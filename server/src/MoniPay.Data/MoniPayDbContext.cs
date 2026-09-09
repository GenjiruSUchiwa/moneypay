using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace MoniPay.Persistence;

public class MoniPayDbContext(DbContextOptions<MoniPayDbContext> options, ModelAssemblies modelAssemblies)
    : DbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        KernelConversions.Apply(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        foreach (Assembly assembly in modelAssemblies.Assemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }
}

public sealed class ModelAssemblies(IReadOnlyList<Assembly> assemblies)
{
    public IReadOnlyList<Assembly> Assemblies { get; } = assemblies;
}
