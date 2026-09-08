using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Features.SignUps.VerifyPhone;
using MoniPay.Sessions.Persistence;
using MoniPay.Tests.Support;
using MoniPay.Users.Persistence;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Complete;

public sealed class CreateSignUpCompletionTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static readonly TimeSpan SignUpLifetime = TimeSpan.FromMinutes(15);
    private static int sequence;

    [Fact]
    public async Task A_first_completion_provisions_the_user_opens_the_bootstrap_session_and_completes_the_sign_up()
    {
        (StartSignUpResult started, CreatePhoneVerificationResult verified) = await StartVerifiedAsync();
        CreateSignUpCompletionCommand command = Command();

        try
        {
            CreateSignUpCompletionResult completed = await Api.CompleteSignUpAsync(started.SignUpId, verified.RegistrationToken, command);

            Assert.True(completed.Created);
            Assert.NotEmpty(completed.Session.RefreshToken);
            Assert.True(completed.Session.AccessTokenExpiresAt > Api.Time.GetUtcNow());
            Assert.True(completed.Session.RefreshTokenExpiresAt > Api.Time.GetUtcNow());

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.Equal(1, await database.Users.CountAsync(user => user.SignUpId == started.SignUpId, Cancellation));
            Assert.Equal(2, await database.UserConsents.CountAsync(consent => consent.UserId == completed.Session.UserId, Cancellation));
            Session session = await database.Sessions.AsNoTracking().SingleAsync(
                candidate => candidate.Id == completed.Session.SessionId, Cancellation);
            Assert.Null(session.RevokedAt);
            Assert.Equal(completed.Session.UserId, session.UserId);
            Assert.Equal(command.DeviceId, session.DeviceId);
            Assert.Equal(1, await database.RefreshTokens.CountAsync(
                token => token.SessionId == completed.Session.SessionId && token.UsedAt == null, Cancellation));
            SignUp signUp = await database.SignUps.AsNoTracking().SingleAsync(
                candidate => candidate.Id == started.SignUpId, Cancellation);
            Assert.Equal(SignUpStatus.Completed, signUp.Status);
            Assert.Equal(completed.Session.UserId, signUp.ProvisionedUserId);
            Assert.Equal(completed.Session.SessionId, signUp.BootstrapSessionId);
        }
        finally
        {
            await CleanUsersTablesAsync();
        }
    }

    [Fact]
    public async Task A_retry_with_the_same_registration_token_replaces_only_the_bootstrap_session()
    {
        (StartSignUpResult started, CreatePhoneVerificationResult verified) = await StartVerifiedAsync();
        CreateSignUpCompletionCommand command = Command();

        try
        {
            CreateSignUpCompletionResult first = await Api.CompleteSignUpAsync(started.SignUpId, verified.RegistrationToken, command);
            CreateSignUpCompletionResult retry = await Api.CompleteSignUpAsync(started.SignUpId, verified.RegistrationToken, command);

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
            Assert.Equal(1, await database.Users.CountAsync(user => user.SignUpId == started.SignUpId, Cancellation));
        }
        finally
        {
            await CleanUsersTablesAsync();
        }
    }

    [Fact]
    public async Task A_failure_in_the_session_insert_rolls_back_the_whole_completion()
    {
        (StartSignUpResult started, CreatePhoneVerificationResult verified) = await StartVerifiedAsync();
        CreateSignUpCompletionCommand command = Command();

        long sessionsBefore;
        long refreshTokensBefore;
        await using (AsyncServiceScope snapshot = Api.Services.CreateAsyncScope())
        {
            MoniPayDbContext database = snapshot.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            sessionsBefore = await database.Sessions.CountAsync(Cancellation);
            refreshTokensBefore = await database.RefreshTokens.CountAsync(Cancellation);
        }

        using WebApplicationFactory<Program> failing = Api.CreateHost(builder =>
        {
            builder.UseTestPorts(Api);
            builder.ConfigureServices(services =>
                services.AddSingleton<IDbContextOptionsContributor>(new FailSessionInsertContributor()));
        });

        try
        {
            await using AsyncServiceScope scope = failing.Services.CreateAsyncScope();
            CreateSignUpCompletionHandler handler = scope.ServiceProvider.GetRequiredService<CreateSignUpCompletionHandler>();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(started.SignUpId, verified.RegistrationToken, command, Cancellation));

            await using AsyncServiceScope reader = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = reader.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.Equal(0, await database.Users.CountAsync(user => user.SignUpId == started.SignUpId, Cancellation));
            Assert.Equal(sessionsBefore, await database.Sessions.CountAsync(Cancellation));
            Assert.Equal(refreshTokensBefore, await database.RefreshTokens.CountAsync(Cancellation));
            Assert.Equal(
                SignUpStatus.PhoneVerified,
                (await database.SignUps.AsNoTracking().SingleAsync(candidate => candidate.Id == started.SignUpId, Cancellation)).Status);
        }
        finally
        {
            await CleanUsersTablesAsync();
        }
    }

    [Fact]
    public async Task Completing_with_a_registered_email_refuses_and_creates_neither_user_nor_session()
    {
        string takenEmail = $"taken.{Interlocked.Increment(ref sequence)}@example.com";
        await Api.RegisterUserAsync(new PhoneNumber(TestPhones.Next()), takenEmail);

        long sessionsBefore;
        await using (AsyncServiceScope snapshot = Api.Services.CreateAsyncScope())
        {
            MoniPayDbContext database = snapshot.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            sessionsBefore = await database.Sessions.CountAsync(Cancellation);
        }

        (StartSignUpResult started, CreatePhoneVerificationResult verified) = await StartVerifiedAsync();

        try
        {
            await SignUpFlow.RefusedAsync(
                Api.CompleteSignUpAsync(started.SignUpId, verified.RegistrationToken, Command(takenEmail)),
                MoniPayErrorTypes.EmailAlreadyRegistered);

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.Equal(0, await database.Users.CountAsync(user => user.SignUpId == started.SignUpId, Cancellation));
            Assert.Equal(sessionsBefore, await database.Sessions.CountAsync(Cancellation));
            Assert.Equal(
                SignUpStatus.PhoneVerified,
                (await database.SignUps.AsNoTracking().SingleAsync(candidate => candidate.Id == started.SignUpId, Cancellation)).Status);
        }
        finally
        {
            await CleanUsersTablesAsync();
        }
    }

    [Fact]
    public async Task Completing_an_expired_sign_up_refuses_and_writes_nothing()
    {
        (StartSignUpResult started, CreatePhoneVerificationResult verified) = await StartVerifiedAsync();
        Api.Time.Advance(SignUpLifetime);

        try
        {
            await SignUpFlow.RefusedAsync(
                Api.CompleteSignUpAsync(started.SignUpId, verified.RegistrationToken, Command()),
                MoniPayErrorTypes.SignUpExpired);

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.Equal(0, await database.Users.CountAsync(user => user.SignUpId == started.SignUpId, Cancellation));
            Assert.Equal(SignUpStatus.PhoneVerified, (await Api.ReadSignUpRowAsync(started.SignUpId)).Status);
        }
        finally
        {
            await CleanUsersTablesAsync();
        }
    }

    [Fact]
    public async Task A_wrong_registration_token_is_refused()
    {
        (StartSignUpResult started, CreatePhoneVerificationResult verified) = await StartVerifiedAsync();

        try
        {
            await SignUpFlow.RefusedAsync(
                Api.CompleteSignUpAsync(started.SignUpId, "not-the-registration-token", Command()),
                MoniPayErrorTypes.RegistrationTokenInvalid);

            await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.Equal(0, await database.Users.CountAsync(user => user.SignUpId == started.SignUpId, Cancellation));
            Assert.Equal(SignUpStatus.PhoneVerified, (await Api.ReadSignUpRowAsync(started.SignUpId)).Status);
        }
        finally
        {
            await CleanUsersTablesAsync();
        }
    }

    [Fact]
    public async Task Eight_parallel_completions_create_one_user_and_leave_exactly_one_active_bootstrap_session()
    {
        (StartSignUpResult started, CreatePhoneVerificationResult verified) = await StartVerifiedAsync();
        CreateSignUpCompletionCommand command = Command();

        try
        {
            CreateSignUpCompletionResult?[] outcomes = await Task.WhenAll(
                Enumerable.Range(0, 8).Select(_ => CompleteCatchingAsync(Api, started.SignUpId, verified.RegistrationToken, command)));

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
            Assert.Equal(1, await database.Users.CountAsync(user => user.SignUpId == started.SignUpId, Cancellation));
        }
        finally
        {
            await CleanUsersTablesAsync();
        }
    }

    [Fact]
    public async Task The_handler_honors_a_canceled_token()
    {
        (StartSignUpResult started, CreatePhoneVerificationResult verified) = await StartVerifiedAsync();
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        CreateSignUpCompletionHandler handler = scope.ServiceProvider.GetRequiredService<CreateSignUpCompletionHandler>();
        using CancellationTokenSource canceled = new();
        await canceled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.HandleAsync(started.SignUpId, verified.RegistrationToken, Command(), canceled.Token));
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

    private static CreateSignUpCompletionCommand Command(string? email = null)
    {
        int unique = Interlocked.Increment(ref sequence);
        return new CreateSignUpCompletionCommand(
            new PersonName("Marie"),
            new PersonName("Ngo"),
            new EmailAddress(email ?? $"complete.marie{unique}@example.com"),
            Guid.NewGuid());
    }

    private async Task<(StartSignUpResult Started, CreatePhoneVerificationResult Verified)> StartVerifiedAsync()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        CreatePhoneVerificationResult verified = await Api.VerifyPhoneAsync(started.SignUpId, Api.Sender.CodeFor(phone));
        return (started, verified);
    }

    private Task CleanUsersTablesAsync() =>
        Api.QueryAsync("TRUNCATE TABLE user_consents, users;", reader => 0);
}

/// <summary>
/// Forces the session insert to fail inside the completion's transaction, so a test can watch the
/// whole completion roll back. A contributor, because the data module is what builds the options.
/// </summary>
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
