using Microsoft.EntityFrameworkCore;
using MoniPay.Notifications.Domain;
using MoniPay.Persistence;

namespace MoniPay.Notifications.Persistence;

internal static class NotificationSets
{
    extension(MoniPayDbContext database)
    {
        public DbSet<Notification> Notifications => database.Set<Notification>();
    }
}
