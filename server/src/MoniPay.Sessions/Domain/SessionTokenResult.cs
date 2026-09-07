using MoniPay.Kernel;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions.Domain;

/// <summary>
/// The credentials one successful session operation returns: the signed access token and the
/// fresh refresh token. The refresh token's raw value exists only here — it is returned once,
/// never persisted or logged, and only its digest is stored.
/// </summary>
internal sealed record SessionTokenResult(
    Guid SessionId,
    UserId UserId,
    AccessToken AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    // The generated record representation would expose the raw refresh token.
    public override string ToString() => nameof(SessionTokenResult);
}
