using Microsoft.EntityFrameworkCore;
using MoniPay.Persistence;

namespace MoniPay.Api.Hosting;

/// <summary>Schema work the host does before it serves its first request.</summary>
public static class DatabaseStartup
{
    /// <summary>
    /// Applies pending migrations when configuration asks for it. Off by default: a host that
    /// migrates on every boot races its own replicas during a rolling deploy.
    /// </summary>
    public static async Task ApplyMigrationsIfConfiguredAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Configuration.GetValue(MoniPayConfiguration.ApplyMigrationsOnStartup, defaultValue: false))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MoniPayDbContext>().Database.MigrateAsync();
    }
}
