using Microsoft.EntityFrameworkCore;
using MoniPay.Persistence;
using MoniPay.Users.Domain;

namespace MoniPay.Users.Persistence;

/// <summary>
/// The module's tables on the shared context. They are extension methods rather than
/// <c>DbSet</c> properties so that <see cref="MoniPayDbContext"/> stays free of any module type.
/// </summary>
internal static class UserSets
{
    public static DbSet<User> Users(this MoniPayDbContext database)
    {
        ArgumentNullException.ThrowIfNull(database);
        return database.Set<User>();
    }

    public static DbSet<UserConsent> UserConsents(this MoniPayDbContext database)
    {
        ArgumentNullException.ThrowIfNull(database);
        return database.Set<UserConsent>();
    }
}
