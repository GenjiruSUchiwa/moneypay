using System.Buffers.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MoniPay.Kernel;

namespace MoniPay.Tests.Support;

internal static class TestTokens
{
    public static string Bearer(
        UserId subject,
        Guid sessionId,
        byte[]? key = null,
        string issuer = TestKeys.Issuer,
        string audience = TestKeys.Audience,
        DateTimeOffset? expires = null) =>
        BearerWithClaims(
            new Dictionary<string, object>
            {
                [MoniPayClaimTypes.Subject] = subject.Value.ToString(),
                [MoniPayClaimTypes.SessionId] = sessionId.ToString(),
                [MoniPayClaimTypes.TokenId] = Guid.CreateVersion7().ToString(),
            },
            key,
            issuer,
            audience,
            expires);

    public static string Unsigned(UserId? subject = null)
    {
        string header = Base64Url.EncodeToString("""{"alg":"none","typ":"JWT"}"""u8);
        string payload = Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            [MoniPayClaimTypes.Subject] = (subject ?? UserId.New()).Value.ToString(),
            [MoniPayClaimTypes.SessionId] = Guid.CreateVersion7().ToString(),
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds(),
        }));

        return $"{header}.{payload}.";
    }

    public static string BearerWithClaims(
        IReadOnlyDictionary<string, object> claims,
        byte[]? key = null,
        string issuer = TestKeys.Issuer,
        string audience = TestKeys.Audience,
        DateTimeOffset? expires = null)
    {
        ArgumentNullException.ThrowIfNull(claims);

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = issuer,
            Audience = audience,
            NotBefore = DateTime.UtcNow.AddMinutes(-10),
            IssuedAt = DateTime.UtcNow.AddMinutes(-10),
            Expires = (expires ?? DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5)).UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key ?? Convert.FromBase64String(TestKeys.Signing)),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>(claims, StringComparer.Ordinal),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
