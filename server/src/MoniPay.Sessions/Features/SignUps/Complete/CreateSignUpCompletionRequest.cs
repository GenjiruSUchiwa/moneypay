using System.Text.Json.Serialization;
using MoniPay.Kernel;
using MoniPay.Kernel.Validation;

namespace MoniPay.Sessions.Features.SignUps.Complete;

internal static class CreateSignUpCompletionPointers
{
    public const string FirstName = "/data/attributes/firstName";

    public const string LastName = "/data/attributes/lastName";

    public const string Email = "/data/attributes/email";

    public const string DeviceId = "/data/attributes/deviceId";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record CreateSignUpCompletionAttributes
{
    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string Email { get; init; }

    public required Guid DeviceId { get; init; }

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
