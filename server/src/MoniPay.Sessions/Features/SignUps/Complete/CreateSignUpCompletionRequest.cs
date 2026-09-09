using System.Text.Json.Serialization;
using MoniPay.Kernel;
using MoniPay.Kernel.Validation;

namespace MoniPay.Sessions.Features.SignUps.Complete;

/// <summary>The JSON pointers the completion validation reports, as the HTTP contract names them.</summary>
internal static class CreateSignUpCompletionPointers
{
    public const string FirstName = "/data/attributes/firstName";

    public const string LastName = "/data/attributes/lastName";

    public const string Email = "/data/attributes/email";

    public const string DeviceId = "/data/attributes/deviceId";
}

/// <summary>
/// The attributes a completion carries. Unknown members are refused rather than ignored, so a
/// client that sneaks a passcode, a biometric flag or a provider field into this document is
/// answered with a 400 instead of having it silently dropped.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record CreateSignUpCompletionAttributes
{
    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string Email { get; init; }

    public required Guid DeviceId { get; init; }

    /// <summary>
    /// Validates the attributes and builds the command. Normalization belongs to the Kernel value
    /// types: the same spelling must reach the same lookup hash whatever the client sent.
    /// </summary>
    public CreateSignUpCompletionCommand Validate()
    {
        ValidationFailures failures = new();
        failures.Require(
            PersonName.TryNormalize(FirstName, out PersonName firstName),
            CreateSignUpCompletionPointers.FirstName,
            ValidationCodes.PersonNameInvalid);
        failures.Require(
            PersonName.TryNormalize(LastName, out PersonName lastName),
            CreateSignUpCompletionPointers.LastName,
            ValidationCodes.PersonNameInvalid);
        failures.Require(
            EmailAddress.TryNormalize(Email, out EmailAddress email),
            CreateSignUpCompletionPointers.Email,
            ValidationCodes.EmailInvalid);
        failures.Require(
            DeviceId != Guid.Empty,
            CreateSignUpCompletionPointers.DeviceId,
            ValidationCodes.DeviceIdRequired);

        if (failures.Any())
        {
            throw new ValidationException(failures);
        }

        return new(firstName, lastName, email, DeviceId);
    }
}
