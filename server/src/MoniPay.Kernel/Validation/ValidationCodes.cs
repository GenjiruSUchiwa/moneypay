namespace MoniPay.Kernel.Validation;

/// <summary>Stable codes used to describe invalid request attributes.</summary>
public static class ValidationCodes
{
    /// <summary>The phone value is not a digits-only value in the accepted length range.</summary>
    public const string PhoneFormatInvalid = "phone-format-invalid";

    /// <summary>The phone calling code or local length is not supported.</summary>
    public const string PhoneCountryUnsupported = "phone-country-unsupported";

    /// <summary>A legal document version is missing, too long or no longer current.</summary>
    public const string LegalVersionOutdated = "legal-version-outdated";

    /// <summary>The verification code is not the expected ASCII digit sequence.</summary>
    public const string VerificationCodeFormatInvalid = "verification-code-format-invalid";

    /// <summary>A person name does not satisfy the accepted character and length rules.</summary>
    public const string PersonNameInvalid = "person-name-invalid";

    /// <summary>The email address does not satisfy the accepted format rules.</summary>
    public const string EmailInvalid = "email-invalid";

    /// <summary>The request does not contain a non-empty device identifier.</summary>
    public const string DeviceIdRequired = "device-id-required";

    /// <summary>The refresh token is not the expected base64url value.</summary>
    public const string RefreshTokenFormatInvalid = "refresh-token-format-invalid";
}
