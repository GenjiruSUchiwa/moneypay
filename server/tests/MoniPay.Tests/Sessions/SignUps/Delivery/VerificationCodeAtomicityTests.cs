using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MoniPay.Api.Composition;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Notifications;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Providers;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Delivery;

/// <summary>
/// Start and resend commit their sign-up and its notification together or neither.
/// </summary>
public sealed class VerificationCodeAtomicityTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SignUpLifetime = TimeSpan.FromMinutes(15);

    [Fact]
    public async Task A_new_start_commits_one_row_and_one_notification_without_sending()
    {
        PhoneNumber phone = new(TestPhones.Next());

        StartSignUpResult started = await Api.StartSignUpAsync(phone);

        Assert.Equal(1, await Api.CountSignUpsAsync(phone));
        Assert.Empty(Api.Sms.CallsFor(phone.Value));
        Assert.Single(await NotificationsForAsync(started.SignUpId));
    }

    [Fact]
    public async Task An_enqueue_failure_leaves_no_sign_up_and_no_notification()
    {
        PhoneNumber phone = new(TestPhones.Next());
        using WebApplicationFactory<Program> failing = FailingHost(new FailingSender());

        await Assert.ThrowsAsync<ProviderUnavailableException>(() =>
            failing.Services.InScopeAsync<StartSignUpHandler, StartSignUpResult>((handler, cancellationToken) =>
                handler.HandleAsync(
                    new StartSignUpCommand(phone, Locale.FrenchCameroon, SignUpFlow.TermsVersion, SignUpFlow.PrivacyVersion),
                    cancellationToken)));

        Assert.Equal(0, await Api.CountSignUpsAsync(phone));
        await Api.RunNotificationCycleAsync(Cancellation);
        Assert.Empty(Api.Sms.CallsFor(phone.Value));
    }

    [Fact]
    public async Task A_failure_after_enqueue_rolls_back_both_writes()
    {
        PhoneNumber phone = new(TestPhones.Next());
        using WebApplicationFactory<Program> failing = Api.CreateHost(builder =>
        {
            builder.UseTestPorts(Api);
            MoniPayApi.UseNotificationTestSettings(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Api.Time);
                services.AddScoped<VerificationCodeDeliveryAdapter>();
                services.AddScoped<IVerificationCodeSender, EnqueueThenThrow>();
            });
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            failing.Services.InScopeAsync<StartSignUpHandler, StartSignUpResult>((handler, cancellationToken) =>
                handler.HandleAsync(
                    new StartSignUpCommand(phone, Locale.FrenchCameroon, SignUpFlow.TermsVersion, SignUpFlow.PrivacyVersion),
                    cancellationToken)));

        Assert.Equal(0, await Api.CountSignUpsAsync(phone));
        await Api.RunNotificationCycleAsync(Cancellation);
        Assert.Empty(Api.Sms.CallsFor(phone.Value));
    }

    [Fact]
    public async Task A_failed_replacement_keeps_the_expired_row_unclosed()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult first = await Api.StartSignUpAsync(phone);
        Api.Time.Advance(SignUpLifetime);
        using WebApplicationFactory<Program> failing = FailingHost(new FailingSender());

        await Assert.ThrowsAsync<ProviderUnavailableException>(() =>
            failing.Services.InScopeAsync<StartSignUpHandler, StartSignUpResult>((handler, cancellationToken) =>
                handler.HandleAsync(
                    new StartSignUpCommand(phone, Locale.FrenchCameroon, SignUpFlow.TermsVersion, SignUpFlow.PrivacyVersion),
                    cancellationToken)));

        SignUp row = await Api.ReadSignUpRowAsync(first.SignUpId);
        Assert.Equal(SignUpStatus.CodePending, row.Status);
        Assert.Equal(1, await Api.CountSignUpsAsync(phone));
    }

    [Fact]
    public async Task A_failed_resend_keeps_the_code_the_count_and_the_cooldown()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string code = await Api.DeliveredCodeAsync(phone);
        Api.Time.Advance(ResendCooldown);
        using WebApplicationFactory<Program> failing = FailingHost(new FailingSender());

        await Assert.ThrowsAsync<ProviderUnavailableException>(() =>
            failing.Services.InScopeAsync<MoniPay.Sessions.Features.SignUps.ResendCode.CreateVerificationCodeDeliveryHandler, MoniPay.Sessions.Features.SignUps.ResendCode.CreateVerificationCodeDeliveryResult>(
                (handler, cancellationToken) => handler.HandleAsync(started.SignUpId, cancellationToken)));

        SignUp row = await Api.ReadSignUpRowAsync(started.SignUpId);
        Assert.Equal(0, row.ResendCount);
        Assert.Single(await NotificationsForAsync(started.SignUpId));
        Assert.NotNull(await Api.VerifyPhoneAsync(started.SignUpId, code));
    }

    private WebApplicationFactory<Program> FailingHost(IVerificationCodeSender sender) =>
        Api.CreateHost(builder =>
        {
            builder.UseTestPorts(Api);
            MoniPayApi.UseNotificationTestSettings(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Api.Time);
                services.AddSingleton(sender);
            });
        });

    private async Task<IReadOnlyList<Notification>> NotificationsForAsync(SignUpId signUpId) =>
        await Api.InScopeAsync<MoniPayDbContext, List<Notification>>(async (database, cancellationToken) =>
            await database.Notifications
                .AsNoTracking()
                .Where(notification => notification.CorrelationId == signUpId.Value)
                .OrderBy(notification => notification.CreatedAt)
                .ThenBy(notification => notification.Id)
                .ToListAsync(cancellationToken));

    private sealed class FailingSender : IVerificationCodeSender
    {
        public Task EnqueueAsync(VerificationCodeMessage message, CancellationToken cancellationToken) =>
            throw new ProviderUnavailableException("sms");

        public Task<CodeDeliveryState?> GetLatestDeliveryAsync(SignUpId signUpId, CancellationToken cancellationToken) =>
            Task.FromResult<CodeDeliveryState?>(null);
    }

    private sealed class EnqueueThenThrow(VerificationCodeDeliveryAdapter inner) : IVerificationCodeSender
    {
        public async Task EnqueueAsync(VerificationCodeMessage message, CancellationToken cancellationToken)
        {
            await inner.EnqueueAsync(message, cancellationToken);
            throw new InvalidOperationException("After enqueue.");
        }

        public Task<CodeDeliveryState?> GetLatestDeliveryAsync(SignUpId signUpId, CancellationToken cancellationToken) =>
            inner.GetLatestDeliveryAsync(signUpId, cancellationToken);
    }
}
