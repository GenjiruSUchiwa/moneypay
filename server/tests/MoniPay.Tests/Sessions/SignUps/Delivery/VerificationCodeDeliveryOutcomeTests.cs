using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MoniPay.Kernel;
using MoniPay.Notifications;
using MoniPay.Notifications.Channels;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using MoniPay.Sessions;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Persistence;
using MoniPay.Sessions.Providers;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Delivery;

public sealed class VerificationCodeDeliveryOutcomeTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan LongLifetime = TimeSpan.FromMinutes(10);

    [Fact]
    public async Task Acceptance_clears_the_body()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        await Api.DeliveredCodeAsync(phone);

        Notification row = await SingleNotificationAsync(started.SignUpId);

        Assert.Equal(NotificationStatus.Sent, row.Status);
        Assert.Null(row.BodyCiphertext);
        Assert.NotNull(row.SentAt);
    }

    [Fact]
    public async Task Retryable_failures_stay_queued_then_fail()
    {
        using WebApplicationFactory<Program> longLived = Api.CreateHost(builder =>
        {
            builder.UseTestPorts(Api);
            MoniPayApi.UseNotificationTestSettings(builder);
            builder.UseSetting(SessionsOptions.Keys.VerificationCodeLifetime, LongLifetime.ToString());
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Api.Time);
            });
        });
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await longLived.Services.InScopeAsync<StartSignUpHandler, StartSignUpResult>(
            (handler, cancellationToken) => handler.HandleAsync(
                new StartSignUpCommand(phone, Locale.FrenchCameroon, SignUpFlow.TermsVersion, SignUpFlow.PrivacyVersion),
                cancellationToken));
        Api.Sms.Result = new ChannelResult.Retry("provider-5xx");

        try
        {
            DateTimeOffset expected = Api.Time.GetUtcNow();
            TimeSpan[] schedule = [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2)];
            foreach (TimeSpan delay in schedule)
            {
                Api.Time.Set(expected);
                await Api.RunNotificationCycleAsync(Cancellation);
                Assert.Equal(CodeDeliveryState.Queued, (await Api.GetSignUpAsync(started.SignUpId)).CodeDelivery);
                expected += delay;
            }

            Api.Time.Set(expected - TimeSpan.FromSeconds(1));
            await Api.RunNotificationCycleAsync(Cancellation);
            Assert.Equal(CodeDeliveryState.Queued, (await Api.GetSignUpAsync(started.SignUpId)).CodeDelivery);

            Api.Time.Set(expected);
            await Api.RunNotificationCycleAsync(Cancellation);

            Assert.Equal(CodeDeliveryState.Failed, (await Api.GetSignUpAsync(started.SignUpId)).CodeDelivery);
        }
        finally
        {
            Api.Sms.Result = new ChannelResult.Accepted("sms-ref");
        }
    }

    [Fact]
    public async Task A_superseded_code_still_sends_but_no_longer_verifies()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        Api.Time.Advance(ResendCooldown);
        await Api.ResendCodeAsync(started.SignUpId);

        await Api.RunNotificationCycleAsync(Cancellation);
        IReadOnlyList<MoniPay.Tests.Fakes.RecordingChannel.ChannelCall> calls = Api.Sms.CallsFor(phone.Value);

        Assert.Equal(2, calls.Count);
        string oldCode = System.Text.RegularExpressions.Regex.Match(
            calls.Single(call => call.IdempotencyKey.EndsWith(":0", StringComparison.Ordinal)).Body, "[0-9]{6}").Value;
        string newCode = System.Text.RegularExpressions.Regex.Match(
            calls.Single(call => call.IdempotencyKey.EndsWith(":1", StringComparison.Ordinal)).Body, "[0-9]{6}").Value;
        Assert.NotEqual(oldCode, newCode);
        await SignUpFlow.RefusedAsync(
            Api.VerifyPhoneAsync(started.SignUpId, oldCode), MoniPay.Kernel.Errors.MoniPayErrorTypes.VerificationCodeInvalid);
        Assert.NotNull(await Api.VerifyPhoneAsync(started.SignUpId, newCode));
    }

    [Fact]
    public async Task Reading_a_resend_waits_for_its_delivery_beyond_the_first_batch()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string original = await Api.DeliveredCodeAsync(phone);
        Api.Time.Advance(ResendCooldown);

        for (int index = 0; index < 10; index++)
        {
            await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        }

        Api.Time.Advance(TimeSpan.FromSeconds(1));
        await Api.ResendCodeAsync(started.SignUpId);
        string resent = await Api.DeliveredCodeAsync(phone);

        Assert.Equal(2, Api.Sms.CallsFor(phone.Value).Count);
        Assert.NotEqual(original, resent);
        Assert.NotNull(await Api.VerifyPhoneAsync(started.SignUpId, resent));
    }

    [Fact]
    public async Task Parallel_starts_enqueue_once()
    {
        PhoneNumber phone = new(TestPhones.Next());

        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => SignUpFlow.RefusalOfAsync(Api.StartSignUpAsync(phone))));
        await Api.RunNotificationCycleAsync(Cancellation);

        Assert.Equal(1, await Api.CountSignUpsAsync(phone));
        SignUpId winner = await SingleSignUpIdAsync(phone);
        int notifications = await Api.InScopeAsync<MoniPayDbContext, int>((database, cancellationToken) =>
            database.Notifications.CountAsync(notification => notification.CorrelationId == winner.Value, cancellationToken));
        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task Delivery_logs_carry_no_code_or_phone()
    {
        PhoneNumber phone = new(TestPhones.Next());
        int before = Api.Logs.Entries.Count;
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string code = await Api.DeliveredCodeAsync(phone);

        IReadOnlyList<MoniPay.Tests.Fakes.RecordingLoggerProvider.LogEntry> written = Api.Logs.Entries
            .Skip(before)
            .Where(entry => entry.Category != null && entry.Category.StartsWith("MoniPay.", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(written);
        foreach (MoniPay.Tests.Fakes.RecordingLoggerProvider.LogEntry entry in written)
        {
            Assert.DoesNotContain(code, entry.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(phone.Value, entry.Message, StringComparison.Ordinal);
        }

        Assert.NotNull(await Api.VerifyPhoneAsync(started.SignUpId, code));
    }

    private async Task<Notification> SingleNotificationAsync(SignUpId signUpId) =>
        await Api.InScopeAsync<MoniPayDbContext, Notification>(async (database, cancellationToken) =>
            await database.Notifications.SingleAsync(
                notification => notification.CorrelationId == signUpId.Value, cancellationToken));

    private async Task<SignUpId> SingleSignUpIdAsync(PhoneNumber phone)
    {
        MoniPay.Sessions.Security.PhoneLookupDigest digests = Api.Services.GetRequiredService<MoniPay.Sessions.Security.PhoneLookupDigest>();
        LookupHash hash = digests.Compute(phone);
        return await Api.InScopeAsync<MoniPayDbContext, SignUpId>((database, cancellationToken) =>
            database.SignUps
                .Where(signUp => signUp.PhoneLookupHash.Equals(hash))
                .Select(signUp => signUp.Id)
                .SingleAsync(cancellationToken));
    }
}
