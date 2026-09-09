using MoniPay.Kernel;

namespace MoniPay.Sessions.Domain;

internal sealed class Session
{
    private Session()
    {
    }

    public Guid Id { get; private set; }

    public UserId UserId { get; private set; }

    public Guid DeviceId { get; private set; }

    public Guid TokenFamilyId { get; private set; }

    public long Version { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public SessionRevokeReason? RevokeReason { get; private set; }

    public bool IsActive => RevokedAt is null;

    public static Session Create(UserId userId, Guid deviceId, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        DeviceId = deviceId,
        TokenFamilyId = Guid.CreateVersion7(),
        Version = 1,
        CreatedAt = now,
        LastSeenAt = now,
    };

    public void Touch(DateTimeOffset now)
    {
        LastSeenAt = now;
        Version += 1;
    }

    public void Revoke(SessionRevokeReason reason, DateTimeOffset now)
    {
        if (!IsActive)
        {
            return;
        }

        RevokedAt = now;
        RevokeReason = reason;
        Version += 1;
    }
}
