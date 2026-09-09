using Microsoft.EntityFrameworkCore;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Persistence;

internal static class SessionSets
{
    extension(MoniPayDbContext database)
    {
        public DbSet<SignUp> SignUps => database.Set<SignUp>();

        public DbSet<Session> Sessions => database.Set<Session>();

        public DbSet<RefreshToken> RefreshTokens => database.Set<RefreshToken>();
    }
}
