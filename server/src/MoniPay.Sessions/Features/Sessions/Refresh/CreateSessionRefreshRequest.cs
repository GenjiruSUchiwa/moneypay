using System.Buffers.Text;
using System.Text.Json.Serialization;
using MoniPay.Kernel.Validation;
using MoniPay.Sessions.Security;

namespace MoniPay.Sessions.Features.Sessions.Refresh;

internal static class CreateSessionRefreshPointers
{
    public const string RefreshToken = "/data/attributes/refreshToken";

    public const string DeviceId = "/data/attributes/deviceId";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record CreateSessionRefreshAttributes
{
    public required string RefreshToken { get; init; }

    public required Guid DeviceId { get; init; }

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
