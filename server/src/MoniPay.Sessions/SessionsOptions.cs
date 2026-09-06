using MoniPay.Kernel;
using MoniPay.Kernel.Security;

namespace MoniPay.Sessions;

/// <summary>
/// The module's configuration, bound from <c>MoniPay:Sessions</c>. The lock has no setting of
/// its own: a sign-up stays locked for the remainder of its lifetime, so
/// <see cref="SignUp"/> derives <c>LockedUntil</c> from <see cref="ExpiresAt"/>.
/// </summary>
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
    private static readonly TimeSpan DefaultAccessTokenLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DefaultRefreshTokenLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan DefaultClockSkew = TimeSpan.FromSeconds(30);

    /// <summary>The shortest and longest code length a workable configuration allows.</summary>
    public const int MinimumCodeLength = 4;
    public const int MaximumCodeLength = 8;

    /// <summary>
    /// The countries a sign-up may originate from: the calling code and the local digit count,
    /// the same six markets the iOS country list ships.
    /// </summary>
    public IReadOnlyList<CountryPhoneRule> SupportedCountries { get; set; } =
    [
        CountryPhoneRules.Cameroon,
        CountryPhoneRules.IvoryCoast,
        CountryPhoneRules.Senegal,
        CountryPhoneRules.Gabon,
        CountryPhoneRules.DrCongo,
        CountryPhoneRules.Benin,
    ];

    /// <summary>The number of ASCII digits a verification code carries.</summary>
    public int VerificationCodeLength { get; set; } = DefaultCodeLength;

    /// <summary>How long a verification code stays checkable.</summary>
    public TimeSpan VerificationCodeLifetime { get; set; } = DefaultCodeLifetime;

    /// <summary>The minimum delay between two code deliveries.</summary>
    public TimeSpan ResendCooldown { get; set; } = DefaultResendCooldown;

    /// <summary>
    /// How many times a code may be delivered again for one sign-up, by a resend or by a start
    /// that reopens it: the two share the budget, because both send a message.
    /// </summary>
    public int MaximumResends { get; set; } = DefaultMaximumResends;

    /// <summary>How many code checks a sign-up tolerates before it locks.</summary>
    public int MaximumVerificationAttempts { get; set; } = DefaultMaximumVerificationAttempts;

    /// <summary>The maximum lifetime of one sign-up, lock included.</summary>
    public TimeSpan SignUpLifetime { get; set; } = DefaultSignUpLifetime;

    /// <summary>
    /// The sliding window of the persistent per-phone start limit, shared with sign-in. The
    /// limit counts <c>sign_ups</c> rows by <c>created_at</c>, so cleanup must keep a closed row
    /// until <c>created_at</c> is older than this window, whatever its <c>expires_at</c>.
    /// </summary>
    public TimeSpan StartWindow { get; set; } = DefaultStartWindow;

    /// <summary>
    /// How many sign-ups one phone may open within <see cref="StartWindow"/>. Reopening the
    /// active one is not a new sign-up; <see cref="MaximumResends"/> limits that.
    /// </summary>
    public int MaximumStartsPerWindow { get; set; } = DefaultMaximumStartsPerWindow;

    /// <summary>How long an access token is accepted after it is issued.</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = DefaultAccessTokenLifetime;

    /// <summary>How long an unused refresh token stays exchangeable.</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = DefaultRefreshTokenLifetime;

    /// <summary>The tolerance token validation grants a drifting clock.</summary>
    public TimeSpan ClockSkew { get; set; } = DefaultClockSkew;

    /// <summary>How the expired-credential sweep runs.</summary>
    public CleanupOptions Cleanup { get; set; } = new();

    /// <summary>The 32-byte HMAC key hashing verification codes and workflow tokens, base64-encoded.</summary>
    public string VerificationCodeKeyBase64 { get; set; } = string.Empty;

    /// <summary>The 32-byte AES key protecting sign-up personal data, base64-encoded.</summary>
    public string PersonalDataKeyBase64 { get; set; } = string.Empty;

    /// <summary>The decoded verification-code key. Valid only once the options are validated.</summary>
    public byte[] VerificationCodeKey => Base64Key.Decode(VerificationCodeKeyBase64);

    /// <summary>The decoded personal-data key. Valid only once the options are validated.</summary>
    public byte[] PersonalDataKey => Base64Key.Decode(PersonalDataKeyBase64);

    /// <summary>Reports whether the configured bounds would produce a workable sign-up.</summary>
    public bool IsWithinBounds() =>
        HasWorkableVerificationBounds()
        && HasWorkableSignUpBounds()
        && HasWorkableTokenLifetimes()
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

    /// <summary>Every token lifetime must be positive; a zero skew is refused too, as the issue asks.</summary>
    private bool HasWorkableTokenLifetimes() =>
        AccessTokenLifetime > TimeSpan.Zero
        && RefreshTokenLifetime > TimeSpan.Zero
        && ClockSkew > TimeSpan.Zero;

    /// <summary>The configuration keys, declared so a mistyped key is a compile error.</summary>
    public static class Keys
    {
        private const string Cleanup = $"{SectionName}:{nameof(SessionsOptions.Cleanup)}";

        public const string SupportedCountries = $"{SectionName}:{nameof(SupportedCountries)}";
        public const string VerificationCodeLength = $"{SectionName}:{nameof(VerificationCodeLength)}";
        public const string VerificationCodeLifetime = $"{SectionName}:{nameof(VerificationCodeLifetime)}";
        public const string ResendCooldown = $"{SectionName}:{nameof(ResendCooldown)}";
        public const string MaximumResends = $"{SectionName}:{nameof(MaximumResends)}";
        public const string MaximumVerificationAttempts = $"{SectionName}:{nameof(MaximumVerificationAttempts)}";
        public const string SignUpLifetime = $"{SectionName}:{nameof(SignUpLifetime)}";
        public const string StartWindow = $"{SectionName}:{nameof(StartWindow)}";
        public const string MaximumStartsPerWindow = $"{SectionName}:{nameof(MaximumStartsPerWindow)}";
        public const string VerificationCodeKeyBase64 = $"{SectionName}:{nameof(VerificationCodeKeyBase64)}";
        public const string PersonalDataKeyBase64 = $"{SectionName}:{nameof(PersonalDataKeyBase64)}";
        public const string AccessTokenLifetime = $"{SectionName}:{nameof(AccessTokenLifetime)}";
        public const string RefreshTokenLifetime = $"{SectionName}:{nameof(RefreshTokenLifetime)}";
        public const string ClockSkew = $"{SectionName}:{nameof(ClockSkew)}";
        public const string CleanupEnabled = $"{Cleanup}:{nameof(CleanupOptions.Enabled)}";
        public const string CleanupInterval = $"{Cleanup}:{nameof(CleanupOptions.Interval)}";
        public const string CleanupBatchSize = $"{Cleanup}:{nameof(CleanupOptions.BatchSize)}";
    }
}
