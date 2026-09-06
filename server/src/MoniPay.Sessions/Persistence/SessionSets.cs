using Microsoft.EntityFrameworkCore;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Persistence;

/// <summary>
/// The module's tables on the shared context. They are extension members rather than
/// <c>DbSet</c> properties so that <see cref="MoniPayDbContext"/> stays free of any module type.
/// </summary>
internal static class SessionSets
{
    extension(MoniPayDbContext database)
    {
        public DbSet<SignUp> SignUps => database.Set<SignUp>();

        public DbSet<Session> Sessions => database.Set<Session>();

        public DbSet<RefreshToken> RefreshTokens => database.Set<RefreshToken>();
    }
}
