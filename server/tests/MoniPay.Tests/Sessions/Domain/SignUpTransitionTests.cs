using System.Net;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Sessions;
using MoniPay.Sessions.Domain;
using Xunit;

namespace MoniPay.Tests.Sessions.Domain;

/// <summary>
/// Every documented row of the <c>SignUp</c> transition table, against options whose values
/// deliberately differ from the defaults: a test passing on a default would prove nothing.
/// </summary>
public sealed class SignUpTransitionTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 9, 5, 10, 0, 0, TimeSpan.Zero);

    private static readonly byte[] CodeDigest = [0xAA, 0xBB];
    private static readonly byte[] ReplacementDigest = [0xCC, 0xDD];
    private static readonly byte[] SignUpTokenDigest = [0x11, 0x22];

    [Fact]
    public void Starting_a_sign_up_records_the_code_pending_state_from_the_options()
    {
        SessionsOptions options = Options();

        SignUp signUp = StartAt(StartedAt, options);

        Assert.Equal(SignUpStatus.CodePending, signUp.Status);
        Assert.Equal(StartedAt + options.VerificationCodeLifetime, signUp.CodeExpiresAt);
        Assert.Equal(StartedAt + options.ResendCooldown, signUp.CanResendAt);
        Assert.Equal(StartedAt + options.SignUpLifetime, signUp.ExpiresAt);
        Assert.Equal(0, signUp.FailedAttempts);
        Assert.Equal(0, signUp.ResendCount);
        Assert.Equal(StartedAt, signUp.CreatedAt);
        Assert.Equal(1, signUp.Version);
        Assert.Equal(CodeDigest, signUp.CodeDigest);
        Assert.Equal(SignUpTokenDigest, signUp.SignUpTokenDigest);
    }

    [Fact]
    public void Rotating_before_the_cooldown_is_refused_with_the_remaining_delay()
    {
        SignUp signUp = StartAt(StartedAt, Options());

        RefusalException refusal = Assert.Throws<RefusalException>(
            () => signUp.RotateVerificationCode(ReplacementDigest, StartedAt + TimeSpan.FromSeconds(10), Options()));

        Assert.Equal(MoniPayErrorTypes.SignUpResendTooSoon, refusal.Type);
        Assert.Equal(HttpStatusCode.TooManyRequests, refusal.Type.Status);
        Assert.Equal(TimeSpan.FromSeconds(20), refusal.RetryAfter);
        Assert.Equal(CodeDigest, signUp.CodeDigest);
    }

    [Fact]
    public void Rotating_past_the_resend_limit_is_refused()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);
        signUp.RotateVerificationCode(ReplacementDigest, StartedAt + TimeSpan.FromSeconds(30), options);
        signUp.RotateVerificationCode(CodeDigest, StartedAt + TimeSpan.FromSeconds(60), options);

        RefusalException refusal = Assert.Throws<RefusalException>(
            () => signUp.RotateVerificationCode(ReplacementDigest, StartedAt + TimeSpan.FromSeconds(90), options));

        Assert.Equal(MoniPayErrorTypes.SignUpResendLimit, refusal.Type);
        Assert.Equal(HttpStatusCode.TooManyRequests, refusal.Type.Status);
        Assert.Equal(signUp.ExpiresAt - (StartedAt + TimeSpan.FromSeconds(90)), refusal.RetryAfter);
        Assert.Equal(2, signUp.ResendCount);
    }

    [Fact]
    public void Rotating_a_locked_sign_up_is_refused_as_a_state_conflict_without_a_delay()
    {
        SessionsOptions options = Options();
        SignUp signUp = LockedSignUp(options);

        RefusalException refusal = Assert.Throws<RefusalException>(
            () => signUp.RotateVerificationCode(ReplacementDigest, StartedAt + TimeSpan.FromSeconds(30), options));

        Assert.Equal(MoniPayErrorTypes.SignUpStateInvalid, refusal.Type);
        Assert.Null(refusal.RetryAfter);
    }

    [Fact]
    public void Rotating_replaces_the_digest_and_preserves_the_failed_attempts()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);
        Assert.Equal(PhoneVerificationOutcome.Mismatch, signUp.VerifyPhone([0xEE], StartedAt, options));

        signUp.RotateVerificationCode(ReplacementDigest, StartedAt + TimeSpan.FromSeconds(30), options);

        Assert.Equal(ReplacementDigest, signUp.CodeDigest);
        Assert.NotEqual(CodeDigest, signUp.CodeDigest);
        Assert.Equal(StartedAt + TimeSpan.FromSeconds(30) + options.VerificationCodeLifetime, signUp.CodeExpiresAt);
        Assert.Equal(1, signUp.ResendCount);
        Assert.Equal(StartedAt + TimeSpan.FromSeconds(60), signUp.CanResendAt);
        Assert.Equal(1, signUp.FailedAttempts);
        Assert.Equal(3, signUp.Version);
    }

    [Fact]
    public void Rotating_an_expired_sign_up_is_refused()
    {
        SignUp signUp = StartAt(StartedAt, Options());

        AssertRefused(
            () => signUp.RotateVerificationCode(ReplacementDigest, signUp.ExpiresAt + TimeSpan.FromSeconds(1), Options()),
            MoniPayErrorTypes.SignUpExpired,
            HttpStatusCode.Gone);
    }

    [Fact]
    public void Rotating_a_verified_sign_up_is_refused()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);
        Assert.Equal(PhoneVerificationOutcome.Verified, signUp.VerifyPhone(CodeDigest, StartedAt, options));

        AssertRefused(
            () => signUp.RotateVerificationCode(ReplacementDigest, StartedAt, options),
            MoniPayErrorTypes.SignUpStateInvalid,
            HttpStatusCode.Conflict);
    }

    [Fact]
    public void Restarting_rotates_the_code_under_the_resend_rules_and_replaces_the_sign_up_token()
    {
        byte[] replacementTokenDigest = [0x55, 0x66];
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);

        signUp.Restart(ReplacementDigest, replacementTokenDigest, StartedAt + TimeSpan.FromSeconds(30), options);

        Assert.Equal(ReplacementDigest, signUp.CodeDigest);
        Assert.Equal(replacementTokenDigest, signUp.SignUpTokenDigest);
        Assert.Equal(1, signUp.ResendCount);
        AssertRefused(
            () => signUp.Restart(CodeDigest, SignUpTokenDigest, StartedAt + TimeSpan.FromSeconds(40), options),
            MoniPayErrorTypes.SignUpResendTooSoon,
            HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public void Restarting_a_locked_sign_up_publishes_the_retry_delay()
    {
        SessionsOptions options = Options();
        SignUp signUp = LockedSignUp(options);

        RefusalException refusal = Assert.Throws<RefusalException>(
            () => signUp.Restart(ReplacementDigest, SignUpTokenDigest, StartedAt + TimeSpan.FromSeconds(30), options));

        Assert.Equal(MoniPayErrorTypes.SignUpAttemptLimit, refusal.Type);
        Assert.Equal(signUp.LockedUntil - (StartedAt + TimeSpan.FromSeconds(30)), refusal.RetryAfter);
        Assert.Equal(SignUpStatus.Locked, signUp.Status);
    }

    [Fact]
    public void Restarting_a_verified_sign_up_is_refused_and_changes_nothing()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);
        Assert.Equal(PhoneVerificationOutcome.Verified, signUp.VerifyPhone(CodeDigest, StartedAt, options));

        AssertRefused(
            () => signUp.Restart(ReplacementDigest, [0x55, 0x66], StartedAt + TimeSpan.FromSeconds(30), options),
            MoniPayErrorTypes.SignUpStateInvalid,
            HttpStatusCode.Conflict);
        Assert.Equal(SignUpStatus.PhoneVerified, signUp.Status);
        Assert.Equal(SignUpTokenDigest, signUp.SignUpTokenDigest);
    }

    [Fact]
    public void A_sign_up_expires_once_its_lifetime_has_passed_and_not_before()
    {
        SignUp signUp = StartAt(StartedAt, Options());

        Assert.False(signUp.TryExpire(signUp.ExpiresAt - TimeSpan.FromSeconds(1)));
        Assert.Equal(SignUpStatus.CodePending, signUp.Status);
        Assert.True(signUp.TryExpire(signUp.ExpiresAt));
        Assert.Equal(SignUpStatus.Expired, signUp.Status);
        Assert.Equal(2, signUp.Version);
    }

    [Fact]
    public void Closing_a_sign_up_frees_the_phone_before_its_lifetime_has_passed()
    {
        SignUp signUp = StartAt(StartedAt, Options());

        signUp.Close();

        Assert.Equal(SignUpStatus.Expired, signUp.Status);
        Assert.Equal(2, signUp.Version);
    }

    [Fact]
    public void A_completed_sign_up_never_expires()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);
        Assert.Equal(PhoneVerificationOutcome.Verified, signUp.VerifyPhone(CodeDigest, StartedAt, options));
        signUp.Complete(UserId.New(), Guid.CreateVersion7(), StartedAt);

        Assert.False(signUp.TryExpire(signUp.ExpiresAt + TimeSpan.FromDays(1)));
        Assert.Equal(SignUpStatus.Completed, signUp.Status);
    }

    [Fact]
    public void The_correct_code_verifies_the_phone()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);

        PhoneVerificationOutcome outcome = signUp.VerifyPhone(CodeDigest, StartedAt, options);

        Assert.Equal(PhoneVerificationOutcome.Verified, outcome);
        Assert.Equal(SignUpStatus.PhoneVerified, signUp.Status);
        Assert.Null(signUp.CodeDigest);
        Assert.Null(signUp.CodeExpiresAt);
        Assert.Equal(StartedAt, signUp.VerifiedAt);
    }

    [Fact]
    public void A_wrong_code_consumes_one_attempt_without_changing_the_state()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);

        PhoneVerificationOutcome outcome = signUp.VerifyPhone([0xEE], StartedAt, options);

        Assert.Equal(PhoneVerificationOutcome.Mismatch, outcome);
        Assert.Equal(SignUpStatus.CodePending, signUp.Status);
        Assert.Equal(1, signUp.FailedAttempts);
    }

    [Fact]
    public void The_last_allowed_wrong_code_locks_the_sign_up_for_its_remaining_lifetime()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);

        Assert.Equal(PhoneVerificationOutcome.Mismatch, signUp.VerifyPhone([0xEE], StartedAt, options));
        Assert.Equal(PhoneVerificationOutcome.Mismatch, signUp.VerifyPhone([0xEE], StartedAt, options));

        PhoneVerificationOutcome outcome = signUp.VerifyPhone([0xEE], StartedAt, options);

        Assert.Equal(PhoneVerificationOutcome.Locked, outcome);
        Assert.Equal(SignUpStatus.Locked, signUp.Status);
        Assert.Equal(signUp.ExpiresAt, signUp.LockedUntil);
    }

    [Fact]
    public void A_locked_sign_up_refuses_checks_and_publishes_the_retry_delay()
    {
        SessionsOptions options = Options();
        SignUp signUp = LockedSignUp(options);

        RefusalException refusal = Assert.Throws<RefusalException>(
            () => signUp.VerifyPhone(CodeDigest, StartedAt, options));

        Assert.Equal(MoniPayErrorTypes.SignUpAttemptLimit, refusal.Type);
        Assert.Equal(HttpStatusCode.TooManyRequests, refusal.Type.Status);
        Assert.Equal(signUp.LockedUntil - StartedAt, refusal.RetryAfter);
    }

    [Fact]
    public void An_expired_code_is_refused_while_the_sign_up_is_alive()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);
        DateTimeOffset beforeTheCode = (signUp.CodeExpiresAt ?? throw new InvalidOperationException()).AddSeconds(-1);
        DateTimeOffset afterTheCode = beforeTheCode.AddSeconds(2);

        Assert.Equal(PhoneVerificationOutcome.Mismatch, signUp.VerifyPhone([0xEE], beforeTheCode, options));
        AssertRefused(
            () => signUp.VerifyPhone(CodeDigest, afterTheCode, options),
            MoniPayErrorTypes.VerificationCodeExpired,
            HttpStatusCode.Gone);
    }

    [Fact]
    public void A_resend_stays_available_one_second_before_the_sign_up_expires()
    {
        SignUp signUp = StartAt(StartedAt, Options());

        signUp.RotateVerificationCode(ReplacementDigest, signUp.ExpiresAt - TimeSpan.FromSeconds(1), Options());

        Assert.Equal(1, signUp.ResendCount);
    }

    [Fact]
    public void An_expired_sign_up_refuses_every_check()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);

        AssertRefused(
            () => signUp.VerifyPhone(CodeDigest, signUp.ExpiresAt + TimeSpan.FromSeconds(1), options),
            MoniPayErrorTypes.SignUpExpired,
            HttpStatusCode.Gone);
    }

    [Fact]
    public void A_verified_sign_up_refuses_further_checks()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);
        Assert.Equal(PhoneVerificationOutcome.Verified, signUp.VerifyPhone(CodeDigest, StartedAt, options));

        AssertRefused(
            () => signUp.VerifyPhone(CodeDigest, StartedAt, options),
            MoniPayErrorTypes.SignUpStateInvalid,
            HttpStatusCode.Conflict);
    }

    [Fact]
    public void Publishing_the_registration_token_voids_the_sign_up_token()
    {
        byte[] registrationDigest = [0x33, 0x44];
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);
        Assert.Equal(PhoneVerificationOutcome.Verified, signUp.VerifyPhone(CodeDigest, StartedAt, options));

        signUp.IssueRegistrationToken(registrationDigest);

        Assert.Null(signUp.SignUpTokenDigest);
        Assert.Equal(registrationDigest, signUp.RegistrationTokenDigest);
        Assert.Equal(StartedAt, signUp.VerifiedAt);
    }

    [Fact]
    public void Publishing_the_registration_token_before_verification_is_refused()
    {
        SignUp signUp = StartAt(StartedAt, Options());

        AssertRefused(
            () => signUp.IssueRegistrationToken([0x33, 0x44]),
            MoniPayErrorTypes.SignUpStateInvalid,
            HttpStatusCode.Conflict);
    }

    [Fact]
    public void Completing_after_verification_records_the_user_and_the_bootstrap_session()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);
        Assert.Equal(PhoneVerificationOutcome.Verified, signUp.VerifyPhone(CodeDigest, StartedAt, options));
        UserId userId = UserId.New();
        Guid sessionId = Guid.CreateVersion7();

        signUp.Complete(userId, sessionId, StartedAt);

        Assert.Equal(SignUpStatus.Completed, signUp.Status);
        Assert.Equal(userId, signUp.ProvisionedUserId);
        Assert.Equal(sessionId, signUp.BootstrapSessionId);
        Assert.Equal(StartedAt, signUp.CompletedAt);
    }

    [Fact]
    public void A_repeated_completion_replaces_the_bootstrap_session_only()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);
        Assert.Equal(PhoneVerificationOutcome.Verified, signUp.VerifyPhone(CodeDigest, StartedAt, options));
        UserId userId = UserId.New();
        Guid replacementSessionId = Guid.CreateVersion7();
        DateTimeOffset completedAt = StartedAt + TimeSpan.FromMinutes(1);
        signUp.Complete(userId, Guid.CreateVersion7(), completedAt);

        signUp.Complete(userId, replacementSessionId, completedAt + TimeSpan.FromMinutes(1));

        Assert.Equal(SignUpStatus.Completed, signUp.Status);
        Assert.Equal(userId, signUp.ProvisionedUserId);
        Assert.Equal(replacementSessionId, signUp.BootstrapSessionId);
        Assert.Equal(completedAt, signUp.CompletedAt);
        Assert.Equal(4, signUp.Version);
    }

    [Fact]
    public void A_repeated_completion_naming_a_different_user_is_refused()
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);
        Assert.Equal(PhoneVerificationOutcome.Verified, signUp.VerifyPhone(CodeDigest, StartedAt, options));
        UserId userId = UserId.New();
        Guid sessionId = Guid.CreateVersion7();
        signUp.Complete(userId, sessionId, StartedAt);

        AssertRefused(
            () => signUp.Complete(UserId.New(), Guid.CreateVersion7(), StartedAt),
            MoniPayErrorTypes.SignUpStateInvalid,
            HttpStatusCode.Conflict);

        Assert.Equal(userId, signUp.ProvisionedUserId);
        Assert.Equal(sessionId, signUp.BootstrapSessionId);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Completing_before_the_phone_is_verified_is_refused(int mismatches)
    {
        SessionsOptions options = Options();
        SignUp signUp = StartAt(StartedAt, options);
        for (int attempt = 0; attempt < mismatches; attempt++)
        {
            signUp.VerifyPhone([0xEE], StartedAt, options);
        }

        AssertRefused(
            () => signUp.Complete(UserId.New(), Guid.CreateVersion7(), StartedAt),
            MoniPayErrorTypes.SignUpStateInvalid,
            HttpStatusCode.Conflict);
    }

    private static SignUp LockedSignUp(SessionsOptions options)
    {
        SignUp signUp = StartAt(StartedAt, options);
        for (int attempt = 0; attempt < options.MaximumVerificationAttempts; attempt++)
        {
            signUp.VerifyPhone([0xEE], StartedAt, options);
        }

        return signUp;
    }

    private static void AssertRefused(Action transition, ProblemType problemType, HttpStatusCode status)
    {
        RefusalException refusal = Assert.Throws<RefusalException>(transition);
        Assert.Equal(problemType, refusal.Type);
        Assert.Equal(status, refusal.Type.Status);
    }

    private static SignUp StartAt(DateTimeOffset startedAt, SessionsOptions options) =>
        SignUp.Start(
            SignUpId.New(),
            new Ciphertext("+237699123456-cipher"),
            new LookupHash([1, 2, 3]),
            Locale.FrenchCameroon,
            "terms-2026-08",
            "privacy-2026-07",
            CodeDigest,
            SignUpTokenDigest,
            startedAt,
            options);

    private static SessionsOptions Options() => new()
    {
        VerificationCodeLength = 8,
        VerificationCodeLifetime = TimeSpan.FromMinutes(2),
        ResendCooldown = TimeSpan.FromSeconds(30),
        MaximumResends = 2,
        MaximumVerificationAttempts = 3,
        SignUpLifetime = TimeSpan.FromMinutes(10),
    };
}
