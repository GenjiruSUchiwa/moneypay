using MoniPay.Kernel.Validation;
using Xunit;

namespace MoniPay.Tests.Kernel;

public sealed class ValidationFailuresTests
{
    [Fact]
    public void An_empty_collection_reports_no_failures()
    {
        ValidationFailures failures = new();

        Assert.False(failures.Any());
        Assert.Empty(failures);
    }

    [Fact]
    public void A_failed_requirement_preserves_its_pointer_and_code()
    {
        ValidationFailures failures = new();

        failures.Require(
            condition: false,
            pointer: "/data/attributes/phone",
            code: ValidationCodes.PhoneFormatInvalid);

        ValidationFailure failure = Assert.Single(failures);
        Assert.Equal("/data/attributes/phone", failure.Pointer);
        Assert.Equal(ValidationCodes.PhoneFormatInvalid, failure.Code);
        Assert.True(failures.Any());
    }

    [Fact]
    public void A_satisfied_requirement_does_not_add_a_failure()
    {
        ValidationFailures failures = new();

        failures.Require(
            condition: true,
            pointer: "/data/attributes/email",
            code: ValidationCodes.EmailInvalid);

        Assert.False(failures.Any());
        Assert.Empty(failures);
    }

    [Fact]
    public void Multiple_failed_requirements_are_enumerated_in_insertion_order()
    {
        ValidationFailures failures = new();

        failures.Require(false, "/data/attributes/firstName", ValidationCodes.PersonNameInvalid);
        failures.Require(false, "/data/attributes/email", ValidationCodes.EmailInvalid);

        ValidationFailure[] collected = failures.ToArray();
        Assert.Equal(2, collected.Length);
        Assert.True(failures.Count == collected.Length);
        Assert.Equal("/data/attributes/firstName", collected[0].Pointer);
        Assert.Equal(ValidationCodes.PersonNameInvalid, collected[0].Code);
        Assert.Equal("/data/attributes/email", collected[1].Pointer);
        Assert.Equal(ValidationCodes.EmailInvalid, collected[1].Code);
    }

    [Fact]
    public void A_validation_exception_freezes_the_failures_at_construction()
    {
        ValidationFailures failures = new();
        failures.Require(false, "/data/attributes/deviceId", ValidationCodes.DeviceIdRequired);

        ValidationException exception = new(failures);
        failures.Require(false, "/data/attributes/email", ValidationCodes.EmailInvalid);

        ValidationFailure failure = Assert.Single(exception.Failures);
        Assert.Equal("/data/attributes/deviceId", failure.Pointer);
        Assert.Equal(ValidationCodes.DeviceIdRequired, failure.Code);
    }

    [Fact]
    public void Validation_codes_match_the_attribute_validation_contract()
    {
        (string Actual, string Expected)[] codes =
        [
            (ValidationCodes.PhoneFormatInvalid, "phone-format-invalid"),
            (ValidationCodes.PhoneCountryUnsupported, "phone-country-unsupported"),
            (ValidationCodes.LegalVersionOutdated, "legal-version-outdated"),
            (ValidationCodes.VerificationCodeFormatInvalid, "verification-code-format-invalid"),
            (ValidationCodes.PersonNameInvalid, "person-name-invalid"),
            (ValidationCodes.EmailInvalid, "email-invalid"),
            (ValidationCodes.DeviceIdRequired, "device-id-required"),
            (ValidationCodes.RefreshTokenFormatInvalid, "refresh-token-format-invalid"),
        ];

        foreach ((string actual, string expected) in codes)
        {
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void An_empty_validation_code_is_rejected()
    {
        ValidationFailures failures = new();

        Assert.Throws<ArgumentException>(
            () => failures.Require(false, "/data/attributes/email", string.Empty));
    }
}
