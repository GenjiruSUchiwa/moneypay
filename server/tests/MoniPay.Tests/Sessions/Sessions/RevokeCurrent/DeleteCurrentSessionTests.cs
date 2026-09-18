using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MoniPay.Api.Composition;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Notifications;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Persistence;
using MoniPay.Notifications.Security;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Sessions.Persistence;
using MoniPay.Sessions.Providers;
using MoniPay.Tests.Fakes;
using MoniPay.Tests.Support;
using MoniPay.Users.Features.Registration;
using Xunit;

namespace MoniPay.Tests.Sessions.Sessions.RevokeCurrent;

public sealed class DeleteCurrentSessionTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task Revoking_returns_204_with_no_body_at_all()
    {
        OpenedSession issued = await Api.CreateSessionAsync();

        using HttpResponseMessage response = await SignUpFlow.RevokeCurrentSessionAsync(
            Client,
            AccessToken(issued));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentType);
        Assert.Equal(0, response.Content.Headers.ContentLength ?? 0);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task A_revoked_session_refuses_both_its_credentials()
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        await SignUpFlow.RevokeCurrentSessionAsync(Client, AccessToken(issued));

        using HttpResponseMessage read = await SignUpFlow.GetCurrentSessionAsync(
            Client,
            AccessToken(issued));
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = await read.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);

        using HttpResponseMessage refresh = await SignUpFlow.PostRefreshAsync(Client, issued);
        await refresh.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_second_revocation_is_401_because_the_credential_no_longer_authenticates()
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        await SignUpFlow.RevokeCurrentSessionAsync(Client, AccessToken(issued));

        using HttpResponseMessage again = await SignUpFlow.RevokeCurrentSessionAsync(
            Client,
            AccessToken(issued));

        await again.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoking_one_session_leaves_another_of_the_same_user_usable()
    {
        UserId user = UserId.New();
        OpenedSession first = await Api.CreateSessionAsync(user);
        OpenedSession second = await Api.CreateSessionAsync(user);
        await SignUpFlow.RevokeCurrentSessionAsync(Client, AccessToken(first));

        using HttpResponseMessage read = await SignUpFlow.GetCurrentSessionAsync(
            Client,
            AccessToken(second));

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        using HttpResponseMessage refresh = await SignUpFlow.PostRefreshAsync(Client, second);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
    }

    [Fact]
    public async Task A_missing_credential_is_401_with_the_bearer_challenge()
    {
        using HttpRequestMessage request = new(HttpMethod.Delete, SignUpFlow.CurrentSessionUrl());
        using HttpResponseMessage response = await Client.SendAsync(request, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal(MoniPayHeaders.Bearer, response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task Accepting_only_problem_json_is_406()
    {
        OpenedSession issued = await Api.CreateSessionAsync();
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.ProblemJson);

        using HttpResponseMessage response = await SignUpFlow.RevokeCurrentSessionAsync(
            client,
            AccessToken(issued));

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.NotAcceptable);
        Assert.Equal(MoniPayErrorTypes.NotAcceptable.Urn, problem.Type);
    }

    [Theory]
    [InlineData("en", "The session token is invalid or expired.")]
    [InlineData("fr", "Le jeton de session est invalide ou expiré.")]
    public async Task Problems_are_localized(string culture, string title)
    {
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        using HttpRequestMessage request = new(HttpMethod.Delete, SignUpFlow.CurrentSessionUrl());

        using HttpResponseMessage response = await client.SendAsync(request, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal(title, problem.Title);
    }

    [Fact]
    public async Task Revoking_enqueues_one_optional_email()
    {
        PhoneNumber phone = new(TestPhones.Next());
        EmailAddress email = new($"revoke{Guid.NewGuid():N}@example.com");

        try
        {
            RegisteredUser user = await Api.RegisterUserAsync(phone, email.Value);
            OpenedSession issued = await Api.CreateSessionAsync(user.Id);

            using HttpResponseMessage response = await SignUpFlow.RevokeCurrentSessionAsync(
                Client,
                AccessToken(issued));
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Notification row = await database.Notifications
                .AsNoTracking()
                .SingleAsync(candidate => candidate.CorrelationId == user.Id.Value
                    && candidate.Kind == SecurityAlertDeliveryAdapter.SessionRevokedKind, Cancellation);

            Assert.Equal(NotificationChannel.Email, row.Channel);
            Assert.False(row.Required);
            Assert.Null(row.ExpiresAt);
            Assert.Equal(NotificationStatus.Pending, row.Status);
            Assert.Equal($"session-revoked:{issued.Session.SessionId}:email", row.IdempotencyKey);

            RecipientProtector protector = Api.Services.GetRequiredService<RecipientProtector>();
            Assert.Equal(email.Value, protector.Unprotect(row.RecipientCiphertext));
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_failing_alert_port_leaves_the_revocation_committed_and_warns()
    {
        PhoneNumber phone = new(TestPhones.Next());
        EmailAddress email = new($"revoke{Guid.NewGuid():N}@example.com");

        try
        {
            RegisteredUser user = await Api.RegisterUserAsync(phone, email.Value);
            OpenedSession issued = await Api.CreateSessionAsync(user.Id);
            RecordingLoggerProvider logs = new();

            using WebApplicationFactory<Program> host = HostWithAlertSender(new FailingSecurityAlertSender(), logs);
            using HttpClient client = host.CreateClient();
            using HttpResponseMessage response = await SignUpFlow.RevokeCurrentSessionAsync(
                client,
                TestTokens.Bearer(user.Id, issued.Session.SessionId));
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            Assert.False(await SessionIsActiveAsync(issued.Session.SessionId));
            Assert.Contains(logs.Entries, entry => IsAlertWarningFor(entry, user.Id));
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    private WebApplicationFactory<Program> HostWithAlertSender(
        ISecurityAlertSender sender,
        RecordingLoggerProvider logs) =>
        Api.CreateHost(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISecurityAlertSender>();
            services.AddScoped(_ => sender);
            services.AddLogging(logging => logging.AddProvider(logs));
        }));

    private async Task<bool> SessionIsActiveAsync(Guid sessionId)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Session session = await database.Sessions
            .AsNoTracking()
            .SingleAsync(row => row.Id == sessionId, Cancellation);

        return session.IsActive;
    }

    private static bool IsAlertWarningFor(RecordingLoggerProvider.LogEntry entry, UserId userId) =>
        entry.Level == LogLevel.Warning
        && entry.Category == typeof(SessionTokenService).FullName
        && entry.Message.Contains(userId.ToString(), StringComparison.Ordinal);

    private static string AccessToken(OpenedSession session) =>
        TestTokens.Bearer(session.Session.UserId, session.Session.SessionId);
}
