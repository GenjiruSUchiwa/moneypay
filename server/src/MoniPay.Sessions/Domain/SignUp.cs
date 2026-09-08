using System.Security.Cryptography;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;

namespace MoniPay.Sessions.Domain;

/// <summary>
/// One phone-verification workflow. The entity protects every transition; handlers coordinate
/// I/O and transactions, and never inspect <see cref="Status"/> themselves. The caller hands in
/// digests and ciphertexts it produced; the aggregate never sees a plaintext code or phone.
/// </summary>
internal sealed class SignUp
{
    private SignUp()
    {
    }

    public SignUpId Id { get; private set; }

    public Ciphertext PhoneCiphertext { get; private set; }

    public LookupHash PhoneLookupHash { get; private set; }

    public Locale Locale { get; private set; }

    public byte[]? CodeDigest { get; private set; }

    public DateTimeOffset? CodeExpiresAt { get; private set; }

    public byte[]? SignUpTokenDigest { get; private set; }

    public byte[]? RegistrationTokenDigest { get; private set; }

    public SignUpStatus Status { get; private set; }

    /// <summary>Never reset by a resend: resending must not become a brute-force channel.</summary>
    public int FailedAttempts { get; private set; }

    public int ResendCount { get; private set; }

    public DateTimeOffset CanResendAt { get; private set; }

    /// <summary>Set when the attempt limit is reached; the source of <c>Retry-After</c>.</summary>
    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public string TermsVersion { get; private set; } = string.Empty;

    public string PrivacyVersion { get; private set; } = string.Empty;

    public UserId? ProvisionedUserId { get; private set; }

    public Guid? BootstrapSessionId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? VerifiedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Optimistic concurrency. The aggregate bumps it on every persisted mutation.</summary>
    public long Version { get; private set; }

    /// <summary>
    /// Opens a sign-up in the <see cref="SignUpStatus.CodePending"/> state: the code digest is
    /// stored before delivery so a crash cannot deliver an unstorable code.
    /// </summary>
    public static SignUp Start(
        SignUpId id,
        Ciphertext phoneCiphertext,
        LookupHash phoneLookupHash,
        Locale locale,
        string termsVersion,
        string privacyVersion,
        byte[] codeDigest,
        byte[] signUpTokenDigest,
        DateTimeOffset now,
        SessionsOptions options)
    {
        ArgumentNullException.ThrowIfNull(termsVersion);
        ArgumentNullException.ThrowIfNull(privacyVersion);
        ArgumentNullException.ThrowIfNull(codeDigest);
        ArgumentNullException.ThrowIfNull(signUpTokenDigest);
        ArgumentNullException.ThrowIfNull(options);

        return new SignUp
        {
            Id = id,
            PhoneCiphertext = phoneCiphertext,
            PhoneLookupHash = phoneLookupHash,
            Locale = locale,
            TermsVersion = termsVersion,
            PrivacyVersion = privacyVersion,
            CodeDigest = codeDigest,
            CodeExpiresAt = now + options.VerificationCodeLifetime,
            SignUpTokenDigest = signUpTokenDigest,
            Status = SignUpStatus.CodePending,
            FailedAttempts = 0,
            ResendCount = 0,
            CanResendAt = now + options.ResendCooldown,
            ExpiresAt = now + options.SignUpLifetime,
            CreatedAt = now,
            Version = 1,
        };
    }

    /// <summary>
    /// Replaces the verification code. The failed-attempt count is untouched: resending must
    /// not reset the attacker's budget. A resend past the limit names the delay to the sign-up's
    /// expiry: no resend will ever succeed on this row, but a fresh start will once it is closed.
    /// </summary>
    public void RotateVerificationCode(byte[] newDigest, DateTimeOffset now, SessionsOptions options)
    {
        ArgumentNullException.ThrowIfNull(newDigest);
        ArgumentNullException.ThrowIfNull(options);

        RefuseUnlessAlive(now);
        if (Status != SignUpStatus.CodePending)
        {
            throw new RefusalException(MoniPayErrorTypes.SignUpStateInvalid);
        }

        if (now < CanResendAt)
        {
            throw new RefusalException(MoniPayErrorTypes.SignUpResendTooSoon, retryAfter: CanResendAt - now);
        }

        if (ResendCount >= options.MaximumResends)
        {
            throw new RefusalException(MoniPayErrorTypes.SignUpResendLimit, retryAfter: ExpiresAt - now);
        }

        CodeDigest = newDigest;
        CodeExpiresAt = now + options.VerificationCodeLifetime;
        ResendCount += 1;
        CanResendAt = now + options.ResendCooldown;
        Version += 1;
    }

    /// <summary>
    /// A start for a phone whose sign-up is in flight: the code is rotated under the resend rules
    /// and the sign-up token is replaced. A locked sign-up answers with its retry delay, because
    /// a fresh start will succeed once the lock has expired with the row.
    /// </summary>
    public void Restart(byte[] codeDigest, byte[] signUpTokenDigest, DateTimeOffset now, SessionsOptions options)
    {
        ArgumentNullException.ThrowIfNull(signUpTokenDigest);

        RefuseUnlessCodePending(now);
        RotateVerificationCode(codeDigest, now, options);
        SignUpTokenDigest = signUpTokenDigest;
    }

    public bool TryExpire(DateTimeOffset now)
    {
        if (Status is SignUpStatus.Completed or SignUpStatus.Expired || now < ExpiresAt)
        {
            return false;
        }

        Close();
        return true;
    }

    /// <summary>
    /// Closes a sign-up that can no longer complete, whatever its lifetime, so the phone is free
    /// for a fresh one at once instead of at expiry.
    /// </summary>
    public void Close()
    {
        Status = SignUpStatus.Expired;
        Version += 1;
    }

    /// <summary>
    /// Checks a candidate code in constant time. A mismatch is returned rather than thrown so
    /// the handler persists the attempt count; the last allowed mismatch locks the sign-up for
    /// the remainder of its lifetime.
    /// </summary>
    public PhoneVerificationOutcome VerifyPhone(
        ReadOnlySpan<byte> candidateDigest,
        DateTimeOffset now,
        SessionsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        RefuseUnlessCodePending(now);

        if (CodeDigest is null || CodeExpiresAt is not { } codeExpiry || codeExpiry <= now)
        {
            throw new RefusalException(MoniPayErrorTypes.VerificationCodeExpired);
        }

        return CryptographicOperations.FixedTimeEquals(candidateDigest, CodeDigest)
            ? MarkVerified(now)
            : RecordFailedAttempt(options.MaximumVerificationAttempts);
    }

    /// <summary>
    /// The refusal a locked sign-up answers with. The handler needs it too, once it has
    /// persisted the attempt that locked the sign-up.
    /// </summary>
    public RefusalException AttemptLimitRefusal(DateTimeOffset now) =>
        new(MoniPayErrorTypes.SignUpAttemptLimit, retryAfter: (LockedUntil ?? ExpiresAt) - now);

    private void RefuseUnlessAlive(DateTimeOffset now)
    {
        if (Status == SignUpStatus.Expired || ExpiresAt <= now)
        {
            throw new RefusalException(MoniPayErrorTypes.SignUpExpired);
        }
    }

    /// <summary>
    /// The guard restart and verify share. A resend answers a locked sign-up with the plain
    /// state refusal instead: no delay makes a resend succeed on a locked row.
    /// </summary>
    private void RefuseUnlessCodePending(DateTimeOffset now)
    {
        RefuseUnlessAlive(now);
        if (Status == SignUpStatus.Locked)
        {
            throw AttemptLimitRefusal(now);
        }

        if (Status != SignUpStatus.CodePending)
        {
            throw new RefusalException(MoniPayErrorTypes.SignUpStateInvalid);
        }
    }

    private PhoneVerificationOutcome MarkVerified(DateTimeOffset now)
    {
        Status = SignUpStatus.PhoneVerified;
        CodeDigest = null;
        CodeExpiresAt = null;
        VerifiedAt = now;
        Version += 1;
        return PhoneVerificationOutcome.Verified;
    }

    private PhoneVerificationOutcome RecordFailedAttempt(int maximumAttempts)
    {
        FailedAttempts += 1;
        Version += 1;
        if (FailedAttempts < maximumAttempts)
        {
            return PhoneVerificationOutcome.Mismatch;
        }

        Status = SignUpStatus.Locked;
        LockedUntil = ExpiresAt;
        return PhoneVerificationOutcome.Locked;
    }

    /// <summary>
    /// Publishes the registration token's digest: the sign-up token is void, and only the
    /// registration credential may now complete this sign-up.
    /// </summary>
    public void IssueRegistrationToken(byte[] digest)
    {
        ArgumentNullException.ThrowIfNull(digest);

        if (Status != SignUpStatus.PhoneVerified)
        {
            throw new RefusalException(MoniPayErrorTypes.SignUpStateInvalid);
        }

        SignUpTokenDigest = null;
        RegistrationTokenDigest = digest;
        Version += 1;
    }

    /// <summary>
    /// Marks the sign-up completed with its provisioned user and bootstrap session. A retry
    /// from <see cref="SignUpStatus.Completed"/> replaces the bootstrap session only, so a
    /// repeated completion leaves one active session; a retry naming a different user is
    /// refused, so a replay can never rebind a finished sign-up. The first completion refuses
    /// an expired sign-up.
    /// </summary>
    public void Complete(UserId userId, Guid sessionId, DateTimeOffset now)
    {
        if (Status is not (SignUpStatus.PhoneVerified or SignUpStatus.Completed))
        {
            throw new RefusalException(MoniPayErrorTypes.SignUpStateInvalid);
        }

        if (Status == SignUpStatus.Completed && ProvisionedUserId != userId)
        {
            throw new RefusalException(MoniPayErrorTypes.SignUpStateInvalid);
        }

        if (Status == SignUpStatus.PhoneVerified)
        {
            RefuseUnlessAlive(now);
        }

        ProvisionedUserId = userId;
        BootstrapSessionId = sessionId;
        if (Status == SignUpStatus.PhoneVerified)
        {
            Status = SignUpStatus.Completed;
            CompletedAt = now;
        }

        Version += 1;
    }
}
