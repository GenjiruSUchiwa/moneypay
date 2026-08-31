using Microsoft.EntityFrameworkCore;
using MoniPay.Persistence;
using MoniPay.Users.Domain;

namespace MoniPay.Users.Persistence;

/// <summary>
/// The module's tables on the shared context. They are extension members rather than
/// <c>DbSet</c> properties so that <see cref="MoniPayDbContext"/> stays free of any module type.
/// </summary>
internal static class UserSets
{
    extension(MoniPayDbContext database)
    {
        public DbSet<User> Users => database.Set<User>();

        public DbSet<UserConsent> UserConsents => database.Set<UserConsent>();
    }
}
