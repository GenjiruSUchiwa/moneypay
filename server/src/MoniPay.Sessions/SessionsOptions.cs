using MoniPay.Kernel;

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

    /// <summary>The shortest and longest code length a workable configuration allows.</summary>
    public const int MinimumCodeLength = 4;
    public const int MaximumCodeLength = 8;

    /// <summary>
    /// The countries a sign-up may originate from: the calling code and the local digit count,
    /// the same six markets the iOS country list ships.
    /// </summary>
    public IReadOnlyList<CountryPhoneRule> SupportedCountries { get; set; } =
    [
        new("237", 9),
        new("225", 10),
        new("221", 9),
        new("241", 8),
        new("243", 9),
        new("229", 8),
    ];

    /// <summary>The number of ASCII digits a verification code carries.</summary>
    public int VerificationCodeLength { get; set; } = DefaultCodeLength;

    /// <summary>How long a verification code stays checkable.</summary>
    public TimeSpan VerificationCodeLifetime { get; set; } = DefaultCodeLifetime;

    /// <summary>The minimum delay between two code deliveries.</summary>
    public TimeSpan ResendCooldown { get; set; } = DefaultResendCooldown;

    /// <summary>How many times a code may be resent before the sign-up must be restarted.</summary>
    public int MaximumResends { get; set; } = DefaultMaximumResends;

    /// <summary>How many code checks a sign-up tolerates before it locks.</summary>
    public int MaximumVerificationAttempts { get; set; } = DefaultMaximumVerificationAttempts;

    /// <summary>The maximum lifetime of one sign-up, lock included.</summary>
    public TimeSpan SignUpLifetime { get; set; } = DefaultSignUpLifetime;

    /// <summary>Reports whether the configured bounds would produce a workable sign-up.</summary>
    public bool IsWithinBounds() =>
        SupportedCountries.Count > 0
        && VerificationCodeLength is >= MinimumCodeLength and <= MaximumCodeLength
        && VerificationCodeLifetime > TimeSpan.Zero
        && ResendCooldown >= TimeSpan.Zero
        && MaximumResends >= 0
        && MaximumVerificationAttempts >= 1
        && SignUpLifetime > TimeSpan.Zero;

    /// <summary>The configuration keys, declared so a mistyped key is a compile error.</summary>
    public static class Keys
    {
        public const string SupportedCountries = $"{SectionName}:{nameof(SupportedCountries)}";
        public const string VerificationCodeLength = $"{SectionName}:{nameof(VerificationCodeLength)}";
        public const string VerificationCodeLifetime = $"{SectionName}:{nameof(VerificationCodeLifetime)}";
        public const string ResendCooldown = $"{SectionName}:{nameof(ResendCooldown)}";
        public const string MaximumResends = $"{SectionName}:{nameof(MaximumResends)}";
        public const string MaximumVerificationAttempts = $"{SectionName}:{nameof(MaximumVerificationAttempts)}";
        public const string SignUpLifetime = $"{SectionName}:{nameof(SignUpLifetime)}";
    }
}
