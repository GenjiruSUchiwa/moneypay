using MoniPay.Kernel;

namespace MoniPay.Sessions.Domain;

/// <summary>
/// One authenticated device session: the <c>sid</c> claim of every access token it issues and
/// the family every refresh token it rotates belongs to. <see cref="UserId"/> is a scalar: this
/// module never references the users table.
/// </summary>
internal sealed class Session
{
    private Session()
    {
    }

    public Guid Id { get; private set; }

    public UserId UserId { get; private set; }

    /// <summary>A label the client chose for its install; never a secret.</summary>
    public Guid DeviceId { get; private set; }

    /// <summary>The scope a refresh-token reuse revokes.</summary>
    public Guid TokenFamilyId { get; private set; }

    /// <summary>Optimistic concurrency. The aggregate bumps it on every persisted mutation.</summary>
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

    /// <summary>Records a successful refresh.</summary>
    public void Touch(DateTimeOffset now)
    {
        LastSeenAt = now;
        Version += 1;
    }

    /// <summary>Ends the session. A second call keeps the first reason and time.</summary>
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
