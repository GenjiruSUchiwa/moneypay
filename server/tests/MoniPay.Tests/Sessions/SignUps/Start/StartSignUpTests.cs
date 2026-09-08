using Microsoft.EntityFrameworkCore;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Notifications;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Start;

public sealed class StartSignUpTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SignUpLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan StartWindow = TimeSpan.FromHours(1);
    private const int MaximumStartsPerWindow = 3;
    private const int MaximumResends = 3;

    [Fact]
    public async Task Starting_stores_the_row_and_queues_one_message_that_expires_with_the_code()
    {
        PhoneNumber phone = new(TestPhones.Next());
        DateTimeOffset now = Api.Time.GetUtcNow();

        StartSignUpResult started = await Api.StartSignUpAsync(phone);

        SignUp row = await Api.ReadSignUpRowAsync(started.SignUpId);
        Assert.Equal(SignUpStatus.CodePending, row.Status);
        Assert.NotNull(row.CodeDigest);
        Assert.Equal(now + CodeLifetime, started.CodeExpiresAt);
        Assert.Equal(now + ResendCooldown, started.CanResendAt);
        Assert.Equal(now + SignUpLifetime, started.SignUpExpiresAt);

        Assert.Empty(Api.Sms.CallsFor(phone.Value));
        Notification notification = Assert.Single(await NotificationsForAsync(started.SignUpId));
        Assert.Equal(NotificationChannel.Sms, notification.Channel);
        Assert.Equal(RetrySchedule.VerificationCodeKind, notification.Kind);
        Assert.True(notification.Required);
        Assert.Equal(started.CodeExpiresAt, notification.ExpiresAt);
        Assert.Equal($"verification-code:{started.SignUpId}:0", notification.IdempotencyKey);

        string code = await Api.DeliveredCodeAsync(phone);
        Assert.Single(Api.Sms.CallsFor(phone.Value));
        Assert.Contains(code, Api.Sms.CallsFor(phone.Value)[0].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_code_the_token_and_the_phone_are_never_stored_in_clear()
    {
        PhoneNumber phone = new(TestPhones.Next());

        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string code = await Api.DeliveredCodeAsync(phone);

        Assert.Empty(await Api.RowsContainingAsync(code));
        Assert.Empty(await Api.RowsContainingAsync(started.SignUpToken));
        Assert.Empty(await Api.RowsContainingAsync(phone.Value));
    }

    [Fact]
    public async Task Starting_again_for_an_in_flight_phone_reuses_the_row_with_a_new_code_and_token()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult first = await Api.StartSignUpAsync(phone);
        string firstCode = await Api.DeliveredCodeAsync(phone);
        Api.Time.Advance(ResendCooldown);

        StartSignUpResult second = await Api.StartSignUpAsync(phone);

        Assert.Equal(first.SignUpId, second.SignUpId);
        Assert.NotEqual(first.SignUpToken, second.SignUpToken);
        Assert.NotEqual(firstCode, await Api.DeliveredCodeAsync(phone));
        Assert.Equal(1, await Api.CountSignUpsAsync(phone));
        Assert.Equal(1, (await Api.ReadSignUpRowAsync(first.SignUpId)).ResendCount);
        await SignUpFlow.RefusedAsync(Api.VerifyPhoneAsync(first.SignUpId, firstCode), MoniPayErrorTypes.VerificationCodeInvalid);
    }

    [Fact]
    public async Task Starting_for_a_registered_phone_looks_like_any_other_start()
    {
        PhoneNumber phone = new(TestPhones.Next());
        await Api.RegisterUserAsync(phone);

        try
        {
            StartSignUpResult started = await Api.StartSignUpAsync(phone);

            Assert.Equal(SignUpStatus.CodePending, (await Api.ReadSignUpRowAsync(started.SignUpId)).Status);
            Assert.Single(await NotificationsForAsync(started.SignUpId));
        }
        finally
        {
            await CleanUsersTablesAsync();
        }
    }

    [Fact]
    public async Task Starting_after_the_sign_up_expired_opens_a_fresh_one()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult first = await Api.StartSignUpAsync(phone);
        Api.Time.Advance(SignUpLifetime);

        StartSignUpResult second = await Api.StartSignUpAsync(phone);

        Assert.NotEqual(first.SignUpId, second.SignUpId);
        Assert.Equal(SignUpStatus.Expired, (await Api.ReadSignUpRowAsync(first.SignUpId)).Status);
        Assert.Equal(SignUpStatus.CodePending, (await Api.ReadSignUpRowAsync(second.SignUpId)).Status);
    }

    [Fact]
    public async Task A_start_past_the_per_phone_limit_is_refused_with_the_delay_to_the_oldest_start()
    {
        PhoneNumber phone = new(TestPhones.Next());
        // Each start waits out the previous sign-up, so every one opens a row and counts.
        TimeSpan spacing = SignUpLifetime + TimeSpan.FromMinutes(1);
        for (int start = 0; start < MaximumStartsPerWindow; start++)
        {
            await Api.StartSignUpAsync(phone);
            Api.Time.Advance(spacing);
        }

        RefusalException refusal = await SignUpFlow.RefusedAsync(Api.StartSignUpAsync(phone), MoniPayErrorTypes.RateLimited);

        Assert.Equal(StartWindow - MaximumStartsPerWindow * spacing, refusal.RetryAfter);
        Assert.Equal(MaximumStartsPerWindow, await Api.CountSignUpsAsync(phone));
    }

    [Fact]
    public async Task Reopening_the_active_sign_up_is_not_a_counted_start()
    {
        PhoneNumber phone = new(TestPhones.Next());
        for (int start = 0; start <= MaximumStartsPerWindow; start++)
        {
            await Api.StartSignUpAsync(phone);
            Api.Time.Advance(ResendCooldown);
        }

        Assert.Equal(1, await Api.CountSignUpsAsync(phone));
    }

    [Fact]
    public async Task Reopening_the_active_sign_up_succeeds_while_the_window_is_full()
    {
        PhoneNumber phone = new(TestPhones.Next());
        TimeSpan spacing = SignUpLifetime + TimeSpan.FromMinutes(1);
        StartSignUpResult last = await Api.StartSignUpAsync(phone);
        for (int start = 1; start < MaximumStartsPerWindow; start++)
        {
            Api.Time.Advance(spacing);
            last = await Api.StartSignUpAsync(phone);
        }

        Api.Time.Advance(ResendCooldown);

        StartSignUpResult reopened = await Api.StartSignUpAsync(phone);

        Assert.Equal(last.SignUpId, reopened.SignUpId);
        Assert.Equal(MaximumStartsPerWindow, await Api.CountSignUpsAsync(phone));
    }

    [Fact]
    public async Task Starting_past_the_resend_limit_reports_the_delay_to_a_fresh_sign_up()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        for (int resend = 0; resend < MaximumResends; resend++)
        {
            Api.Time.Advance(ResendCooldown);
            await Api.ResendCodeAsync(started.SignUpId);
        }

        Api.Time.Advance(ResendCooldown);

        RefusalException refusal = await SignUpFlow.RefusedAsync(Api.StartSignUpAsync(phone), MoniPayErrorTypes.SignUpResendLimit);

        Assert.Equal(started.SignUpExpiresAt - Api.Time.GetUtcNow(), refusal.RetryAfter);
    }

    [Fact]
    public async Task Starting_for_a_locked_phone_reports_the_retry_delay()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string wrongCode = SignUpFlow.Wrong(await Api.DeliveredCodeAsync(phone));
        for (int attempt = 0; attempt < 3; attempt++)
        {
            await SignUpFlow.RefusalOfAsync(Api.VerifyPhoneAsync(started.SignUpId, wrongCode));
        }

        RefusalException refusal = await SignUpFlow.RefusedAsync(Api.StartSignUpAsync(phone), MoniPayErrorTypes.SignUpAttemptLimit);

        Assert.Equal(started.SignUpExpiresAt - Api.Time.GetUtcNow(), refusal.RetryAfter);
        Assert.Equal(1, await Api.CountSignUpsAsync(phone));
    }

    [Fact]
    public async Task Starting_for_a_verified_phone_leaves_its_sign_up_untouched()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        await Api.VerifyPhoneAsync(started.SignUpId, await Api.DeliveredCodeAsync(phone));

        await SignUpFlow.RefusedAsync(Api.StartSignUpAsync(phone), MoniPayErrorTypes.SignUpStateInvalid);

        SignUp row = await Api.ReadSignUpRowAsync(started.SignUpId);
        Assert.Equal(SignUpStatus.PhoneVerified, row.Status);
        Assert.NotNull(row.RegistrationTokenDigest);
        Assert.Single(await NotificationsForAsync(started.SignUpId));
    }

    [Fact]
    public async Task Parallel_starts_for_one_phone_share_one_sign_up()
    {
        PhoneNumber phone = new(TestPhones.Next());

        RefusalException?[] outcomes = await Task.WhenAll(
            Enumerable.Range(0, 4).Select(_ => SignUpFlow.RefusalOfAsync(Api.StartSignUpAsync(phone))));

        Assert.Equal(1, outcomes.Count(outcome => outcome is null));
        Assert.All(outcomes.OfType<RefusalException>(), refusal => Assert.Equal(MoniPayErrorTypes.SignUpResendTooSoon, refusal.Type));
        Assert.Equal(1, await Api.CountSignUpsAsync(phone));
    }

    [Fact]
    public async Task A_start_racing_a_verification_is_refused_rather_than_failing_on_the_version()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string code = await Api.DeliveredCodeAsync(phone);
        Api.Time.Advance(ResendCooldown);

        // A concurrency failure is not a refusal, so it would fail the test instead of being collected.
        RefusalException?[] outcomes = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(attempt => attempt % 2 == 0
                ? SignUpFlow.RefusalOfAsync(Api.StartSignUpAsync(phone))
                : SignUpFlow.RefusalOfAsync(Api.VerifyPhoneAsync(started.SignUpId, code))));

        // Whichever side won the row lock, the loser saw its committed state: a rotated code
        // (mismatches, then the lock) or a verified phone (state conflicts).
        ProblemType[] expected =
        [
            MoniPayErrorTypes.SignUpResendTooSoon,
            MoniPayErrorTypes.SignUpStateInvalid,
            MoniPayErrorTypes.VerificationCodeInvalid,
            MoniPayErrorTypes.SignUpAttemptLimit,
        ];
        Assert.Equal(1, await Api.CountSignUpsAsync(phone));
        Assert.All(outcomes.OfType<RefusalException>(), refusal => Assert.Contains(refusal.Type, expected));
    }

    [Fact]
    public async Task Parallel_starts_for_an_in_flight_phone_reopen_it_once()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult first = await Api.StartSignUpAsync(phone);
        Api.Time.Advance(ResendCooldown);

        RefusalException?[] outcomes = await Task.WhenAll(
            Enumerable.Range(0, 4).Select(_ => SignUpFlow.RefusalOfAsync(Api.StartSignUpAsync(phone))));

        Assert.Equal(1, outcomes.Count(outcome => outcome is null));
        Assert.All(outcomes.OfType<RefusalException>(), refusal => Assert.Equal(MoniPayErrorTypes.SignUpResendTooSoon, refusal.Type));
        Assert.Equal(1, (await Api.ReadSignUpRowAsync(first.SignUpId)).ResendCount);
    }

    private Task CleanUsersTablesAsync() =>
        Api.QueryAsync("TRUNCATE TABLE user_consents, users;", reader => 0);

    private async Task<IReadOnlyList<Notification>> NotificationsForAsync(SignUpId signUpId) =>
        await Api.InScopeAsync<MoniPayDbContext, List<Notification>>(async (database, cancellationToken) =>
            await database.Notifications
                .AsNoTracking()
                .Where(notification => notification.CorrelationId == signUpId.Value)
                .OrderBy(notification => notification.CreatedAt)
                .ThenBy(notification => notification.Id)
                .ToListAsync(cancellationToken));
}
