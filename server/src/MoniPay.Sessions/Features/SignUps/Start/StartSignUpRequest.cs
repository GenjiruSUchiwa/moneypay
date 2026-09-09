using System.Text.Json.Serialization;
using MoniPay.Kernel;
using MoniPay.Kernel.Validation;

namespace MoniPay.Sessions.Features.SignUps.Start;

/// <summary>The JSON pointers the start validation reports, as the HTTP contract names them.</summary>
internal static class StartSignUpPointers
{
    public const string Phone = "/data/attributes/phone";

    public const string TermsVersion = "/data/attributes/termsVersion";

    public const string PrivacyVersion = "/data/attributes/privacyVersion";
}

/// <summary>The attributes a start request carries. Validation runs at the edge before the handler.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record StartSignUpAttributes
{
    public required string Phone { get; init; }

    public required string TermsVersion { get; init; }

    public required string PrivacyVersion { get; init; }

    /// <summary>
    /// Checks the request attributes. A phone needs its configured country rules; legal versions must
    /// equal the published ones. An outdated legal version is a validation failure, not a conflict:
    /// the client must show the new document and send the new version.
    /// </summary>
    public ValidationFailures Validate(SessionsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ValidationFailures failures = new();
        ValidatePhone(options, failures);
        failures.Require(
            IsCurrent(TermsVersion, options.Legal.TermsVersion),
            StartSignUpPointers.TermsVersion,
            ValidationCodes.LegalVersionOutdated);
        failures.Require(
            IsCurrent(PrivacyVersion, options.Legal.PrivacyVersion),
            StartSignUpPointers.PrivacyVersion,
            ValidationCodes.LegalVersionOutdated);

        return failures;
    }

    private void ValidatePhone(SessionsOptions options, ValidationFailures failures)
    {
        if (PhoneNumber.TryNormalize(Phone, options.SupportedCountries, out _, out PhoneFailure failure))
        {
            return;
        }

        failures.Require(false, StartSignUpPointers.Phone, failure == PhoneFailure.CountryUnsupported
            ? ValidationCodes.PhoneCountryUnsupported
            : ValidationCodes.PhoneFormatInvalid);
    }

    private static bool IsCurrent(string value, string current) =>
        value.Length is > 0 and <= SessionsOptions.LegalOptions.MaximumVersionLength
        && string.Equals(value, current, StringComparison.Ordinal);
}
