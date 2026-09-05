using Microsoft.EntityFrameworkCore;
using MoniPay.Notifications.Domain;
using MoniPay.Persistence;

namespace MoniPay.Notifications.Persistence;

/// <summary>
/// The module's table on the shared context. It is an extension member rather than a
/// <c>DbSet</c> property so that <see cref="MoniPayDbContext"/> stays free of any module type.
/// </summary>
internal static class NotificationSets
{
    extension(MoniPayDbContext database)
    {
        public DbSet<Notification> Notifications => database.Set<Notification>();
    }
}
