using MoniPay.Kernel;
using MoniPay.Kernel.Security;

namespace MoniPay.Sessions;

internal sealed class SessionsOptions
{
    public const string SectionName = "MoniPay:Sessions";

    private const int DefaultCodeLength = 6;
    private static readonly TimeSpan DefaultCodeLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultResendCooldown = TimeSpan.FromSeconds(60);
    private const int DefaultMaximumResends = 3;
    private const int DefaultMaximumVerificationAttempts = 5;
    private static readonly TimeSpan DefaultSignUpLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DefaultStartWindow = TimeSpan.FromHours(1);
    private const int DefaultMaximumStartsPerWindow = 5;

    public const int MinimumCodeLength = 4;
    public const int MaximumCodeLength = 8;

    public IReadOnlyList<CountryPhoneRule> SupportedCountries { get; set; } =
    [
        CountryPhoneRules.Cameroon,
        CountryPhoneRules.IvoryCoast,
        CountryPhoneRules.Senegal,
        CountryPhoneRules.Gabon,
        CountryPhoneRules.DrCongo,
        CountryPhoneRules.Benin,
    ];

    public int VerificationCodeLength { get; set; } = DefaultCodeLength;

    public TimeSpan VerificationCodeLifetime { get; set; } = DefaultCodeLifetime;

    public TimeSpan ResendCooldown { get; set; } = DefaultResendCooldown;

    public int MaximumResends { get; set; } = DefaultMaximumResends;

    public int MaximumVerificationAttempts { get; set; } = DefaultMaximumVerificationAttempts;

    public TimeSpan SignUpLifetime { get; set; } = DefaultSignUpLifetime;

    public TimeSpan StartWindow { get; set; } = DefaultStartWindow;

    public int MaximumStartsPerWindow { get; set; } = DefaultMaximumStartsPerWindow;

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(30);

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKeyBase64 { get; set; } = string.Empty;

    public string? PreviousSigningKeyBase64 { get; set; }

    public CleanupOptions Cleanup { get; set; } = new();

    public LegalOptions Legal { get; set; } = new();

    public string VerificationCodeKeyBase64 { get; set; } = string.Empty;

    public string PersonalDataKeyBase64 { get; set; } = string.Empty;

    public byte[] VerificationCodeKey => Base64Key.Decode(VerificationCodeKeyBase64);

    public byte[] PersonalDataKey => Base64Key.Decode(PersonalDataKeyBase64);

    public byte[] SigningKey => Base64Key.Decode(SigningKeyBase64);

    public byte[]? PreviousSigningKey =>
        PreviousSigningKeyBase64 is null ? null : Base64Key.Decode(PreviousSigningKeyBase64);

    public bool IsWithinBounds() =>
        HasWorkableVerificationBounds()
        && HasWorkableSignUpBounds()
        && HasWorkableTokenLifetimes()
        && Legal.IsWithinBounds()
        && Cleanup.IsWithinBounds();

    private bool HasWorkableVerificationBounds() =>
        SupportedCountries.Count > 0
        && VerificationCodeLength is >= MinimumCodeLength and <= MaximumCodeLength
        && VerificationCodeLifetime > TimeSpan.Zero
        && ResendCooldown >= TimeSpan.Zero
        && MaximumResends >= 0
        && MaximumVerificationAttempts >= 1;

    private bool HasWorkableSignUpBounds() =>
        SignUpLifetime > TimeSpan.Zero
        && StartWindow > TimeSpan.Zero
        && MaximumStartsPerWindow >= 1;

    private bool HasWorkableTokenLifetimes() =>
        AccessTokenLifetime > TimeSpan.Zero
        && RefreshTokenLifetime > TimeSpan.Zero
        && ClockSkew > TimeSpan.Zero;

    public bool HasWorkableTokenIssuer() =>
        Issuer.Length > 0 && Audience.Length > 0;

    public sealed class LegalOptions
    {
        public const int MaximumVersionLength = 64;

        public string TermsVersion { get; set; } = string.Empty;

        public string PrivacyVersion { get; set; } = string.Empty;

        public bool IsWithinBounds() =>
            IsVersion(TermsVersion) && IsVersion(PrivacyVersion);

        private static bool IsVersion(string value) =>
            value.Length is > 0 and <= MaximumVersionLength;
    }

    public static class Keys
    {
        private const string Cleanup = $"{SectionName}:{nameof(SessionsOptions.Cleanup)}";
        private const string Legal = $"{SectionName}:{nameof(SessionsOptions.Legal)}";

        public const string SupportedCountries = $"{SectionName}:{nameof(SupportedCountries)}";
        public const string VerificationCodeLength = $"{SectionName}:{nameof(VerificationCodeLength)}";
        public const string VerificationCodeLifetime = $"{SectionName}:{nameof(VerificationCodeLifetime)}";
        public const string ResendCooldown = $"{SectionName}:{nameof(ResendCooldown)}";
        public const string MaximumResends = $"{SectionName}:{nameof(MaximumResends)}";
        public const string MaximumVerificationAttempts = $"{SectionName}:{nameof(MaximumVerificationAttempts)}";
        public const string SignUpLifetime = $"{SectionName}:{nameof(SignUpLifetime)}";
        public const string StartWindow = $"{SectionName}:{nameof(StartWindow)}";
        public const string MaximumStartsPerWindow = $"{SectionName}:{nameof(MaximumStartsPerWindow)}";
        public const string Issuer = $"{SectionName}:{nameof(Issuer)}";
        public const string Audience = $"{SectionName}:{nameof(Audience)}";
        public const string SigningKeyBase64 = $"{SectionName}:{nameof(SigningKeyBase64)}";
        public const string PreviousSigningKeyBase64 = $"{SectionName}:{nameof(PreviousSigningKeyBase64)}";
        public const string VerificationCodeKeyBase64 = $"{SectionName}:{nameof(VerificationCodeKeyBase64)}";
        public const string PersonalDataKeyBase64 = $"{SectionName}:{nameof(PersonalDataKeyBase64)}";
        public const string AccessTokenLifetime = $"{SectionName}:{nameof(AccessTokenLifetime)}";
        public const string RefreshTokenLifetime = $"{SectionName}:{nameof(RefreshTokenLifetime)}";
        public const string ClockSkew = $"{SectionName}:{nameof(ClockSkew)}";
        public const string CleanupEnabled = $"{Cleanup}:{nameof(CleanupOptions.Enabled)}";
        public const string CleanupInterval = $"{Cleanup}:{nameof(CleanupOptions.Interval)}";
        public const string CleanupBatchSize = $"{Cleanup}:{nameof(CleanupOptions.BatchSize)}";
        public const string LegalTermsVersion = $"{Legal}:{nameof(LegalOptions.TermsVersion)}";
        public const string LegalPrivacyVersion = $"{Legal}:{nameof(LegalOptions.PrivacyVersion)}";
    }
}
