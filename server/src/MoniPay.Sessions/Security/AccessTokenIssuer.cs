using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MoniPay.Kernel;
using MoniPay.Kernel.Security;

namespace MoniPay.Sessions.Security;

internal sealed class AccessTokenIssuer(IOptions<SessionsOptions> options)
{
    private readonly SessionsOptions settings = options.Value;
    private readonly JsonWebTokenHandler handler = new();

    public AccessToken Issue(UserId userId, Guid sessionId, DateTimeOffset now)
    {
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            NotBefore = now.UtcDateTime,
            IssuedAt = now.UtcDateTime,
            Expires = (now + settings.AccessTokenLifetime).UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(settings.SigningKey), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [MoniPayClaimTypes.Subject] = userId.Value.ToString(),
                [MoniPayClaimTypes.SessionId] = sessionId.ToString(),
                [MoniPayClaimTypes.TokenId] = Guid.CreateVersion7().ToString(),
            },
        };

        return new AccessToken(handler.CreateToken(descriptor));
    }
}
