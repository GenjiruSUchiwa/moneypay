using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MoniPay.Kernel;
using MoniPay.Sessions;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

/// <summary>
/// The access token is the only credential a session holds: identity claims, a lifetime from
/// configuration, and nothing that profiles the user — every profile fact is fetched, never
/// stamped into a token that cannot be revoked.
/// </summary>
public sealed class AccessTokenIssuerTests
{
    /// <summary>The real clock, truncated to the second: the time claims travel as epoch seconds.</summary>
    private static DateTimeOffset Now =>
        new(
            TimeProvider.System.GetUtcNow().Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond,
            TimeSpan.Zero);

    private static AccessTokenIssuer Issuer(TimeSpan lifetime) => new(Options.Create(new SessionsOptions
    {
        Issuer = TestKeys.Issuer,
        Audience = TestKeys.Audience,
        SigningKeyBase64 = TestKeys.Signing,
        AccessTokenLifetime = lifetime,
    }));

    private static async Task<TokenValidationResult> Validate(string jwt) =>
        await new JsonWebTokenHandler { MapInboundClaims = false }.ValidateTokenAsync(jwt, new TokenValidationParameters
        {
            ValidIssuer = TestKeys.Issuer,
            ValidAudience = TestKeys.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(TestKeys.Signing)),
        });

    [Fact]
    public async Task The_token_validates_and_carries_the_identity_claims()
    {
        UserId userId = UserId.New();
        Guid sessionId = Guid.CreateVersion7();

        AccessToken token = Issuer(TimeSpan.FromMinutes(10)).Issue(userId, sessionId, Now);
        TokenValidationResult validation = await Validate(token.Value);

        Assert.True(validation.IsValid);
        Assert.Equal(TestKeys.Issuer, validation.Claims["iss"]);
        Assert.Equal(TestKeys.Audience, validation.Claims["aud"]);
        Assert.Equal(userId.Value.ToString(), validation.Claims[MoniPayClaimTypes.Subject]);
        Assert.Equal(sessionId.ToString(), validation.Claims[MoniPayClaimTypes.SessionId]);
        Assert.True(Guid.TryParse((string)validation.Claims[MoniPayClaimTypes.TokenId], out _));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("phone_number")]
    [InlineData("email")]
    [InlineData("locale")]
    [InlineData("kyc_status")]
    [InlineData("role")]
    public async Task The_token_carries_no_profile_claim(string claim)
    {
        AccessToken token = Issuer(TimeSpan.FromMinutes(10)).Issue(UserId.New(), Guid.CreateVersion7(), Now);
        TokenValidationResult validation = await Validate(token.Value);

        Assert.True(validation.IsValid);
        Assert.False(validation.Claims.ContainsKey(claim));
    }

    [Fact]
    public async Task The_lifetime_comes_from_the_options()
    {
        TimeSpan lifetime = TimeSpan.FromMinutes(7);

        AccessToken token = Issuer(lifetime).Issue(UserId.New(), Guid.CreateVersion7(), Now);
        TokenValidationResult validation = await Validate(token.Value);

        DateTimeOffset issuedAt = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(validation.Claims["iat"]));
        DateTimeOffset notBefore = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(validation.Claims["nbf"]));
        DateTimeOffset expiresAt = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(validation.Claims["exp"]));
        Assert.Equal(Now, issuedAt);
        Assert.Equal(Now, notBefore);
        Assert.Equal(Now + lifetime, expiresAt);
    }

    [Fact]
    public async Task A_token_signed_with_another_key_is_refused()
    {
        AccessToken token = Issuer(TimeSpan.FromMinutes(10)).Issue(UserId.New(), Guid.CreateVersion7(), Now);

        TokenValidationResult validation = await new JsonWebTokenHandler { MapInboundClaims = false }
            .ValidateTokenAsync(
                token.Value,
                new TokenValidationParameters
                {
                    ValidIssuer = TestKeys.Issuer,
                    ValidAudience = TestKeys.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes("AnotherSigningKeyForTests00000")),
                });

        Assert.False(validation.IsValid);
    }

    [Fact]
    public void The_token_never_prints_itself()
    {
        AccessToken token = Issuer(TimeSpan.FromMinutes(10)).Issue(UserId.New(), Guid.CreateVersion7(), Now);

        Assert.Equal(nameof(AccessToken), token.ToString());
    }
}
