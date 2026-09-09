using Microsoft.EntityFrameworkCore;
using MoniPay.Persistence;
using MoniPay.Users.Domain;

namespace MoniPay.Users.Persistence;

internal static class UserSets
{
    extension(MoniPayDbContext database)
    {
        public DbSet<User> Users => database.Set<User>();

        public DbSet<UserConsent> UserConsents => database.Set<UserConsent>();
    }
}
