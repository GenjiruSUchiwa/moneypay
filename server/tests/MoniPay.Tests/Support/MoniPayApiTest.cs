using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Notifications.Channels;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using Xunit;

namespace MoniPay.Tests.Support;

public abstract class MoniPayApiTest(MoniPayApi api) : IAsyncLifetime
{
    protected MoniPayApi Api { get; } = api;

    protected HttpClient Client { get; } = api.CreateClient();

    protected CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        Api.Time.Reset();

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        await database.Notifications.ExecuteDeleteAsync(Cancellation);
        Api.Sms.Result = new ChannelResult.Accepted("sms-ref");
        Api.Email.Result = new ChannelResult.Accepted("email-ref");
        Api.Sms.SlowRecipient = null;
        Api.Email.SlowRecipient = null;
    }

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }
}
