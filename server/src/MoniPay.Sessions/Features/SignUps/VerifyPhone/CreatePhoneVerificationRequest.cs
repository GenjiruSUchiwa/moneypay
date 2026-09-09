using System.Text.Json.Serialization;
using MoniPay.Kernel.Validation;

namespace MoniPay.Sessions.Features.SignUps.VerifyPhone;

/// <summary>The JSON pointer the verification validation reports, as the HTTP contract names it.</summary>
internal static class CreatePhoneVerificationPointers
{
    public const string VerificationCode = "/data/attributes/verificationCode";
}

/// <summary>
/// The attributes a phone verification carries. A code is a fixed-length run of ASCII digits, so
/// its shape is checked before the handler spends an attempt on it: a malformed code never
/// consumes the budget, and a leading zero is a code, not a number.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record CreatePhoneVerificationAttributes
{
    public required string VerificationCode { get; init; }

    /// <summary>Validates the attributes and returns the code, unchanged and untrimmed.</summary>
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
