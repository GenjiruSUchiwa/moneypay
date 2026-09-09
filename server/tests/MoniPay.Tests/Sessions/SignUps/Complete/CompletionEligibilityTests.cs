using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Sessions;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Persistence;
using MoniPay.Tests.Support;
using MoniPay.Users.Persistence;
using Npgsql;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Complete;

public sealed class CompletionEligibilityTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static readonly TimeSpan RegistrationLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ShortSignUpLifetime = TimeSpan.FromMinutes(5);

    private const string ClearRegistrationDigestSql =
        $"UPDATE {SessionsSchema.SignUpsTable} SET registration_token_digest = NULL WHERE id = {{0}}";

    private const string SetStatusSql =
        $"UPDATE {SessionsSchema.SignUpsTable} SET status = {{0}} WHERE id = {{1}}";

    public static TheoryData<string, ProblemType> RefusableStates => new()
    {
        { "CodePending", MoniPayErrorTypes.SignUpStateInvalid },
        { "Locked", MoniPayErrorTypes.SignUpStateInvalid },
        { "Expired", MoniPayErrorTypes.SignUpExpired },
    };

    public static TheoryData<TimeSpan, ProblemType?> RegistrationLifetimeBoundaries => new()
    {
        { TimeSpan.FromSeconds(-1), null },
        { TimeSpan.Zero, MoniPayErrorTypes.RegistrationTokenInvalid },
        { TimeSpan.FromSeconds(1), MoniPayErrorTypes.RegistrationTokenInvalid },
    };

    public static TheoryData<TimeSpan, ProblemType?> SignUpLifetimeBoundaries => new()
    {
        { TimeSpan.FromSeconds(-1), null },
        { TimeSpan.Zero, MoniPayErrorTypes.SignUpExpired },
        { TimeSpan.FromSeconds(1), MoniPayErrorTypes.SignUpExpired },
    };

    [Fact]
    public async Task A_sign_up_that_does_not_exist_is_refused()
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        CreateSignUpCompletionHandler handler = scope.ServiceProvider.GetRequiredService<CreateSignUpCompletionHandler>();

        RefusalException refusal = await Assert.ThrowsAsync<RefusalException>(
            () => handler.HandleAsync(
                SignUpId.New(), "a-registration-token", SignUpFlow.CompletionCommand(), Cancellation));

        Assert.Equal(MoniPayErrorTypes.SignUpTokenInvalid, refusal.Type);
    }

    [Fact]
    public async Task A_sign_up_that_persisted_no_registration_credential_is_refused()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        await ExecuteAsync(ClearRegistrationDigestSql, signUp.Started.SignUpId.Value);
        long sessionsBefore = await SessionsAsync();

        try
        {
            await SignUpFlow.RefusedAsync(
                Api.CompleteSignUpAsync(
                    signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand()),
                MoniPayErrorTypes.RegistrationTokenInvalid);

            Assert.Equal(0, await UsersAsync(signUp.Started.SignUpId));
            Assert.Equal(sessionsBefore, await SessionsAsync());
            Assert.Equal(SignUpStatus.PhoneVerified, (await Api.ReadSignUpRowAsync(signUp.Started.SignUpId)).Status);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task Another_sign_ups_registration_credential_is_refused()
    {
        VerifiedSignUp owner = await Api.StartVerifiedAsync();
        VerifiedSignUp other = await Api.StartVerifiedAsync();
        long sessionsBefore = await SessionsAsync();

        try
        {
            await SignUpFlow.RefusedAsync(
                Api.CompleteSignUpAsync(
                    other.Started.SignUpId, owner.Verified.RegistrationToken, SignUpFlow.CompletionCommand()),
                MoniPayErrorTypes.RegistrationTokenInvalid);

            Assert.Equal(0, await UsersAsync(other.Started.SignUpId));
            Assert.Equal(sessionsBefore, await SessionsAsync());
            Assert.Equal(SignUpStatus.PhoneVerified, (await Api.ReadSignUpRowAsync(owner.Started.SignUpId)).Status);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_wrong_credential_is_refused_before_the_state_or_the_lifetimes()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        Api.Time.Advance(RegistrationLifetime);

        try
        {
            await SignUpFlow.RefusedAsync(
                Api.CompleteSignUpAsync(
                    signUp.Started.SignUpId, "not-the-registration-token", SignUpFlow.CompletionCommand()),
                MoniPayErrorTypes.RegistrationTokenInvalid);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Theory]
    [MemberData(nameof(RefusableStates))]
    public async Task A_state_that_cannot_complete_is_refused(string status, ProblemType expected)
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        await ExecuteAsync(SetStatusSql, status, signUp.Started.SignUpId.Value);
        long sessionsBefore = await SessionsAsync();

        try
        {
            await SignUpFlow.RefusedAsync(
                Api.CompleteSignUpAsync(
                    signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand()),
                expected);

            Assert.Equal(0, await UsersAsync(signUp.Started.SignUpId));
            Assert.Equal(sessionsBefore, await SessionsAsync());
            Assert.Equal(status, (await Api.ReadSignUpRowAsync(signUp.Started.SignUpId)).Status.ToString());
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Theory]
    [MemberData(nameof(RegistrationLifetimeBoundaries))]
    public async Task The_registration_lifetime_ends_a_first_completion_at_its_exact_boundary(
        TimeSpan offsetFromLimit,
        ProblemType? expected)
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        Api.Time.Advance(RegistrationLifetime + offsetFromLimit);

        try
        {
            await AssertAtBoundaryAsync(signUp, expected, usersBefore: 0, created: true);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Theory]
    [MemberData(nameof(RegistrationLifetimeBoundaries))]
    public async Task The_registration_lifetime_ends_a_retry_at_its_exact_boundary(
        TimeSpan offsetFromLimit,
        ProblemType? expected)
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        await Api.CompleteSignUpAsync(
            signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand());
        Api.Time.Advance(RegistrationLifetime + offsetFromLimit);

        try
        {
            await AssertAtBoundaryAsync(signUp, expected, usersBefore: 1, created: false);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Theory]
    [MemberData(nameof(SignUpLifetimeBoundaries))]
    public async Task The_sign_up_lifetime_ends_eligibility_at_its_exact_boundary(
        TimeSpan offsetFromLimit,
        ProblemType? expected)
    {
        using WebApplicationFactory<Program> shortLived = Api.CreateHost(builder =>
        {
            builder.UseTestPorts(Api);
            MoniPayApi.UseNotificationTestSettings(builder);
            builder.UseSetting(SessionsOptions.Keys.SignUpLifetime, ShortSignUpLifetime.ToString());
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Api.Time);
            });
        });
        VerifiedSignUp signUp = await Api.StartVerifiedAsync(shortLived.Services);
        Api.Time.Advance(ShortSignUpLifetime + offsetFromLimit);
        long sessionsBefore = await SessionsAsync();

        try
        {
            Task<CreateSignUpCompletionResult> attempt = shortLived.Services.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand());
            if (expected is { } refusal)
            {
                await SignUpFlow.RefusedAsync(attempt, refusal);
                Assert.Equal(0, await UsersAsync(signUp.Started.SignUpId));
                Assert.Equal(sessionsBefore, await SessionsAsync());
                return;
            }

            Assert.True((await attempt).Created);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_completion_that_waited_past_its_window_is_refused()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        await using AsyncServiceScope holder = Api.Services.CreateAsyncScope();
        MoniPayDbContext holderDatabase = holder.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        await using IDbContextTransaction transaction = await holderDatabase.Database.BeginTransactionAsync(Cancellation);
        await holderDatabase.LockSignUpAsync(signUp.Started.SignUpId, Cancellation);
        NpgsqlConnection holderConnection = Assert.IsType<NpgsqlConnection>(holderDatabase.Database.GetDbConnection());
        long sessionsBefore = await SessionsAsync();

        try
        {
            Task<CreateSignUpCompletionResult> attempt = Api.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand());
            await Api.WaitUntilBlockedAsync(holderConnection.ProcessID, Cancellation);

            Api.Time.Advance(RegistrationLifetime);
            await transaction.RollbackAsync(Cancellation);

            await SignUpFlow.RefusedAsync(attempt, MoniPayErrorTypes.RegistrationTokenInvalid);
            Assert.Equal(0, await UsersAsync(signUp.Started.SignUpId));
            Assert.Equal(sessionsBefore, await SessionsAsync());
            Assert.Equal(SignUpStatus.PhoneVerified, (await Api.ReadSignUpRowAsync(signUp.Started.SignUpId)).Status);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    private async Task AssertAtBoundaryAsync(
        VerifiedSignUp signUp,
        ProblemType? expected,
        long usersBefore,
        bool created)
    {
        long sessionsBefore = await SessionsAsync();
        Task<CreateSignUpCompletionResult> attempt = Api.CompleteSignUpAsync(
            signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand());
        if (expected is { } refusal)
        {
            await SignUpFlow.RefusedAsync(attempt, refusal);
            Assert.Equal(usersBefore, await UsersAsync(signUp.Started.SignUpId));
            Assert.Equal(sessionsBefore, await SessionsAsync());
            return;
        }

        Assert.Equal(created, (await attempt).Created);
    }

    private Task<long> UsersAsync(SignUpId signUpId) =>
        Api.InScopeAsync<MoniPayDbContext, long>((database, cancellationToken) =>
            database.Users.LongCountAsync(user => user.SignUpId == signUpId, cancellationToken));

    private Task<long> SessionsAsync() =>
        Api.InScopeAsync<MoniPayDbContext, long>((database, cancellationToken) =>
            database.Sessions.LongCountAsync(cancellationToken));

    private Task ExecuteAsync(string sql, params object[] parameters) =>
        Api.InScopeAsync<MoniPayDbContext, int>((database, cancellationToken) =>
            database.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken));

}
