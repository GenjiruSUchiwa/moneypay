using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Features.SignUps.Get;
using MoniPay.Sessions.Features.SignUps.ResendCode;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Features.SignUps.VerifyPhone;
using MoniPay.Sessions.Persistence;
using MoniPay.Sessions.Security;
using MoniPay.Users.Features.Registration;
using Xunit;

namespace MoniPay.Tests.Support;

/// <summary>The sign-up a completion starts from, and the credential that authorizes it.</summary>
internal sealed record VerifiedSignUp(
    StartSignUpResult Started,
    CreatePhoneVerificationResult Verified,
    PhoneNumber Phone);

/// <summary>
/// Drives the sign-up slices through their handlers, each call in a fresh scope of the test host,
/// exactly as one HTTP request would. Every helper is typed on the slice's own records.
/// </summary>
internal static class SignUpFlow
{
    public const string TermsVersion = "terms-2026-08";
    public const string PrivacyVersion = "privacy-2026-07";

    private static int sequence;

    public static Task<TResult> InScopeAsync<TService, TResult>(
        this MoniPayApi api,
        Func<TService, CancellationToken, Task<TResult>> act)
        where TService : notnull
    {
        ArgumentNullException.ThrowIfNull(api);

        return api.Services.InScopeAsync(act);
    }

    /// <summary>
    /// One call in one scope of any host — the shared fixture's, or one a test configured for a
    /// scenario the fixture cannot be reconfigured for.
    /// </summary>
    public static async Task<TResult> InScopeAsync<TService, TResult>(
        this IServiceProvider services,
        Func<TService, CancellationToken, Task<TResult>> act)
        where TService : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(act);

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        return await act(scope.ServiceProvider.GetRequiredService<TService>(), TestContext.Current.CancellationToken);
    }

    public static Task<StartSignUpResult> StartSignUpAsync(this MoniPayApi api, PhoneNumber phone) =>
        api.InScopeAsync<StartSignUpHandler, StartSignUpResult>((handler, cancellationToken) =>
            handler.HandleAsync(new StartSignUpCommand(phone, Locale.FrenchCameroon, TermsVersion, PrivacyVersion), cancellationToken));

    public static Task<SignUpView> GetSignUpAsync(this MoniPayApi api, SignUpId signUpId) =>
        api.InScopeAsync<GetSignUpHandler, SignUpView>((handler, cancellationToken) =>
            handler.HandleAsync(signUpId, cancellationToken));

    public static Task<CreateVerificationCodeDeliveryResult> ResendCodeAsync(this MoniPayApi api, SignUpId signUpId) =>
        api.InScopeAsync<CreateVerificationCodeDeliveryHandler, CreateVerificationCodeDeliveryResult>((handler, cancellationToken) =>
            handler.HandleAsync(signUpId, cancellationToken));

    public static Task<CreatePhoneVerificationResult> VerifyPhoneAsync(this MoniPayApi api, SignUpId signUpId, string code) =>
        api.InScopeAsync<CreatePhoneVerificationHandler, CreatePhoneVerificationResult>((handler, cancellationToken) =>
            handler.HandleAsync(signUpId, code, cancellationToken));

    /// <summary>The code the worker delivered for a phone through the real outbox.</summary>
    public static async Task<string> DeliveredCodeAsync(this MoniPayApi api, PhoneNumber phone)
    {
        ArgumentNullException.ThrowIfNull(api);

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        for (int cycle = 0; cycle < 20; cycle++)
        {
            await api.RunNotificationCycleAsync(cancellationToken);
            if (api.Sms.CallsFor(phone.Value).Count > 0)
            {
                return api.Sms.CodeFor(phone.Value);
            }
        }

        return api.Sms.CodeFor(phone.Value);
    }

    /// <summary>A phone proven by its code, on its own number: the state completion starts from.</summary>
    public static Task<VerifiedSignUp> StartVerifiedAsync(this MoniPayApi api)
    {
        ArgumentNullException.ThrowIfNull(api);

        return api.StartVerifiedAsync(api.Services);
    }

    /// <summary>Starts through one host and delivers through the shared fixture.</summary>
    public static async Task<VerifiedSignUp> StartVerifiedAsync(this MoniPayApi api, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentNullException.ThrowIfNull(services);

        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await services.InScopeAsync<StartSignUpHandler, StartSignUpResult>(
            (handler, cancellationToken) => handler.HandleAsync(
                new StartSignUpCommand(phone, Locale.FrenchCameroon, TermsVersion, PrivacyVersion),
                cancellationToken));
        string code = await api.DeliveredCodeAsync(phone);
        return new VerifiedSignUp(
            started,
            await services.InScopeAsync<CreatePhoneVerificationHandler, CreatePhoneVerificationResult>(
                (handler, cancellationToken) => handler.HandleAsync(started.SignUpId, code, cancellationToken)),
            phone);
    }

    /// <summary>The profile a completion submits, on an email and device no other test uses.</summary>
    public static CreateSignUpCompletionCommand CompletionCommand(string? email = null, Guid? deviceId = null)
    {
        int unique = Interlocked.Increment(ref sequence);
        return new CreateSignUpCompletionCommand(
            new PersonName("Marie"),
            new PersonName("Ngo"),
            new EmailAddress(email ?? $"complete.marie{unique}@example.com"),
            deviceId ?? Guid.NewGuid());
    }

    public static Task<CreateSignUpCompletionResult> CompleteSignUpAsync(
        this MoniPayApi api,
        SignUpId signUpId,
        string registrationToken,
        CreateSignUpCompletionCommand command)
    {
        ArgumentNullException.ThrowIfNull(api);

        return api.Services.CompleteSignUpAsync(signUpId, registrationToken, command);
    }

    public static Task<CreateSignUpCompletionResult> CompleteSignUpAsync(
        this IServiceProvider services,
        SignUpId signUpId,
        string registrationToken,
        CreateSignUpCompletionCommand command) =>
        services.InScopeAsync<CreateSignUpCompletionHandler, CreateSignUpCompletionResult>((handler, cancellationToken) =>
            handler.HandleAsync(signUpId, registrationToken, command, cancellationToken));

    /// <summary>Registers a user the way a completed sign-up would have provisioned one.</summary>
    public static Task<RegisteredUser> RegisterUserAsync(this MoniPayApi api, PhoneNumber phone, string? email = null)
    {
        ArgumentNullException.ThrowIfNull(api);
        int unique = Interlocked.Increment(ref sequence);
        return api.InScopeAsync<RegisterUserHandler, RegisteredUser>((handler, cancellationToken) =>
            handler.HandleAsync(
                new RegisterUserCommand(
                    SignUpId.New(),
                    UserId.New(),
                    phone,
                    new PersonName("Marie"),
                    new PersonName("Ngo Nyobé"),
                    new EmailAddress(email ?? $"marie.ngo{unique}@example.com"),
                    Locale.FrenchCameroon,
                    TermsVersion,
                    PrivacyVersion,
                    new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero)),
                cancellationToken));
    }

    /// <summary>Empties the users tables, so a user one test provisioned never ages into another.</summary>
    public static Task CleanUsersAsync(this MoniPayApi api)
    {
        ArgumentNullException.ThrowIfNull(api);

        return api.QueryAsync("TRUNCATE TABLE user_consents, users;", reader => 0);
    }

    /// <summary>The row as PostgreSQL holds it, for asserting what a slice persisted.</summary>
    public static Task<SignUp> ReadSignUpRowAsync(this MoniPayApi api, SignUpId signUpId) =>
        api.InScopeAsync<MoniPayDbContext, SignUp>((database, cancellationToken) =>
            database.SignUps.AsNoTracking().SingleAsync(signUp => signUp.Id == signUpId, cancellationToken));

    public static Task<int> CountSignUpsAsync(this MoniPayApi api, PhoneNumber phone)
    {
        ArgumentNullException.ThrowIfNull(api);

        LookupHash phoneHash = api.Services.GetRequiredService<PhoneLookupDigest>().Compute(phone);
        return api.InScopeAsync<MoniPayDbContext, int>((database, cancellationToken) =>
            database.SignUps.CountAsync(signUp => signUp.PhoneLookupHash.Equals(phoneHash), cancellationToken));
    }

    /// <summary>A code that differs from the real one in its first digit, for a guaranteed mismatch.</summary>
    public static string Wrong(string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        return (code[0] == '0' ? "1" : "0") + code[1..];
    }

    /// <summary>The refusal a slice call ended in, or <c>null</c> when it succeeded; for parallel calls.</summary>
    public static async Task<RefusalException?> RefusalOfAsync(Task attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        try
        {
            await attempt;
            return null;
        }
        catch (RefusalException refusal)
        {
            return refusal;
        }
    }

    public static async Task<RefusalException> RefusedAsync(Task attempt, ProblemType problemType)
    {
        RefusalException refusal = await Assert.ThrowsAsync<RefusalException>(() => attempt);
        Assert.Equal(problemType, refusal.Type);
        return refusal;
    }
}
