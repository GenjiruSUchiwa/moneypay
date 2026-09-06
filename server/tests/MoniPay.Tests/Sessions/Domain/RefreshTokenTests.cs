using System.Security.Cryptography;
using MoniPay.Sessions.Domain;
using Xunit;

namespace MoniPay.Tests.Sessions.Domain;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset IssuedAt = new(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);
    private static readonly byte[] Digest = SHA256.HashData("raw-refresh-token"u8);

    [Fact]
    public void Issuing_stores_the_digest_and_the_absolute_expiry_only()
    {
        Guid sessionId = Guid.CreateVersion7();

        RefreshToken token = RefreshToken.Issue(sessionId, Digest, IssuedAt, Lifetime);

        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.Equal(sessionId, token.SessionId);
        Assert.Equal(Digest, token.TokenDigest);
        Assert.Equal(IssuedAt, token.CreatedAt);
        Assert.Equal(IssuedAt + Lifetime, token.ExpiresAt);
        Assert.Null(token.UsedAt);
        Assert.Null(token.ReplacedById);
        Assert.False(token.IsConsumed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(33)]
    public void Issuing_refuses_anything_but_a_sha256_digest(int length)
    {
        Assert.Throws<ArgumentException>(
            () => RefreshToken.Issue(Guid.CreateVersion7(), new byte[length], IssuedAt, Lifetime));
    }

    [Fact]
    public void Issuing_refuses_a_non_positive_lifetime()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RefreshToken.Issue(Guid.CreateVersion7(), Digest, IssuedAt, TimeSpan.Zero));
    }

    [Fact]
    public void A_token_expires_at_its_expiry_and_not_before()
    {
        RefreshToken token = RefreshToken.Issue(Guid.CreateVersion7(), Digest, IssuedAt, Lifetime);

        Assert.False(token.IsExpired(token.ExpiresAt - TimeSpan.FromSeconds(1)));
        Assert.True(token.IsExpired(token.ExpiresAt));
    }

    [Fact]
    public void Consuming_keeps_the_digest_so_a_replay_is_detectable()
    {
        RefreshToken token = RefreshToken.Issue(Guid.CreateVersion7(), Digest, IssuedAt, Lifetime);
        Guid replacementId = Guid.CreateVersion7();
        DateTimeOffset usedAt = IssuedAt + TimeSpan.FromHours(2);

        token.Consume(replacementId, usedAt);

        Assert.True(token.IsConsumed);
        Assert.Equal(usedAt, token.UsedAt);
        Assert.Equal(replacementId, token.ReplacedById);
        Assert.Equal(Digest, token.TokenDigest);
    }

    [Fact]
    public void Consuming_twice_is_refused_and_keeps_the_first_rotation()
    {
        RefreshToken token = RefreshToken.Issue(Guid.CreateVersion7(), Digest, IssuedAt, Lifetime);
        Guid replacementId = Guid.CreateVersion7();
        token.Consume(replacementId, IssuedAt + TimeSpan.FromHours(2));

        Assert.Throws<InvalidOperationException>(
            () => token.Consume(Guid.CreateVersion7(), IssuedAt + TimeSpan.FromHours(3)));

        Assert.Equal(replacementId, token.ReplacedById);
        Assert.Equal(IssuedAt + TimeSpan.FromHours(2), token.UsedAt);
    }
}
