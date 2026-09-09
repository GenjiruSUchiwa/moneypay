using Microsoft.EntityFrameworkCore;
using MoniPay.Persistence;

namespace MoniPay.Api.Hosting;

public static class DatabaseStartup
{
    public static async Task ApplyMigrationsIfConfiguredAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Configuration.GetValue(MoniPayConfiguration.ApplyMigrationsOnStartup, defaultValue: false))
        {
            return;
        }

        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MoniPayDbContext>().Database.MigrateAsync();
    }
}
