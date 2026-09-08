using Microsoft.EntityFrameworkCore;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps.ResendCode;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.ResendCode;

public sealed class CreateVerificationCodeDeliveryTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SignUpLifetime = TimeSpan.FromMinutes(15);
    private const int MaximumAttempts = 3;

    [Fact]
    public async Task A_resend_refreshes_the_code_timing_and_keeps_the_failed_attempts()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        await SignUpFlow.RefusedAsync(
            Api.VerifyPhoneAsync(started.SignUpId, SignUpFlow.Wrong(await Api.DeliveredCodeAsync(phone))),
            MoniPayErrorTypes.VerificationCodeInvalid);
        Api.Time.Advance(ResendCooldown);
        DateTimeOffset now = Api.Time.GetUtcNow();

        CreateVerificationCodeDeliveryResult resent = await Api.ResendCodeAsync(started.SignUpId);

        Assert.Equal(now + CodeLifetime, resent.CodeExpiresAt);
        Assert.Equal(now + ResendCooldown, resent.CanResendAt);
        Assert.Equal(started.SignUpExpiresAt, resent.SignUpExpiresAt);
        SignUp row = await Api.ReadSignUpRowAsync(started.SignUpId);
        Assert.Equal(1, row.FailedAttempts);
        Assert.Equal(1, row.ResendCount);
    }

    [Fact]
    public async Task A_resend_queues_the_new_code_and_invalidates_the_prior_one()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string priorCode = await Api.DeliveredCodeAsync(phone);
        Api.Time.Advance(ResendCooldown);

        CreateVerificationCodeDeliveryResult resent = await Api.ResendCodeAsync(started.SignUpId);

        IReadOnlyList<Notification> notifications = await NotificationsForAsync(started.SignUpId);
        Assert.Equal(2, notifications.Count);
        Assert.Equal($"verification-code:{started.SignUpId}:1", notifications[^1].IdempotencyKey);
        Assert.Equal(resent.CodeExpiresAt, notifications[^1].ExpiresAt);
        string newCode = await Api.DeliveredCodeAsync(phone);
        Assert.NotEqual(priorCode, newCode);
        await SignUpFlow.RefusedAsync(Api.VerifyPhoneAsync(started.SignUpId, priorCode), MoniPayErrorTypes.VerificationCodeInvalid);
        Assert.NotNull(await Api.VerifyPhoneAsync(started.SignUpId, newCode));
    }

    [Fact]
    public async Task A_resend_stays_possible_after_the_code_expired()
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        Api.Time.Advance(CodeLifetime + TimeSpan.FromSeconds(1));

        CreateVerificationCodeDeliveryResult resent = await Api.ResendCodeAsync(started.SignUpId);

        Assert.Equal(Api.Time.GetUtcNow() + CodeLifetime, resent.CodeExpiresAt);
    }

    [Fact]
    public async Task A_locked_sign_up_refuses_a_resend_as_a_state_conflict_without_a_delay()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string wrongCode = SignUpFlow.Wrong(await Api.DeliveredCodeAsync(phone));
        for (int attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            await SignUpFlow.RefusalOfAsync(Api.VerifyPhoneAsync(started.SignUpId, wrongCode));
        }

        Api.Time.Advance(ResendCooldown);

        RefusalException refusal = await SignUpFlow.RefusedAsync(Api.ResendCodeAsync(started.SignUpId), MoniPayErrorTypes.SignUpStateInvalid);

        Assert.Null(refusal.RetryAfter);
    }

    [Fact]
    public async Task An_expired_sign_up_refuses_a_resend()
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        Api.Time.Advance(SignUpLifetime);

        await SignUpFlow.RefusedAsync(Api.ResendCodeAsync(started.SignUpId), MoniPayErrorTypes.SignUpExpired);
    }

    private async Task<IReadOnlyList<Notification>> NotificationsForAsync(SignUpId signUpId) =>
        await Api.InScopeAsync<MoniPayDbContext, List<Notification>>(async (database, cancellationToken) =>
            await database.Notifications
                .AsNoTracking()
                .Where(notification => notification.CorrelationId == signUpId.Value)
                .OrderBy(notification => notification.CreatedAt)
                .ThenBy(notification => notification.Id)
                .ToListAsync(cancellationToken));
}
