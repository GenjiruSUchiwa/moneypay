using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MoniPay.Api;
using MoniPay.Kernel.Http;
using MoniPay.Sessions;
using MoniPay.Users;
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
            builder.UseSetting(UsersOptions.Keys.PersonalDataKeyBase64, TestKeys.UsersPersonalData);
            builder.UseSetting(SessionsOptions.Keys.MaximumVerificationAttempts, "3");
            builder.UseSetting(SessionsOptions.Keys.VerificationCodeLifetime, "00:02:00");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Time);
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
