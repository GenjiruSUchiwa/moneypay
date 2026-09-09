using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Features.SignUps.Get;
using MoniPay.Sessions.Features.SignUps.ResendCode;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Features.SignUps.VerifyPhone;
using MoniPay.Sessions.Persistence;
using MoniPay.Sessions.Security;
using MoniPay.Users.Features.CurrentUser;
using MoniPay.Users.Features.Registration;
using Xunit;

namespace MoniPay.Tests.Support;

internal sealed record VerifiedSignUp(
    StartSignUpResult Started,
    CreatePhoneVerificationResult Verified,
    PhoneNumber Phone);

internal sealed record OpenedSession(SessionTokenResult Session, Guid DeviceId);

internal static class SignUpFlow
{
    public const string TermsVersion = "terms-2026-08";
    public const string PrivacyVersion = "privacy-2026-07";

    private static int sequence;

    private static int isolatedIpCounter;

    public static Task<TResult> InScopeAsync<TService, TResult>(
        this MoniPayApi api,
        Func<TService, CancellationToken, Task<TResult>> act)
        where TService : notnull
    {
        ArgumentNullException.ThrowIfNull(api);

        return api.Services.InScopeAsync(act);
    }

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

    public static async Task<string> DeliveredCodeAsync(this MoniPayApi api, PhoneNumber phone)
    {
        ArgumentNullException.ThrowIfNull(api);

        LookupHash phoneHash = api.Services.GetRequiredService<PhoneLookupDigest>().Compute(phone);
        SignUp signUp = await api.InScopeAsync<MoniPayDbContext, SignUp>((database, token) =>
            database.SignUps.AsNoTracking()
                .Where(row => row.PhoneLookupHash.Equals(phoneHash))
                .OrderByDescending(row => row.CreatedAt)
                .ThenByDescending(row => row.Id)
                .FirstAsync(token));
        string key = FormattableString.Invariant($"verification-code:{signUp.Id}:{signUp.ResendCount}");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        while (!api.Sms.CallsFor(phone.Value).Any(call => call.IdempotencyKey == key)
            && await api.RunNotificationCycleAsync(cancellationToken) > 0)
        {
        }

        Fakes.RecordingChannel.ChannelCall delivered = Assert.Single(
            api.Sms.CallsFor(phone.Value), call => call.IdempotencyKey == key);
        return System.Text.RegularExpressions.Regex.Match(delivered.Body, "[0-9]{6}").Value;
    }

    public static Task<VerifiedSignUp> StartVerifiedAsync(this MoniPayApi api)
    {
        ArgumentNullException.ThrowIfNull(api);

        return api.StartVerifiedAsync(api.Services);
    }

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

    public static Task CleanUsersAsync(this MoniPayApi api)
    {
        ArgumentNullException.ThrowIfNull(api);

        return api.QueryAsync("TRUNCATE TABLE user_consents, users;", reader => 0);
    }

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

    public static string Wrong(string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        return (code[0] == '0' ? "1" : "0") + code[1..];
    }

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

    public static Task<OpenedSession> CreateSessionAsync(this MoniPayApi api) => api.CreateSessionAsync(UserId.New());

    public static Task<OpenedSession> CreateSessionAsync(this MoniPayApi api, UserId userId)
    {
        Guid deviceId = Guid.CreateVersion7();

        return api.InScopeAsync<SessionTokenService, OpenedSession>(async (sessions, cancellationToken) =>
            new OpenedSession(await sessions.CreateAsync(userId, deviceId, cancellationToken), deviceId));
    }

    public static string CurrentSessionUrl() => SessionRoutes.Group + SessionRoutes.Current;

    public static Task<HttpResponseMessage> GetCurrentSessionAsync(HttpClient client, string accessToken) =>
        SendWithBearerAsync(client, HttpMethod.Get, CurrentSessionUrl(), accessToken);

    public static Task<HttpResponseMessage> RevokeCurrentSessionAsync(HttpClient client, string accessToken) =>
        SendWithBearerAsync(client, HttpMethod.Delete, CurrentSessionUrl(), accessToken);

    public static string CurrentUserUrl() => MoniPayRoutes.CurrentUser;

    public static Task<HttpResponseMessage> GetCurrentUserAsync(HttpClient client, string accessToken) =>
        SendWithBearerAsync(client, HttpMethod.Get, CurrentUserUrl(), accessToken);

    private static Task<HttpResponseMessage> SendWithBearerAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        string accessToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(accessToken);

        HttpRequestMessage request = new(method, url);
        request.Headers.Authorization = new(MoniPayHeaders.Bearer, accessToken);

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static string RefreshUrl() => SessionRoutes.Refreshes;

    public static StringContent RefreshBody(string? refreshToken, Guid deviceId)
    {
        string json = JsonSerializer.Serialize(new
        {
            data = new
            {
                type = SessionResourceTypes.SessionRefreshes,
                attributes = new
                {
                    refreshToken,
                    deviceId,
                },
            },
        });

        StringContent body = new(json, Encoding.UTF8);
        body.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);

        return body;
    }

    public static Task<HttpResponseMessage> PostRefreshAsync(HttpClient client, OpenedSession session) =>
        PostRefreshAsync(client, session.Session.RefreshToken, session.DeviceId);

    public static Task<HttpResponseMessage> PostRefreshAsync(HttpClient client, string? refreshToken, Guid deviceId)
    {
        ArgumentNullException.ThrowIfNull(client);

        HttpRequestMessage request = new(HttpMethod.Post, RefreshUrl())
        {
            Content = RefreshBody(refreshToken, deviceId),
        };
        request.Headers.Add("X-Forwarded-For", IsolatedIp());

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static string ResendUrl(SignUpId signUpId) =>
        FormattableString.Invariant($"{SignUpResources.Self(signUpId)}/verification-code-deliveries");

    public static Task<HttpResponseMessage> PostResendAsync(
        HttpClient client,
        SignUpId signUpId,
        string signUpToken,
        HttpContent? body = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(signUpToken);

        HttpRequestMessage request = new(HttpMethod.Post, ResendUrl(signUpId))
        {
            Content = body,
        };
        request.Headers.Authorization = new(SessionsSchemes.SignUp, signUpToken);
        request.Headers.Add("X-Forwarded-For", IsolatedIp());

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static string VerifyUrl(SignUpId signUpId) =>
        FormattableString.Invariant($"{SignUpResources.Self(signUpId)}/phone-verifications");

    public static StringContent VerifyBody(SignUpId signUpId, string? code)
    {
        string json = JsonSerializer.Serialize(new
        {
            data = new
            {
                type = SignUpResourceTypes.PhoneVerifications,
                attributes = new
                {
                    verificationCode = code,
                },
                relationships = new
                {
                    signUp = new
                    {
                        data = new
                        {
                            type = SignUpResourceTypes.SignUps,
                            id = signUpId.Value.ToString(),
                        },
                    },
                },
            },
        });

        StringContent body = new(json, Encoding.UTF8);
        body.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);

        return body;
    }

    public static Task<HttpResponseMessage> PostVerifyAsync(
        HttpClient client,
        SignUpId signUpId,
        string signUpToken,
        string? code,
        StringContent? body = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(signUpToken);

        HttpRequestMessage request = new(HttpMethod.Post, VerifyUrl(signUpId))
        {
            Content = body ?? VerifyBody(signUpId, code),
        };
        request.Headers.Authorization = new(SessionsSchemes.SignUp, signUpToken);
        request.Headers.Add("X-Forwarded-For", IsolatedIp());

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static string StartUrl() => SignUpRoutes.Group;

    public static string ReadUrl(SignUpId signUpId) => SignUpResources.Self(signUpId);

    public static StringContent StartBody(string phone, string? termsVersion = null, string? privacyVersion = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(phone);

        string json = JsonSerializer.Serialize(new
        {
            data = new
            {
                type = SignUpResourceTypes.SignUps,
                attributes = new
                {
                    phone,
                    termsVersion = termsVersion ?? TermsVersion,
                    privacyVersion = privacyVersion ?? PrivacyVersion,
                },
            },
        });

        StringContent body = new(json, Encoding.UTF8);
        body.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);

        return body;
    }

    public static Task<HttpResponseMessage> PostStartAsync(HttpClient client, HttpContent body)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(body);

        HttpRequestMessage request = new(HttpMethod.Post, StartUrl())
        {
            Content = body,
        };
        request.Headers.Add("X-Forwarded-For", IsolatedIp());

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static Task<HttpResponseMessage> GetSignUpHttpAsync(
        HttpClient client,
        SignUpId signUpId,
        string scheme,
        string token)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(scheme);
        ArgumentException.ThrowIfNullOrEmpty(token);

        HttpRequestMessage request = new(HttpMethod.Get, ReadUrl(signUpId));
        request.Headers.Authorization = new(scheme, token);

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static async Task<IReadOnlyList<MoniPay.Notifications.Domain.Notification>> NotificationsForAsync(
        MoniPayApi api,
        SignUpId signUpId)
    {
        ArgumentNullException.ThrowIfNull(api);

        return await api.InScopeAsync<MoniPayDbContext, List<MoniPay.Notifications.Domain.Notification>>(
            async (database, cancellationToken) => await database.Notifications
                .AsNoTracking()
                .Where(notification => notification.CorrelationId == signUpId.Value)
                .OrderBy(notification => notification.CreatedAt)
                .ThenBy(notification => notification.Id)
                .ToListAsync(cancellationToken));
    }

    private static string IsolatedIp()
    {
        int unique = Interlocked.Increment(ref isolatedIpCounter);

        return FormattableString.Invariant($"203.0.113.{unique % 250 + 1}");
    }
}
