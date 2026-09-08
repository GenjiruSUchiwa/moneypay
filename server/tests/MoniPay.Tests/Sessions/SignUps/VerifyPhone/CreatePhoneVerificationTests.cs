using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Features.SignUps.VerifyPhone;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.VerifyPhone;

public sealed class CreatePhoneVerificationTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private const int MaximumAttempts = 3;
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan SignUpLifetime = TimeSpan.FromMinutes(15);

    [Fact]
    public async Task The_right_code_verifies_the_phone_and_returns_the_registration_token_once()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);

        CreatePhoneVerificationResult verified = await Api.VerifyPhoneAsync(started.SignUpId, Api.Sender.CodeFor(phone));

        Assert.Equal(started.SignUpId, verified.SignUpId);
        Assert.NotEmpty(verified.RegistrationToken);
        Assert.Equal(started.SignUpExpiresAt, verified.SignUpExpiresAt);
        SignUp row = await Api.ReadSignUpRowAsync(started.SignUpId);
        Assert.Equal(SignUpStatus.PhoneVerified, row.Status);
        Assert.Null(row.SignUpTokenDigest);
        Assert.NotNull(row.RegistrationTokenDigest);
        Assert.Empty(await Api.RowsContainingAsync(verified.RegistrationToken));
    }

    [Fact]
    public async Task A_wrong_code_is_refused_with_a_pointer_and_the_attempt_is_persisted()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);

        RefusalException refusal = await SignUpFlow.RefusedAsync(
            Api.VerifyPhoneAsync(started.SignUpId, SignUpFlow.Wrong(Api.Sender.CodeFor(phone))),
            MoniPayErrorTypes.VerificationCodeInvalid);

        Assert.Equal([CreatePhoneVerificationHandler.VerificationCodePointer], refusal.Pointers);
        Assert.Equal(1, (await Api.ReadSignUpRowAsync(started.SignUpId)).FailedAttempts);
    }

    [Fact]
    public async Task Exhausting_the_attempts_locks_the_sign_up_and_reports_the_retry_delay()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string code = Api.Sender.CodeFor(phone);
        for (int attempt = 1; attempt < MaximumAttempts; attempt++)
        {
            await SignUpFlow.RefusedAsync(Api.VerifyPhoneAsync(started.SignUpId, SignUpFlow.Wrong(code)), MoniPayErrorTypes.VerificationCodeInvalid);
        }

        RefusalException locking = await SignUpFlow.RefusedAsync(
            Api.VerifyPhoneAsync(started.SignUpId, SignUpFlow.Wrong(code)),
            MoniPayErrorTypes.SignUpAttemptLimit);

        Assert.Equal(started.SignUpExpiresAt - Api.Time.GetUtcNow(), locking.RetryAfter);
        SignUp row = await Api.ReadSignUpRowAsync(started.SignUpId);
        Assert.Equal(SignUpStatus.Locked, row.Status);
        Assert.Equal(MaximumAttempts, row.FailedAttempts);
        await SignUpFlow.RefusedAsync(Api.VerifyPhoneAsync(started.SignUpId, code), MoniPayErrorTypes.SignUpAttemptLimit);
    }

    [Fact]
    public async Task An_expired_code_does_not_verify()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        Api.Time.Advance(CodeLifetime);

        await SignUpFlow.RefusedAsync(Api.VerifyPhoneAsync(started.SignUpId, Api.Sender.CodeFor(phone)), MoniPayErrorTypes.VerificationCodeExpired);

        Assert.Equal(SignUpStatus.CodePending, (await Api.ReadSignUpRowAsync(started.SignUpId)).Status);
    }

    [Fact]
    public async Task An_expired_sign_up_refuses_verification()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        Api.Time.Advance(SignUpLifetime);

        await SignUpFlow.RefusedAsync(Api.VerifyPhoneAsync(started.SignUpId, Api.Sender.CodeFor(phone)), MoniPayErrorTypes.SignUpExpired);
    }

    [Fact]
    public async Task A_phone_a_user_already_owns_is_refused_and_its_sign_up_closed_so_a_fresh_start_is_free()
    {
        PhoneNumber phone = new(TestPhones.Next());
        await Api.RegisterUserAsync(phone);

        try
        {
            StartSignUpResult started = await Api.StartSignUpAsync(phone);

            await SignUpFlow.RefusedAsync(
                Api.VerifyPhoneAsync(started.SignUpId, Api.Sender.CodeFor(phone)),
                MoniPayErrorTypes.PhoneAlreadyRegistered);

            SignUp row = await Api.ReadSignUpRowAsync(started.SignUpId);
            Assert.Equal(SignUpStatus.Expired, row.Status);
            Assert.Null(row.RegistrationTokenDigest);
            Assert.NotEqual(started.SignUpId, (await Api.StartSignUpAsync(phone)).SignUpId);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_wrong_code_does_not_disclose_that_the_phone_is_registered()
    {
        PhoneNumber phone = new(TestPhones.Next());
        await Api.RegisterUserAsync(phone);
        StartSignUpResult started = await Api.StartSignUpAsync(phone);

        try
        {
            RefusalException refusal = await SignUpFlow.RefusedAsync(
                Api.VerifyPhoneAsync(started.SignUpId, SignUpFlow.Wrong(Api.Sender.CodeFor(phone))),
                MoniPayErrorTypes.VerificationCodeInvalid);

            Assert.Equal([CreatePhoneVerificationHandler.VerificationCodePointer], refusal.Pointers);
            SignUp row = await Api.ReadSignUpRowAsync(started.SignUpId);
            Assert.Equal(SignUpStatus.CodePending, row.Status);
            Assert.Null(row.RegistrationTokenDigest);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task Eight_parallel_right_codes_yield_exactly_one_registration_token()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string code = Api.Sender.CodeFor(phone);

        RefusalException?[] outcomes = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => SignUpFlow.RefusalOfAsync(Api.VerifyPhoneAsync(started.SignUpId, code))));

        Assert.Equal(1, outcomes.Count(outcome => outcome is null));
        Assert.All(outcomes.OfType<RefusalException>(), refusal => Assert.Equal(MoniPayErrorTypes.SignUpStateInvalid, refusal.Type));
        Assert.Equal(SignUpStatus.PhoneVerified, (await Api.ReadSignUpRowAsync(started.SignUpId)).Status);
    }

    [Fact]
    public async Task Eight_parallel_wrong_codes_never_exceed_the_attempt_limit()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string wrongCode = SignUpFlow.Wrong(Api.Sender.CodeFor(phone));

        RefusalException?[] outcomes = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => SignUpFlow.RefusalOfAsync(Api.VerifyPhoneAsync(started.SignUpId, wrongCode))));

        Assert.All(outcomes, outcome => Assert.NotNull(outcome));
        Assert.Equal(MaximumAttempts, (await Api.ReadSignUpRowAsync(started.SignUpId)).FailedAttempts);
    }

}
