using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Notifications.Channels;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using Xunit;

namespace MoniPay.Tests.Support;

/// <summary>Common access to the shared MoniPay test host and its cancellation token.</summary>
public abstract class MoniPayApiTest(MoniPayApi api) : IAsyncLifetime
{
    protected MoniPayApi Api { get; } = api;

    protected HttpClient Client { get; } = api.CreateClient();

    protected CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        // JWT validation uses system time; an earlier test's clock advance must not date new tokens in the future.
        Api.Time.Reset();

        // Tests run serially; each scenario owns the entire notification queue.
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
