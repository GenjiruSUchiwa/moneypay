using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MoniPay.Api;
using MoniPay.Kernel.Http;
using MoniPay.Notifications;
using MoniPay.Notifications.Channels;
using MoniPay.Notifications.Features.Deliver;
using MoniPay.Notifications.Features.Purge;
using MoniPay.Sessions;
using MoniPay.Sessions.Persistence;
using MoniPay.Tests.Fakes;
using Testcontainers.PostgreSql;
using Xunit;

[assembly: AssemblyFixture(typeof(MoniPay.Tests.Support.MoniPayApi))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MoniPay.Tests.Support;

/// <summary>
/// The shared HTTP test host backed by one PostgreSQL container for the whole test assembly.
/// </summary>
public sealed class MoniPayApi : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("monipay")
        .WithUsername("monipay")
        .WithPassword("monipay")
        .Build();

    private WebApplicationFactory<Program>? factory;

    public TestTimeProvider Time { get; } = new();

    /// <summary>Stands in for the host's delivery adapter until it lands; tests read codes from here.</summary>
    public RecordingVerificationCodeSender Sender { get; } = new();

    /// <summary>Stands in for the host's Users adapter until it lands.</summary>
    public StubRegisteredPhoneLookup RegisteredPhones { get; } = new();

    /// <summary>The channels the delivery worker sends through; tests read what was sent from here.</summary>
    public RecordingChannel Sms { get; } = new("sms-ref");

    public RecordingChannel Email { get; } = new("email-ref");

    public RecordingLoggerProvider Logs { get; } = new();

    internal string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();

        ConnectionString = container.GetConnectionString();
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(MoniPayEnvironments.Testing);
            builder.UseSetting("ConnectionStrings:MoniPay", ConnectionString);
            builder.UseSetting(MoniPayConfiguration.ApplyMigrationsOnStartup, "true");
            builder.UseTestKeys();
            builder.UseTestPorts(this);
            builder.UseSetting(SessionsOptions.Keys.MaximumVerificationAttempts, "3");
            builder.UseSetting(SessionsOptions.Keys.VerificationCodeLifetime, "00:02:00");
            builder.UseSetting(SessionsOptions.Keys.MaximumStartsPerWindow, "3");
            builder.UseSetting(SessionsOptions.Keys.CleanupEnabled, "false");
            builder.UseSetting(SessionsOptions.Keys.CleanupBatchSize, "5");
            UseNotificationTestSettings(builder);

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Time);
                services.AddKeyedSingleton<INotificationChannel>(NotificationChannel.Sms, Sms);
                services.AddKeyedSingleton<INotificationChannel>(NotificationChannel.Email, Email);
                services.AddLogging(logging => logging.AddProvider(Logs));
            });
        });
    }

    public HttpClient CreateClient()
    {
        WebApplicationFactory<Program> currentFactory = factory ?? throw new InvalidOperationException(
            "The MoniPay test host has not been initialized.");
        HttpClient client = currentFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.Accept);
        return client;
    }

    /// <summary>
    /// The worker and the purge are off in the test host; both are driven one cycle at a time.
    /// </summary>
    public static IWebHostBuilder UseNotificationTestSettings(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSetting(NotificationsOptions.Keys.WorkerEnabled, "false");
        builder.UseSetting(NotificationsOptions.Keys.WorkerBatchSize, "10");
        return builder;
    }

    /// <summary>One delivery cycle in a fresh scope, exactly as the worker runs it.</summary>
    public async Task<int> RunNotificationCycleAsync(CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<NotificationProcessor>().RunCycleAsync(cancellationToken);
    }

    /// <summary>One purge sweep in a fresh scope, exactly as the worker runs it; returns how many rows it deleted.</summary>
    public async Task<int> RunNotificationPurgeAsync(CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<NotificationPurger>().RunAsync(cancellationToken);
    }

    /// <summary>One expired-credential sweep, exactly as the worker runs it; returns how many rows it deleted.</summary>
    public Task<int> RunCleanupCycleAsync(CancellationToken cancellationToken = default) =>
        Services.GetRequiredService<ExpiredCredentialCleanupService>().RunCycleAsync(cancellationToken);

    public IServiceProvider Services =>
        (factory ?? throw new InvalidOperationException(
            "The MoniPay test host has not been initialized.")).Services;

    public async ValueTask DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }
}
