using MoniPay.Kernel;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions.Domain;

internal sealed record SessionTokenResult(
    Guid SessionId,
    UserId UserId,
    AccessToken AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    public override string ToString() => nameof(SessionTokenResult);
}
