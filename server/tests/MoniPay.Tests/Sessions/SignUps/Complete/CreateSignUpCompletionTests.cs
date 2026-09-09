using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Api.Composition;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Persistence;
using MoniPay.Tests.Support;
using MoniPay.Users.Domain;
using MoniPay.Users.Persistence;
using MoniPay.Users.Security;
using Npgsql;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Complete;

public sealed class CreateSignUpCompletionTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static readonly TimeSpan SignUpLifetime = TimeSpan.FromMinutes(15);

    [Fact]
    public async Task A_first_completion_provisions_the_user_opens_the_bootstrap_session_and_completes_the_sign_up()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        CreateSignUpCompletionCommand command = SignUpFlow.CompletionCommand();

        try
        {
            CreateSignUpCompletionResult completed = await Api.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, command);

            Assert.True(completed.Created);
            Assert.NotEmpty(completed.Session.RefreshToken);
            Assert.True(completed.Session.AccessTokenExpiresAt > Api.Time.GetUtcNow());
            Assert.True(completed.Session.RefreshTokenExpiresAt > Api.Time.GetUtcNow());

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.Equal(1, await database.Users.CountAsync(
                user => user.SignUpId == signUp.Started.SignUpId, Cancellation));
            Assert.Equal(2, await database.UserConsents.CountAsync(
                consent => consent.UserId == completed.Session.UserId, Cancellation));
            Session session = await database.Sessions.AsNoTracking().SingleAsync(
                candidate => candidate.Id == completed.Session.SessionId, Cancellation);
            Assert.Null(session.RevokedAt);
            Assert.Equal(completed.Session.UserId, session.UserId);
            Assert.Equal(command.DeviceId, session.DeviceId);
            Assert.Equal(1, await database.RefreshTokens.CountAsync(
                token => token.SessionId == completed.Session.SessionId && token.UsedAt == null, Cancellation));
            SignUp row = await database.SignUps.AsNoTracking().SingleAsync(
                candidate => candidate.Id == signUp.Started.SignUpId, Cancellation);
            Assert.Equal(SignUpStatus.Completed, row.Status);
            Assert.Equal(completed.Session.UserId, row.ProvisionedUserId);
            Assert.Equal(completed.Session.SessionId, row.BootstrapSessionId);

            await AssertSingleWelcomeAsync(database, command, completed.Session.UserId);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    private async Task AssertSingleWelcomeAsync(
        MoniPayDbContext database,
        CreateSignUpCompletionCommand command,
        UserId userId)
    {
        Assert.Equal(1, await database.Notifications.CountAsync(
            candidate => candidate.Kind == WelcomeMessageDeliveryAdapter.WelcomeKind
                && candidate.CorrelationId == userId.Value,
            Cancellation));

        await Api.RunNotificationCycleAsync(Cancellation);

        Assert.Single(Api.Email.CallsFor(command.Email.Value));
    }

    [Fact]
    public async Task A_retry_with_the_same_registration_token_replaces_only_the_bootstrap_session()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        CreateSignUpCompletionCommand command = SignUpFlow.CompletionCommand();

        try
        {
            CreateSignUpCompletionResult first = await Api.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, command);
            SignUp completed = await Api.ReadSignUpRowAsync(signUp.Started.SignUpId);
            CreateSignUpCompletionResult retry = await Api.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, command);

            Assert.False(retry.Created);
            Assert.NotEqual(first.Session.SessionId, retry.Session.SessionId);
            Assert.Equal(first.Session.UserId, retry.Session.UserId);

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            List<Session> sessions = await database.Sessions.AsNoTracking()
                .Where(candidate => candidate.UserId == retry.Session.UserId)
                .ToListAsync(Cancellation);
            Session prior = Assert.Single(sessions, candidate => candidate.Id == first.Session.SessionId);
            Assert.NotNull(prior.RevokedAt);
            Assert.Equal(SessionRevokeReason.BootstrapReplaced, prior.RevokeReason);
            Session replacement = Assert.Single(sessions, candidate => candidate.Id == retry.Session.SessionId);
            Assert.Null(replacement.RevokedAt);
            Assert.NotEqual(prior.TokenFamilyId, replacement.TokenFamilyId);
            Assert.Equal(1, await database.Users.CountAsync(
                user => user.SignUpId == signUp.Started.SignUpId, Cancellation));
            Assert.Equal(2, await database.UserConsents.CountAsync(
                consent => consent.UserId == retry.Session.UserId, Cancellation));

            SignUp retried = await database.SignUps.AsNoTracking().SingleAsync(
                candidate => candidate.Id == signUp.Started.SignUpId, Cancellation);
            Assert.Equal(retry.Session.SessionId, retried.BootstrapSessionId);
            Assert.Equal(completed.CompletedAt, retried.CompletedAt);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_retry_with_another_profile_keeps_the_provisioned_one()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();

        try
        {
            await Api.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand());
            User provisioned = await ReadUserAsync(signUp.Started.SignUpId);
            List<UserConsent> consents = await ReadConsentsAsync(provisioned.Id);

            CreateSignUpCompletionResult retry = await Api.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand());
            User stored = await ReadUserAsync(signUp.Started.SignUpId);

            Assert.False(retry.Created);
            Assert.Equal(provisioned.Id, stored.Id);
            Assert.Equal(provisioned.FirstName, stored.FirstName);
            Assert.Equal(provisioned.LastName, stored.LastName);
            Assert.Equal(provisioned.Phone.Hash.Value, stored.Phone.Hash.Value);
            Assert.Equal(provisioned.Email.Hash.Value, stored.Email.Hash.Value);
            Assert.Equal(provisioned.Locale, stored.Locale);
            Assert.Equal(Fingerprint(consents), Fingerprint(await ReadConsentsAsync(provisioned.Id)));
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_replacement_leaves_other_sessions_alone_and_the_prior_bootstrap_cannot_refresh()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        CreateSignUpCompletionCommand command = SignUpFlow.CompletionCommand();

        try
        {
            CreateSignUpCompletionResult first = await Api.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, command);
            SessionTokenResult other = await OpenSessionAsync(first.Session.UserId);
            CreateSignUpCompletionCommand retryCommand = SignUpFlow.CompletionCommand();
            CreateSignUpCompletionResult retry = await Api.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, retryCommand);

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Session unrelated = await database.Sessions.AsNoTracking().SingleAsync(
                candidate => candidate.Id == other.SessionId, Cancellation);
            Assert.Null(unrelated.RevokedAt);
            Session replacement = await database.Sessions.AsNoTracking().SingleAsync(
                candidate => candidate.Id == retry.Session.SessionId, Cancellation);
            Assert.Equal(retryCommand.DeviceId, replacement.DeviceId);

            RefusalException refusal = await Assert.ThrowsAsync<RefusalException>(
                () => RefreshAsync(first.Session.RefreshToken, command.DeviceId));
            Assert.Equal(MoniPayErrorTypes.SessionInvalid, refusal.Type);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_retry_over_a_bootstrap_revoked_by_logout_opens_another_one()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();

        try
        {
            CreateSignUpCompletionResult first = await Api.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand());
            await RevokeAsync(first.Session.SessionId);
            CreateSignUpCompletionResult retry = await Api.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand());

            Assert.False(retry.Created);
            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Session revoked = await database.Sessions.AsNoTracking().SingleAsync(
                candidate => candidate.Id == first.Session.SessionId, Cancellation);
            Assert.Equal(SessionRevokeReason.UserRequest, revoked.RevokeReason);
            Session replacement = await database.Sessions.AsNoTracking().SingleAsync(
                candidate => candidate.Id == retry.Session.SessionId, Cancellation);
            Assert.Null(replacement.RevokedAt);
            SignUp row = await database.SignUps.AsNoTracking().SingleAsync(
                candidate => candidate.Id == signUp.Started.SignUpId, Cancellation);
            Assert.Equal(retry.Session.SessionId, row.BootstrapSessionId);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_failure_in_the_session_insert_rolls_back_the_whole_completion()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        CreateSignUpCompletionCommand command = SignUpFlow.CompletionCommand();
        long sessionsBefore = await CountAsync(database => database.Sessions);
        long refreshTokensBefore = await CountAsync(database => database.RefreshTokens);

        using WebApplicationFactory<Program> failing = FailingSessionInsertHost();

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CompleteInAsync(failing, signUp, command));

            await AssertNothingProvisionedAsync(signUp.Started.SignUpId, sessionsBefore, refreshTokensBefore);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_failure_while_replacing_the_bootstrap_restores_the_previous_one()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        CreateSignUpCompletionResult first = await Api.CompleteSignUpAsync(
            signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand());
        SignUp before = await Api.ReadSignUpRowAsync(signUp.Started.SignUpId);
        long sessionsBefore = await CountAsync(database => database.Sessions);

        using WebApplicationFactory<Program> failing = FailingSessionInsertHost();

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CompleteInAsync(failing, signUp, SignUpFlow.CompletionCommand()));

            await using AsyncServiceScope reader = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = reader.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Session prior = await database.Sessions.AsNoTracking().SingleAsync(
                candidate => candidate.Id == first.Session.SessionId, Cancellation);
            Assert.Null(prior.RevokedAt);
            Assert.Null(prior.RevokeReason);
            Assert.Equal(sessionsBefore, await database.Sessions.CountAsync(Cancellation));
            SignUp after = await database.SignUps.AsNoTracking().SingleAsync(
                candidate => candidate.Id == signUp.Started.SignUpId, Cancellation);
            Assert.Equal(before.BootstrapSessionId, after.BootstrapSessionId);
            Assert.Equal(before.Version, after.Version);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task The_user_and_its_consents_keep_the_sign_ups_locale_versions_and_receipt_time()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        DateTimeOffset receipt = Api.Time.GetUtcNow();

        try
        {
            CreateSignUpCompletionResult completed = await Api.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand());

            User user = await ReadUserAsync(signUp.Started.SignUpId);
            Assert.Equal(Locale.FrenchCameroon, user.Locale);
            Assert.Equal(receipt, user.CreatedAt);

            List<UserConsent> consents = await ReadConsentsAsync(completed.Session.UserId);
            Assert.Equal(2, consents.Count);
            Assert.All(consents, consent => Assert.Equal(receipt, consent.AcceptedAt));
            Assert.Contains(consents, consent =>
                consent.DocumentKind == LegalDocumentKind.Terms
                && consent.DocumentVersion == SignUpFlow.TermsVersion);
            Assert.Contains(consents, consent =>
                consent.DocumentKind == LegalDocumentKind.Privacy
                && consent.DocumentVersion == SignUpFlow.PrivacyVersion);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_completion_that_waited_for_the_lock_keeps_the_time_it_was_received()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        await using AsyncServiceScope holder = Api.Services.CreateAsyncScope();
        MoniPayDbContext holderDatabase = holder.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        await using IDbContextTransaction transaction = await holderDatabase.Database.BeginTransactionAsync(Cancellation);
        await holderDatabase.LockSignUpAsync(signUp.Started.SignUpId, Cancellation);
        NpgsqlConnection holderConnection = Assert.IsType<NpgsqlConnection>(holderDatabase.Database.GetDbConnection());

        try
        {
            DateTimeOffset receipt = Api.Time.GetUtcNow();
            Task<CreateSignUpCompletionResult> attempt = Api.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand());
            await Api.WaitUntilBlockedAsync(holderConnection.ProcessID, Cancellation);
            Api.Time.Advance(TimeSpan.FromMinutes(1));
            await transaction.RollbackAsync(Cancellation);

            CreateSignUpCompletionResult completed = await attempt;
            User user = await ReadUserAsync(signUp.Started.SignUpId);
            List<UserConsent> consents = await ReadConsentsAsync(completed.Session.UserId);

            Assert.Equal(receipt, user.CreatedAt);
            Assert.All(consents, consent => Assert.Equal(receipt, consent.AcceptedAt));
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task Completing_with_a_registered_email_refuses_and_creates_neither_user_nor_session()
    {
        string takenEmail = $"taken.{Guid.NewGuid():N}@example.com";
        await Api.RegisterUserAsync(new PhoneNumber(TestPhones.Next()), takenEmail);
        long sessionsBefore = await CountAsync(database => database.Sessions);
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();

        try
        {
            await SignUpFlow.RefusedAsync(
                Api.CompleteSignUpAsync(
                    signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand(takenEmail)),
                MoniPayErrorTypes.EmailAlreadyRegistered);

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.Equal(0, await database.Users.CountAsync(user => user.SignUpId == signUp.Started.SignUpId, Cancellation));
            Assert.Equal(sessionsBefore, await database.Sessions.CountAsync(Cancellation));
            Assert.Equal(SignUpStatus.PhoneVerified, (await Api.ReadSignUpRowAsync(signUp.Started.SignUpId)).Status);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_phone_another_user_took_after_verification_refuses_completion()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        await Api.RegisterUserAsync(signUp.Phone);
        long sessionsBefore = await CountAsync(database => database.Sessions);

        try
        {
            await SignUpFlow.RefusedAsync(
                Api.CompleteSignUpAsync(
                    signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand()),
                MoniPayErrorTypes.PhoneAlreadyRegistered);

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.Equal(0, await database.Users.CountAsync(user => user.SignUpId == signUp.Started.SignUpId, Cancellation));
            Assert.Equal(sessionsBefore, await database.Sessions.CountAsync(Cancellation));
            Assert.Equal(SignUpStatus.PhoneVerified, (await Api.ReadSignUpRowAsync(signUp.Started.SignUpId)).Status);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task Two_sign_ups_racing_for_one_email_leave_one_user_and_one_uncompleted_sign_up()
    {
        VerifiedSignUp first = await Api.StartVerifiedAsync();
        VerifiedSignUp second = await Api.StartVerifiedAsync();
        string email = $"race.{Guid.NewGuid():N}@example.com";

        try
        {
            Task<CreateSignUpCompletionResult> firstAttempt = Api.CompleteSignUpAsync(
                first.Started.SignUpId, first.Verified.RegistrationToken, SignUpFlow.CompletionCommand(email));
            Task<CreateSignUpCompletionResult> secondAttempt = Api.CompleteSignUpAsync(
                second.Started.SignUpId, second.Verified.RegistrationToken, SignUpFlow.CompletionCommand(email.ToUpperInvariant()));
            RefusalException?[] refusals = await Task.WhenAll(
                SignUpFlow.RefusalOfAsync(firstAttempt),
                SignUpFlow.RefusalOfAsync(secondAttempt));

            int loser = refusals[0] is null ? 1 : 0;
            Assert.NotNull(refusals[loser]);
            Assert.Equal(MoniPayErrorTypes.EmailAlreadyRegistered, refusals[loser]!.Type);

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            LookupHash emailHash = Api.Services.GetRequiredService<UserLookupDigest>().Compute(email);
            Assert.Equal(1, await database.Users.CountAsync(
                user => user.Email.Hash.Equals(emailHash), Cancellation));

            VerifiedSignUp lost = loser == 0 ? first : second;
            VerifiedSignUp won = loser == 0 ? second : first;
            Assert.Equal(0, await database.Users.CountAsync(
                user => user.SignUpId == lost.Started.SignUpId, Cancellation));
            Assert.Equal(SignUpStatus.PhoneVerified, (await Api.ReadSignUpRowAsync(lost.Started.SignUpId)).Status);
            UserId winner = await database.Users
                .Where(user => user.SignUpId == won.Started.SignUpId)
                .Select(user => user.Id)
                .SingleAsync(Cancellation);
            Assert.Equal(1, await database.Sessions.CountAsync(session => session.UserId == winner, Cancellation));
            Assert.Equal(1, await database.Notifications.CountAsync(
                row => row.Kind == WelcomeMessageDeliveryAdapter.WelcomeKind
                    && row.CorrelationId == winner.Value, Cancellation));
            Assert.Equal(1, await database.Notifications.CountAsync(
                row => row.Kind == WelcomeMessageDeliveryAdapter.WelcomeKind, Cancellation));
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task Completing_an_expired_sign_up_refuses_and_writes_nothing()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        Api.Time.Advance(SignUpLifetime);

        try
        {
            await SignUpFlow.RefusedAsync(
                Api.CompleteSignUpAsync(
                    signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand()),
                MoniPayErrorTypes.SignUpExpired);

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.Equal(0, await database.Users.CountAsync(user => user.SignUpId == signUp.Started.SignUpId, Cancellation));
            Assert.Equal(SignUpStatus.PhoneVerified, (await Api.ReadSignUpRowAsync(signUp.Started.SignUpId)).Status);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_wrong_registration_token_is_refused()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();

        try
        {
            await SignUpFlow.RefusedAsync(
                Api.CompleteSignUpAsync(signUp.Started.SignUpId, "not-the-registration-token", SignUpFlow.CompletionCommand()),
                MoniPayErrorTypes.RegistrationTokenInvalid);

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.Equal(0, await database.Users.CountAsync(user => user.SignUpId == signUp.Started.SignUpId, Cancellation));
            Assert.Equal(SignUpStatus.PhoneVerified, (await Api.ReadSignUpRowAsync(signUp.Started.SignUpId)).Status);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task Eight_parallel_completions_create_one_user_and_leave_exactly_one_active_bootstrap_session()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        CreateSignUpCompletionCommand command = SignUpFlow.CompletionCommand();

        try
        {
            CreateSignUpCompletionResult?[] outcomes = await Task.WhenAll(
                Enumerable.Range(0, 8).Select(_ => CompleteCatchingAsync(
                    Api, signUp.Started.SignUpId, signUp.Verified.RegistrationToken, command)));

            Assert.All(outcomes, outcome => Assert.NotNull(outcome));
            Assert.Equal(1, outcomes.Count(outcome => outcome is { Created: true }));

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            UserId provisioned = outcomes
                .OfType<CreateSignUpCompletionResult>()
                .Single(outcome => outcome.Created)
                .Session.UserId;
            List<Session> sessions = await database.Sessions.AsNoTracking()
                .Where(candidate => candidate.UserId == provisioned)
                .ToListAsync(Cancellation);

            Assert.Equal(8, sessions.Count);
            Assert.Equal(1, sessions.Count(candidate => candidate.IsActive));
            Assert.Equal(8, sessions.Select(candidate => candidate.TokenFamilyId).Distinct().Count());
            Assert.Equal(1, await database.Users.CountAsync(
                user => user.SignUpId == signUp.Started.SignUpId, Cancellation));

            SignUp row = await Api.ReadSignUpRowAsync(signUp.Started.SignUpId);
            Assert.Contains(row.BootstrapSessionId, sessions.Select(candidate => (Guid?)candidate.Id));
            Assert.True(sessions.Single(candidate => candidate.Id == row.BootstrapSessionId).IsActive);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    private static async Task<CreateSignUpCompletionResult?> CompleteCatchingAsync(
        MoniPayApi api,
        SignUpId signUpId,
        string registrationToken,
        CreateSignUpCompletionCommand command)
    {
        try
        {
            return await api.CompleteSignUpAsync(signUpId, registrationToken, command);
        }
        catch (RefusalException)
        {
            return null;
        }
    }

    private static IEnumerable<(LegalDocumentKind Kind, string Version, DateTimeOffset AcceptedAt)> Fingerprint(
        IEnumerable<UserConsent> consents) =>
        consents.Select(consent => (consent.DocumentKind, consent.DocumentVersion, consent.AcceptedAt));

    private WebApplicationFactory<Program> FailingSessionInsertHost() =>
        Api.CreateHost(builder =>
        {
            builder.UseTestPorts(Api);
            builder.ConfigureServices(services =>
                services.AddSingleton<IDbContextOptionsContributor>(new FailSessionInsertContributor()));
        });

    private async Task<CreateSignUpCompletionResult> CompleteInAsync(
        WebApplicationFactory<Program> host,
        VerifiedSignUp signUp,
        CreateSignUpCompletionCommand command)
    {
        ArgumentNullException.ThrowIfNull(host);

        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CreateSignUpCompletionHandler>()
            .HandleAsync(signUp.Started.SignUpId, signUp.Verified.RegistrationToken, command, Cancellation);
    }

    private async Task AssertNothingProvisionedAsync(
        SignUpId signUpId,
        long sessionsBefore,
        long refreshTokensBefore)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Assert.Equal(0, await database.Users.CountAsync(user => user.SignUpId == signUpId, Cancellation));
        Assert.Equal(sessionsBefore, await database.Sessions.CountAsync(Cancellation));
        Assert.Equal(refreshTokensBefore, await database.RefreshTokens.CountAsync(Cancellation));
        Assert.Equal(0, await database.Notifications.CountAsync(
            row => row.Kind == WelcomeMessageDeliveryAdapter.WelcomeKind, Cancellation));
        Assert.Equal(
            SignUpStatus.PhoneVerified,
            (await database.SignUps.AsNoTracking().SingleAsync(
                candidate => candidate.Id == signUpId, Cancellation)).Status);
    }

    private async Task<long> CountAsync(Func<MoniPayDbContext, IQueryable<object>> set)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        return await set(database).CountAsync(Cancellation);
    }

    private Task<User> ReadUserAsync(SignUpId signUpId) =>
        Api.InScopeAsync<MoniPayDbContext, User>((database, cancellationToken) =>
            database.Users.AsNoTracking().SingleAsync(user => user.SignUpId == signUpId, cancellationToken));

    private Task<List<UserConsent>> ReadConsentsAsync(UserId userId) =>
        Api.InScopeAsync<MoniPayDbContext, List<UserConsent>>((database, cancellationToken) =>
            database.UserConsents.AsNoTracking()
                .Where(consent => consent.UserId == userId)
                .OrderBy(consent => consent.DocumentKind)
                .ToListAsync(cancellationToken));

    private Task<SessionTokenResult> OpenSessionAsync(UserId userId) =>
        Api.InScopeAsync<SessionTokenService, SessionTokenResult>((sessions, cancellationToken) =>
            sessions.CreateAsync(userId, Guid.NewGuid(), cancellationToken));

    private async Task RevokeAsync(Guid sessionId)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SessionTokenService>().RevokeAsync(sessionId, Cancellation);
    }

    private Task<SessionTokenResult> RefreshAsync(string refreshToken, Guid deviceId) =>
        Api.InScopeAsync<SessionTokenService, SessionTokenResult>((sessions, cancellationToken) =>
            sessions.RefreshAsync(refreshToken, deviceId, cancellationToken));

}

file sealed class FailSessionInsertContributor : IDbContextOptionsContributor
{
    public void Contribute(DbContextOptionsBuilder options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.AddInterceptors([new FailSessionInsertInterceptor()]);
    }
}

file sealed class FailSessionInsertInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context && context.ChangeTracker.Entries<Session>().Any())
        {
            throw new InvalidOperationException("Forced failure in the session insert.");
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
