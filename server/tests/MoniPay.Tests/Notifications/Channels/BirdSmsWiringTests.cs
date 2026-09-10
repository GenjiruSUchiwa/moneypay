using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Api;
using MoniPay.Notifications;
using MoniPay.Notifications.Channels;
using MoniPay.Notifications.Domain;
using MoniPay.Notifications.Features.Deliver;
using MoniPay.Notifications.Persistence;
using MoniPay.Persistence;
using MoniPay.Tests.Fakes;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Notifications.Channels;

public sealed class BirdSmsWiringTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task A_delivery_cycle_reaches_the_adapter_and_persists_its_result()
    {
        StubHandler stub = new(HttpStatusCode.Accepted, """{"id":"sms_01wiringtest00000000000001","status":"scheduled"}""");
        using WebApplicationFactory<Program> host = Api.CreateHost(builder => builder.ConfigureServices(services =>
        {
            services.AddHttpClient<BirdSmsChannel>().ConfigurePrimaryHttpMessageHandler(() => stub);
        }));
        host.CreateClient().Dispose();

        string key = $"wire-{Guid.CreateVersion7()}";
        Guid correlation = Guid.CreateVersion7();
        await EnqueueAsync(host, new OutboundMessage(
            NotificationChannel.Sms,
            TestPhones.Next(),
            null,
            "Votre code MoniPay est le 041822, valable 5 minutes.",
            RetrySchedule.VerificationCodeKind,
            true,
            key,
            null,
            correlation));

        int delivered = await DeliverAsync(host);

        Assert.Equal(1, delivered);
        StubHandler.RequestSnapshot snapshot = Assert.Single(stub.Snapshots);
        Assert.Equal(TestKeys.SmsBaseUrl + "/v1/sms/messages", snapshot.Uri?.ToString());
        Assert.Equal("Bearer " + TestKeys.SmsApiKey, snapshot.Authorization);
        Assert.Equal(key, snapshot.IdempotencyKey);
        Assert.Contains("041822", snapshot.Body, StringComparison.Ordinal);

        Notification row = await RowAsync(host, correlation);
        Assert.Equal(NotificationStatus.Sent, row.Status);
        Assert.Equal("sms_01wiringtest00000000000001", row.ProviderReference);
        Assert.Null(row.BodyCiphertext);
    }

    private async Task EnqueueAsync(WebApplicationFactory<Program> host, OutboundMessage message)
    {
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<NotificationOutbox>().Enqueue(message);
        await scope.ServiceProvider.GetRequiredService<MoniPayDbContext>().SaveChangesAsync(Cancellation);
    }

    private async Task<int> DeliverAsync(WebApplicationFactory<Program> host)
    {
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        INotificationChannel channel = scope.ServiceProvider
            .GetRequiredKeyedService<INotificationChannel>(NotificationChannel.Sms);
        Assert.IsType<BirdSmsChannel>(channel);
        return await scope.ServiceProvider.GetRequiredService<NotificationProcessor>().RunCycleAsync(Cancellation);
    }

    private async Task<Notification> RowAsync(WebApplicationFactory<Program> host, Guid correlation)
    {
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<MoniPayDbContext>().Notifications
            .AsNoTracking()
            .SingleAsync(notification => notification.CorrelationId == correlation, Cancellation);
    }
}
