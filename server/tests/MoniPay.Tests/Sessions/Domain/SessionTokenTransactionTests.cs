using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Persistence;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Domain;

public sealed class SessionTokenTransactionTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Creation_and_replacement_roll_back_with_the_callers_transaction(bool replaceBootstrap)
    {
        UserId userId = UserId.New();
        Guid deviceId = Guid.CreateVersion7();
        SessionTokenResult bootstrap;
        await using (AsyncServiceScope seed = Api.Services.CreateAsyncScope())
        {
            bootstrap = await seed.ServiceProvider.GetRequiredService<SessionTokenService>()
                .CreateAsync(userId, deviceId, Cancellation);
        }

        SessionTokenResult created;
        await using (AsyncServiceScope scope = Api.Services.CreateAsyncScope())
        {
            MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            SessionTokenService service = scope.ServiceProvider.GetRequiredService<SessionTokenService>();
            await using IDbContextTransaction transaction = await database.Database.BeginTransactionAsync(Cancellation);
            created = replaceBootstrap
                ? await service.ReplaceBootstrapAsync(userId, bootstrap.SessionId, deviceId, Cancellation)
                : await service.CreateAsync(userId, deviceId, Cancellation);

            Assert.Same(transaction, database.Database.CurrentTransaction);
            Assert.True(await database.Sessions.AnyAsync(session => session.Id == created.SessionId, Cancellation));
            await transaction.RollbackAsync(Cancellation);
        }

        await using AsyncServiceScope check = Api.Services.CreateAsyncScope();
        MoniPayDbContext reader = check.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Assert.False(await reader.Sessions.AnyAsync(session => session.Id == created.SessionId, Cancellation));
        Assert.False(await reader.RefreshTokens.AnyAsync(token => token.SessionId == created.SessionId, Cancellation));
        Session prior = await reader.Sessions.SingleAsync(session => session.Id == bootstrap.SessionId, Cancellation);
        Assert.True(prior.IsActive);
    }
}
