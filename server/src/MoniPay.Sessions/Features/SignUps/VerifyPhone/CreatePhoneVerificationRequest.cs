using System.Text.Json.Serialization;
using MoniPay.Kernel.Validation;

namespace MoniPay.Sessions.Features.SignUps.VerifyPhone;

internal static class CreatePhoneVerificationPointers
{
    public const string VerificationCode = "/data/attributes/verificationCode";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record CreatePhoneVerificationAttributes
{
    public required string VerificationCode { get; init; }

    public string Validate(SessionsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ValidationFailures failures = new();
        failures.Require(
            IsCode(VerificationCode, options.VerificationCodeLength),
            CreatePhoneVerificationPointers.VerificationCode,
            ValidationCodes.VerificationCodeFormatInvalid);

        if (failures.Any())
        {
            throw new ValidationException(failures);
        }

        return VerificationCode;
    }

    private static bool IsCode(string? value, int length) =>
        value is not null && value.Length == length && value.All(character => character is >= '0' and <= '9');
}
