using MoniPay.Kernel;
using MoniPay.Sessions.Domain;
using Xunit;

namespace MoniPay.Tests.Sessions.Domain;

public sealed class SessionTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Creating_a_session_starts_it_active_with_its_own_token_family()
    {
        UserId userId = UserId.New();
        Guid deviceId = Guid.CreateVersion7();

        Session session = Session.Create(userId, deviceId, CreatedAt);

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(deviceId, session.DeviceId);
        Assert.NotEqual(Guid.Empty, session.TokenFamilyId);
        Assert.NotEqual(session.Id, session.TokenFamilyId);
        Assert.Equal(1, session.Version);
        Assert.Equal(CreatedAt, session.CreatedAt);
        Assert.Equal(CreatedAt, session.LastSeenAt);
        Assert.Null(session.RevokedAt);
        Assert.Null(session.RevokeReason);
        Assert.True(session.IsActive);
    }

    [Fact]
    public void Touching_records_the_refresh_and_bumps_the_version()
    {
        Session session = Session.Create(UserId.New(), Guid.CreateVersion7(), CreatedAt);
        DateTimeOffset later = CreatedAt + TimeSpan.FromMinutes(9);

        session.Touch(later);

        Assert.Equal(later, session.LastSeenAt);
        Assert.Equal(CreatedAt, session.CreatedAt);
        Assert.Equal(2, session.Version);
    }

    [Fact]
    public void Revoking_ends_the_session_with_its_reason()
    {
        const SessionRevokeReason reason = SessionRevokeReason.RefreshTokenReuse;
        Session session = Session.Create(UserId.New(), Guid.CreateVersion7(), CreatedAt);
        DateTimeOffset revokedAt = CreatedAt + TimeSpan.FromHours(1);

        session.Revoke(reason, revokedAt);

        Assert.False(session.IsActive);
        Assert.Equal(revokedAt, session.RevokedAt);
        Assert.Equal(reason, session.RevokeReason);
        Assert.Equal(2, session.Version);
    }

    [Fact]
    public void Revoking_twice_keeps_the_first_reason_and_time()
    {
        Session session = Session.Create(UserId.New(), Guid.CreateVersion7(), CreatedAt);
        DateTimeOffset first = CreatedAt + TimeSpan.FromHours(1);
        session.Revoke(SessionRevokeReason.UserRequest, first);

        session.Revoke(SessionRevokeReason.RefreshTokenReuse, first + TimeSpan.FromHours(1));

        Assert.Equal(first, session.RevokedAt);
        Assert.Equal(SessionRevokeReason.UserRequest, session.RevokeReason);
        Assert.Equal(2, session.Version);
    }
}
