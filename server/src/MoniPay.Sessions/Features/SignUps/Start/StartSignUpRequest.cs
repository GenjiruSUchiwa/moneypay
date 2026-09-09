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
    /// Validates and normalizes the request attributes. A phone needs its configured country rules;
    /// legal versions must equal the published ones. An outdated legal version is a validation
    /// failure, not a conflict: the client must show the new document and send the new version.
    /// </summary>
    public StartSignUpCommand Validate(SessionsOptions options, Locale locale)
    {
        ArgumentNullException.ThrowIfNull(options);

        ValidationFailures failures = new();
        if (!PhoneNumber.TryNormalize(Phone, options.SupportedCountries, out PhoneNumber phone, out PhoneFailure failure))
        {
            failures.Require(false, StartSignUpPointers.Phone, failure == PhoneFailure.CountryUnsupported
                ? ValidationCodes.PhoneCountryUnsupported
                : ValidationCodes.PhoneFormatInvalid);
        }

        failures.Require(
            IsCurrent(TermsVersion, options.Legal.TermsVersion),
            StartSignUpPointers.TermsVersion,
            ValidationCodes.LegalVersionOutdated);
        failures.Require(
            IsCurrent(PrivacyVersion, options.Legal.PrivacyVersion),
            StartSignUpPointers.PrivacyVersion,
            ValidationCodes.LegalVersionOutdated);

        if (failures.Any())
        {
            throw new ValidationException(failures);
        }

        return new(phone, locale, TermsVersion, PrivacyVersion);
    }

    private static bool IsCurrent(string? value, string current) =>
        value is { Length: > 0 and <= SessionsOptions.LegalOptions.MaximumVersionLength }
        && string.Equals(value, current, StringComparison.Ordinal);
}
