namespace MoniPay.Sessions.Domain;

internal sealed class RefreshToken
{
    public const int DigestLength = 32;

    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid SessionId { get; private set; }

    public byte[] TokenDigest { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public Guid? ReplacedById { get; private set; }

    public bool IsConsumed => UsedAt is not null;

    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;

    public static RefreshToken Issue(Guid sessionId, byte[] digest, DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentNullException.ThrowIfNull(digest);
        if (digest.Length != DigestLength)
        {
            throw new ArgumentException($"A refresh-token digest is {DigestLength} bytes.", nameof(digest));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lifetime, TimeSpan.Zero);

        return new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            SessionId = sessionId,
            TokenDigest = digest,
            CreatedAt = now,
            ExpiresAt = now + lifetime,
        };
    }

    public void Consume(Guid replacementId, DateTimeOffset now)
    {
        if (IsConsumed)
        {
            throw new InvalidOperationException("The refresh token has already been consumed.");
        }

        UsedAt = now;
        ReplacedById = replacementId;
    }
}
