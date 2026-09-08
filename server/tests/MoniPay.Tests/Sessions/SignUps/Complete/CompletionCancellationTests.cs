using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Api.Composition;
using MoniPay.Kernel;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Persistence;
using MoniPay.Tests.Support;
using MoniPay.Users.Persistence;
using Npgsql;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Complete;

/// <summary>
/// A canceled completion writes nothing, wherever the cancellation lands, and the provisioning
/// port is handed the caller's token rather than a fresh one.
/// </summary>
public sealed class CompletionCancellationTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task The_handler_honors_a_canceled_token()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        CreateSignUpCompletionHandler handler = scope.ServiceProvider.GetRequiredService<CreateSignUpCompletionHandler>();
        using CancellationTokenSource canceled = new();
        await canceled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.HandleAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand(), canceled.Token));
    }

    [Fact]
    public async Task A_request_canceled_while_it_waits_for_the_lock_writes_nothing()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        await using AsyncServiceScope holder = Api.Services.CreateAsyncScope();
        MoniPayDbContext holderDatabase = holder.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        await using IDbContextTransaction transaction = await holderDatabase.Database.BeginTransactionAsync(Cancellation);
        await holderDatabase.LockSignUpAsync(signUp.Started.SignUpId, Cancellation);
        NpgsqlConnection holderConnection = Assert.IsType<NpgsqlConnection>(holderDatabase.Database.GetDbConnection());
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        CreateSignUpCompletionHandler handler = scope.ServiceProvider.GetRequiredService<CreateSignUpCompletionHandler>();
        using CancellationTokenSource cancellation = new();

        try
        {
            Task<CreateSignUpCompletionResult> attempt = handler.HandleAsync(
                signUp.Started.SignUpId,
                signUp.Verified.RegistrationToken,
                SignUpFlow.CompletionCommand(),
                cancellation.Token);
            await Api.WaitUntilBlockedAsync(holderConnection.ProcessID, Cancellation);
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => attempt);
            await transaction.RollbackAsync(Cancellation);

            await using AsyncServiceScope reader = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = reader.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            Assert.Equal(0, await database.Users.CountAsync(user => user.SignUpId == signUp.Started.SignUpId, Cancellation));

            Assert.True((await Api.CompleteSignUpAsync(
                signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand())).Created);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task Cancellation_after_provisioning_commits_nothing()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        long sessionsBefore = await CountAsync(database => database.Sessions);
        using WebApplicationFactory<Program> cancelling = Api.CreateHost(builder =>
        {
            builder.UseTestPorts(Api);
            builder.ConfigureServices(services =>
            {
                services.AddScoped<UserProvisioningAdapter>();
                services.AddScoped<IUserProvisioning, CancellingUserProvisioning>();
            });
        });

        try
        {
            await using AsyncServiceScope scope = cancelling.Services.CreateAsyncScope();
            CreateSignUpCompletionHandler handler = scope.ServiceProvider.GetRequiredService<CreateSignUpCompletionHandler>();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => handler.HandleAsync(
                    signUp.Started.SignUpId, signUp.Verified.RegistrationToken, SignUpFlow.CompletionCommand(), Cancellation));

            await using AsyncServiceScope reader = Api.Services.CreateAsyncScope();
            MoniPayDbContext database = reader.ServiceProvider.GetRequiredService<MoniPayDbContext>();
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
    public async Task The_provisioning_port_receives_the_callers_cancellation_token()
    {
        VerifiedSignUp signUp = await Api.StartVerifiedAsync();
        RecordingUserProvisioning port = new();
        using CancellationTokenSource cancellation = new();
        using WebApplicationFactory<Program> observing = Api.CreateHost(builder =>
        {
            builder.UseTestPorts(Api);
            builder.ConfigureServices(services => services.AddSingleton<IUserProvisioning>(port));
        });

        await using AsyncServiceScope scope = observing.Services.CreateAsyncScope();
        CreateSignUpCompletionHandler handler = scope.ServiceProvider.GetRequiredService<CreateSignUpCompletionHandler>();

        await handler.HandleAsync(
            signUp.Started.SignUpId,
            signUp.Verified.RegistrationToken,
            SignUpFlow.CompletionCommand(),
            cancellation.Token);

        Assert.Equal(cancellation.Token, port.Token);
    }

    private async Task<long> CountAsync(Func<MoniPayDbContext, IQueryable<object>> set)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        return await set(database).CountAsync(Cancellation);
    }
}

/// <summary>
/// Provisions through the real Users slice and then fails the way a request canceled mid-flight
/// would. It adds no rule of its own: the mapping stays the host adapter's.
/// </summary>
file sealed class CancellingUserProvisioning(UserProvisioningAdapter inner) : IUserProvisioning
{
    public async Task<UserId> ProvisionAsync(ProvisionUserRequest request, CancellationToken cancellationToken)
    {
        UserId userId = await inner.ProvisionAsync(request, cancellationToken).ConfigureAwait(false);
        throw new OperationCanceledException(cancellationToken);
    }
}

/// <summary>
/// The provisioning port as a test double: it records the token it was handed and answers with an
/// identity, without writing, so a test can assert forwarding alone.
/// </summary>
file sealed class RecordingUserProvisioning : IUserProvisioning
{
    public CancellationToken Token { get; private set; }

    public UserId Id { get; } = UserId.New();

    public Task<UserId> ProvisionAsync(ProvisionUserRequest request, CancellationToken cancellationToken)
    {
        Token = cancellationToken;
        return Task.FromResult(Id);
    }
}
