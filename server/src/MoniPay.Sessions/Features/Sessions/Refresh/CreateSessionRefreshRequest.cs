using System.Buffers.Text;
using System.Text.Json.Serialization;
using MoniPay.Kernel.Validation;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions.Features.Sessions.Refresh;

/// <summary>The JSON pointers the refresh validation reports, as the HTTP contract names them.</summary>
internal static class CreateSessionRefreshPointers
{
    public const string RefreshToken = "/data/attributes/refreshToken";

    public const string DeviceId = "/data/attributes/deviceId";
}

/// <summary>
/// The attributes a refresh carries. Validation runs at the edge before the handler: a token in
/// the wrong shape is a malformed request, not an unknown credential, and it must not consume the
/// real one.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record CreateSessionRefreshAttributes
{
    public required string RefreshToken { get; init; }

    public required Guid DeviceId { get; init; }

    /// <summary>
    /// Validates the attributes. A refresh token is the base64url encoding of 32 bytes, so its
    /// length and alphabet decide its shape; an empty device identifier is a missing install
    /// label, not a session to rotate.
    /// </summary>
    public void Validate()
    {
        ValidationFailures failures = new();
        failures.Require(
            IsWellFormed(RefreshToken),
            CreateSessionRefreshPointers.RefreshToken,
            ValidationCodes.RefreshTokenFormatInvalid);
        failures.Require(
            DeviceId != Guid.Empty,
            CreateSessionRefreshPointers.DeviceId,
            ValidationCodes.DeviceIdRequired);

        if (failures.Any())
        {
            throw new ValidationException(failures);
        }
    }

    private static bool IsWellFormed(string? token) =>
        token is { Length: RefreshTokenFactory.RawLength } && Base64Url.IsValid(token);
}
