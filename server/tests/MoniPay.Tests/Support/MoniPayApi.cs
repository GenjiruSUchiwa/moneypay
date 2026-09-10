using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MoniPay.Api;
using MoniPay.Kernel;
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

public sealed class MoniPayApi : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("monipay")
        .WithUsername("monipay")
        .WithPassword("monipay")
        .Build();

    private WebApplicationFactory<Program>? factory;

    public TestTimeProvider Time { get; } = new();

    public SessionQueryCounter Queries { get; } = new();

    public RecordingChannel Sms { get; } = new("sms-ref");

    public RecordingChannel Email { get; } = new("email-ref");

    public RecordingLoggerProvider Logs { get; } = new();

    internal string ConnectionString { get; private set; } = string.Empty;

    private readonly List<WebApplicationFactory<Program>> secondaryFactories = [];

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
            builder.UseSetting(SessionsOptions.Keys.LegalTermsVersion, SignUpFlow.TermsVersion);
            builder.UseSetting(SessionsOptions.Keys.LegalPrivacyVersion, SignUpFlow.PrivacyVersion);
            builder.UseSetting(SessionsOptions.Keys.CleanupEnabled, "false");
            builder.UseSetting(SessionsOptions.Keys.CleanupBatchSize, "5");
            builder.UseSetting(MoniPayConfiguration.ForwardedHeadersKnownProxies, "127.0.0.1,::1");
            UseNotificationTestSettings(builder);

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Time);
                UseStubSmsChannel(services);
                services.RemoveAllKeyed<INotificationChannel>(NotificationChannel.Email);
                services.AddKeyedSingleton<INotificationChannel>(NotificationChannel.Email, Email);
                services.AddLogging(logging => logging.AddProvider(Logs));
                UseSessionQueryCounter(services);
            });
        });
    }

    public WebApplicationFactory<Program> CreateHost(
        Action<IWebHostBuilder>? customize = null,
        bool stubSmsChannel = true)
    {
        WebApplicationFactory<Program> secondary = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(MoniPayEnvironments.Testing);
            builder.UseSetting("ConnectionStrings:MoniPay", ConnectionString);
            builder.UseTestKeys();
            builder.UseSetting(SessionsOptions.Keys.LegalTermsVersion, SignUpFlow.TermsVersion);
            builder.UseSetting(SessionsOptions.Keys.LegalPrivacyVersion, SignUpFlow.PrivacyVersion);
            UseNotificationTestSettings(builder);
            customize?.Invoke(builder);
            builder.ConfigureServices(services =>
            {
                if (stubSmsChannel)
                {
                    UseStubSmsChannel(services);
                }

                UseSessionQueryCounter(services);
            });
        });
        secondaryFactories.Add(secondary);
        return secondary;
    }

    private void UseSessionQueryCounter(IServiceCollection services)
    {
        services.AddSingleton(Queries);
        services.AddSingleton<IDbContextOptionsContributor>(new SessionQueryCountContributor(Queries));
    }

    public HttpClient CreateClient()
    {
        WebApplicationFactory<Program> currentFactory = factory ?? throw new InvalidOperationException(
            "The MoniPay test host has not been initialized.");
        HttpClient client = currentFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.Accept);
        return client;
    }

    private void UseStubSmsChannel(IServiceCollection services)
    {
        services.RemoveAllKeyed<INotificationChannel>(NotificationChannel.Sms);
        services.AddKeyedSingleton<INotificationChannel>(NotificationChannel.Sms, Sms);
    }

    public static IWebHostBuilder UseNotificationTestSettings(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSetting(NotificationsOptions.Keys.WorkerEnabled, "false");
        builder.UseSetting(NotificationsOptions.Keys.WorkerBatchSize, "10");
        return builder;
    }

    public async Task<int> RunNotificationCycleAsync(CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<NotificationProcessor>().RunCycleAsync(cancellationToken);
    }

    public async Task<int> RunNotificationPurgeAsync(CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<NotificationPurger>().RunAsync(cancellationToken);
    }

    public Task<int> RunCleanupCycleAsync(CancellationToken cancellationToken = default) =>
        Services.GetRequiredService<ExpiredCredentialCleanupService>().RunCycleAsync(cancellationToken);

    public IServiceProvider Services =>
        (factory ?? throw new InvalidOperationException(
            "The MoniPay test host has not been initialized.")).Services;

    public async ValueTask DisposeAsync()
    {
        foreach (WebApplicationFactory<Program> secondary in secondaryFactories)
        {
            await secondary.DisposeAsync();
        }

        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }
}
