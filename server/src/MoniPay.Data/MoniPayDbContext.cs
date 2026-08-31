using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace MoniPay.Persistence;

/// <summary>
/// The single database context of the monolith. It owns no entity of its own: each module
/// contributes its own <c>IEntityTypeConfiguration</c>, and the composition root passes the
/// assemblies to scan. One context keeps a write across two modules inside one transaction.
/// </summary>
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

/// <summary>
/// The assemblies whose entity configurations compose the model. A module missing from this
/// list is a silently missing table the day it gains its first configuration, so the list is
/// injected rather than discovered.
/// </summary>
public sealed class ModelAssemblies(IReadOnlyList<Assembly> assemblies)
{
    public IReadOnlyList<Assembly> Assemblies { get; } = assemblies;
}
