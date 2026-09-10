using System.Net;
using Microsoft.AspNetCore.Hosting;
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

namespace MoniPay.Tests.Notifications.Deliver;

public sealed class BirdEmailWorkerTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task The_email_key_resolves_the_typed_adapter_and_a_worker_cycle_persists_its_result()
    {
        StubHandler stub = new(HttpStatusCode.Accepted, """{"id":"em_01worker","status":"accepted"}""");
        using WebApplicationFactory<Program> factory = Api.CreateHost(builder =>
        {
            MoniPayApi.UseNotificationTestSettings(builder);
            builder.ConfigureServices(services => services.AddHttpClient<BirdEmailChannel>()
                .ConfigurePrimaryHttpMessageHandler(() => stub));
        });

        string email = $"worker.{Guid.NewGuid():N}@example.com";
        string key = $"worker-{Guid.CreateVersion7()}";
        await EnqueueAsync(factory.Services, email, key);

        INotificationChannel? channel;
        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            channel = scope.ServiceProvider.GetKeyedService<INotificationChannel>(NotificationChannel.Email);
        }

        Assert.IsType<BirdEmailChannel>(channel);

        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            Assert.True(await scope.ServiceProvider.GetRequiredService<NotificationProcessor>().RunCycleAsync(Cancellation) >= 1);
        }

        Notification row = await RowAsync(key);
        Assert.True(
            row.Status == NotificationStatus.Sent,
            $"Status={row.Status} Attempts={row.Attempts} Error={row.LastErrorCode} StubCalls={stub.Requests.Count}");
        Assert.Equal("em_01worker", row.ProviderReference);
        Assert.Null(row.BodyCiphertext);
        HttpRequestMessage request = Assert.Single(stub.Requests);
        Assert.Equal("/v1/email/messages", request.RequestUri?.AbsolutePath);
        Assert.Equal([key], request.Headers.GetValues("Idempotency-Key"));
    }

    private async Task EnqueueAsync(IServiceProvider services, string email, string key)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        scope.ServiceProvider.GetRequiredService<NotificationOutbox>().Enqueue(new OutboundMessage(
            Channel: NotificationChannel.Email,
            Recipient: email,
            Subject: "Bienvenue sur MoniPay",
            Body: "Votre compte est prêt.",
            Kind: "Welcome",
            Required: false,
            IdempotencyKey: key,
            ExpiresAt: null,
            CorrelationId: Guid.CreateVersion7()));
        await database.SaveChangesAsync(Cancellation);
    }

    private async Task<Notification> RowAsync(string key)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<MoniPayDbContext>().Notifications
            .AsNoTracking()
            .SingleAsync(row => row.IdempotencyKey == key, Cancellation);
    }

}
