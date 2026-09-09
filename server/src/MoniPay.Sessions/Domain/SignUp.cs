using System.Security.Cryptography;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;

namespace MoniPay.Sessions.Domain;

internal sealed class SignUp
{
    private static readonly TimeSpan RegistrationLifetime = TimeSpan.FromMinutes(10);

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

    public int FailedAttempts { get; private set; }

    public int ResendCount { get; private set; }

    public DateTimeOffset CanResendAt { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public string TermsVersion { get; private set; } = string.Empty;

    public string PrivacyVersion { get; private set; } = string.Empty;

    public UserId? ProvisionedUserId { get; private set; }

    public Guid? BootstrapSessionId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? VerifiedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public long Version { get; private set; }

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

    public void Close()
    {
        Status = SignUpStatus.Expired;
        Version += 1;
    }

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

    public RefusalException AttemptLimitRefusal(DateTimeOffset now) =>
        new(MoniPayErrorTypes.SignUpAttemptLimit, retryAfter: (LockedUntil ?? ExpiresAt) - now);

    private void RefuseUnlessAlive(DateTimeOffset now)
    {
        if (Status == SignUpStatus.Expired || ExpiresAt <= now)
        {
            throw new RefusalException(MoniPayErrorTypes.SignUpExpired);
        }
    }

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

    public ProblemType? CompletionRefusal(DateTimeOffset now)
    {
        if (Status == SignUpStatus.Expired || ExpiresAt <= now)
        {
            return MoniPayErrorTypes.SignUpExpired;
        }

        if (Status is not (SignUpStatus.PhoneVerified or SignUpStatus.Completed))
        {
            return MoniPayErrorTypes.SignUpStateInvalid;
        }

        if (VerifiedAt is not { } issuedAt || issuedAt + RegistrationLifetime <= now)
        {
            return MoniPayErrorTypes.RegistrationTokenInvalid;
        }

        return null;
    }

    public void Complete(UserId userId, Guid sessionId, DateTimeOffset now)
    {
        if (CompletionRefusal(now) is { } refusal)
        {
            throw new RefusalException(refusal);
        }

        if (Status == SignUpStatus.Completed && ProvisionedUserId != userId)
        {
            throw new RefusalException(MoniPayErrorTypes.SignUpStateInvalid);
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
