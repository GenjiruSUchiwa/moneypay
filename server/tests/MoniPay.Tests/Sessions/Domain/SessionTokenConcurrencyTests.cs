using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Persistence;
using MoniPay.Tests.Support;
using Npgsql;
using Xunit;

namespace MoniPay.Tests.Sessions.Domain;

public sealed class SessionTokenConcurrencyTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Revocation_and_replacement_wait_for_an_in_flight_session_update(
        bool firstRevokes,
        bool replaceBootstrap)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(Cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        CancellationToken cancellationToken = timeout.Token;
        await using AsyncServiceScope first = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = first.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Guid deviceId = Guid.CreateVersion7();
        SessionTokenResult created = await first.ServiceProvider.GetRequiredService<SessionTokenService>()
            .CreateAsync(UserId.New(), deviceId, cancellationToken);
        Session session = await database.Sessions.SingleAsync(row => row.Id == created.SessionId, cancellationToken);
        await using IDbContextTransaction transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (firstRevokes)
        {
            session.Revoke(SessionRevokeReason.UserRequest, Api.Time.GetUtcNow());
        }
        else
        {
            session.Touch(Api.Time.GetUtcNow());
        }

        await database.SaveChangesAsync(cancellationToken);
        NpgsqlConnection holder = Assert.IsType<NpgsqlConnection>(database.Database.GetDbConnection());
        await using AsyncServiceScope second = Api.Services.CreateAsyncScope();
        SessionTokenService service = second.ServiceProvider.GetRequiredService<SessionTokenService>();
        Task pending = replaceBootstrap
            ? service.ReplaceBootstrapAsync(created.UserId, created.SessionId, deviceId, cancellationToken)
            : service.RevokeAsync(created.SessionId, cancellationToken);
        try
        {
            await Api.WaitUntilBlockedAsync(holder.ProcessID, cancellationToken);
        }
        finally
        {
            await transaction.CommitAsync(Cancellation);
        }

        await pending;
        await using AsyncServiceScope check = Api.Services.CreateAsyncScope();
        MoniPayDbContext reader = check.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Session revoked = await reader.Sessions.SingleAsync(row => row.Id == created.SessionId, cancellationToken);
        SessionRevokeReason expected = !firstRevokes && replaceBootstrap
            ? SessionRevokeReason.BootstrapReplaced
            : SessionRevokeReason.UserRequest;
        Assert.Equal(expected, revoked.RevokeReason);
        Assert.Equal(firstRevokes ? 2 : 3, revoked.Version);
        Assert.Equal(replaceBootstrap ? 1 : 0, await reader.Sessions.CountAsync(
            row => row.UserId == created.UserId && row.RevokedAt == null, cancellationToken));
    }
}
