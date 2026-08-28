---
title: One Test Project, a Real PostgreSQL, a Frozen Clock
impact: HIGH
impactDescription: Tests that drive the real host against a real database catch what a mocked DbContext never will
tags: testing, xunit, testcontainers, postgresql, webapplicationfactory, faketimeprovider
---

## One Test Project, a Real PostgreSQL, a Frozen Clock

**Impact: HIGH**

`server/tests/MoniPay.Tests/` is the whole suite — one project, not one per module. A wallet test
that stops at the service has not proved the endpoint authorises, the JSON serialises, or the
migration created the column. The suite drives **the real host over HTTP against a real PostgreSQL**,
with only the two provider ports (Campay, Sudo) replaced by fakes.

**The runner.** xunit.v3 runs on Microsoft.Testing.Platform, and `server/dotnet.config` selects it:

```ini
# Without this, "dotnet test" hands the assembly to VSTest, which discovers no test at all
# and still reports success.
[dotnet.test.runner]
name = "Microsoft.Testing.Platform"
```

```xml
<!-- server/tests/MoniPay.Tests/MoniPay.Tests.csproj -->
<PropertyGroup>
  <IsPackable>false</IsPackable>
  <OutputType>Exe</OutputType>
  <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
  <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />
  <PackageReference Include="Testcontainers.PostgreSql" />
  <PackageReference Include="xunit.v3" />
</ItemGroup>
```

**Layout:**

```
server/tests/MoniPay.Tests/
  Support/      MoniPayApi.cs (the host + container), MoniPayApiTest.cs, HttpJson.cs, TestTimeProvider.cs
  Fakes/        InMemoryCampay.cs, InMemorySudo.cs, RecordingLogger.cs
  Wallet/  TopUps/  Cards/  Fx/       one folder per module
```

**Incorrect (mocking the database, asserting the mock):**

```csharp
[Fact]
public async Task Credits_the_wallet()
{
    var database = new Mock<MoniPayDbContext>();               // mocks EF, proves nothing
    var service = new WalletService(database.Object, TimeProvider.System); // wall clock: flaky
    await service.AppendAsync(userId, 25, WalletEntryKind.TopUp, "ref-1", default);
    database.Verify(d => d.SaveChangesAsync(default), Times.Once);  // asserts a call, not a balance
}
```

The unique index that actually prevents the double credit is never exercised.

**Correct (real host, real database, fake providers, frozen clock):**

```csharp
// server/tests/MoniPay.Tests/Support/MoniPayApi.cs
public sealed class MoniPayApi : IAsyncLifetime
{
    /// <summary>Test-only key. The value is irrelevant; that it decodes to 32 bytes is not.</summary>
    public static readonly string EncryptionKeyBase64 =
        Convert.ToBase64String(Encoding.UTF8.GetBytes("monipay-integration-test-key-32b"));

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("monipay").WithUsername("monipay").WithPassword("monipay").Build();

    private WebApplicationFactory<Program>? factory;

    public TestTimeProvider Time { get; } = new();

    public InMemoryCampay Campay { get; } = new();

    public InMemorySudo Sudo { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:MoniPay", container.GetConnectionString());
            builder.UseSetting("MoniPay:ApplyMigrationsOnStartup", "true");
            builder.UseSetting("MoniPay:Encryption:KeyBase64", EncryptionKeyBase64);
            // The Campay demo cap the production code must respect. It differs from the default
            // on purpose: a test asserting the default would pass even if nothing read the option.
            builder.UseSetting("MoniPay:Campay:MaximumXaf", "25");
            // Workers are driven cycle by cycle, never raced.
            builder.UseSetting("MoniPay:Outbox:Enabled", "false");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Time);
                services.RemoveAll<ICollectFunds>();
                services.AddSingleton<ICollectFunds>(Campay);
                services.RemoveAll<IIssueCards>();
                services.AddSingleton<IIssueCards>(Sudo);
            });
        });
    }

    public HttpClient CreateClient() => factory!.CreateClient();

    public void Reset() { Time.Reset(); Campay.Reset(); Sudo.Reset(); }

    public async ValueTask DisposeAsync()
    {
        if (factory is not null) { await factory.DisposeAsync(); }
        await container.DisposeAsync();
    }
}
```

```csharp
// server/tests/MoniPay.Tests/TopUps/TopUpIdempotencyTests.cs
public sealed class TopUpIdempotencyTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task A_replayed_campay_callback_credits_the_wallet_once()
    {
        var user = await Api.SignUpAsync();
        var topUp = await Api.StartTopUpAsync(user, amountXaf: 25);

        Api.Campay.Settle(topUp.ProviderReference, CollectionOutcome.Succeeded);
        await Api.RunOutboxCycleAsync();
        await Api.RunOutboxCycleAsync();          // the replay

        var wallet = await Api.GetWalletAsync(user);
        Assert.Equal(25, wallet.BalanceXaf);
    }
}
```

**Rules:**

- **One container for the whole suite.** Isolation comes from each test creating its own user, since
  every row is user-scoped; a container per test turns a two-minute suite into twenty.
- **`FakeTimeProvider` behind a swappable `TestTimeProvider`.** A `FakeTimeProvider` refuses to move
  backwards, so isolating tests means replacing the instance, not rewinding it. Nothing in the suite
  sleeps on the wall clock.
- **Fakes, not mocks.** `InMemoryCampay` implements `ICollectFunds` and records what was asked; the
  assertion is on the resulting balance, not on a `Verify`.
- **Test values differ from defaults on purpose.** Asserting a default value passes even when nothing
  read the configuration.
- **Workers are off in the test host** and driven explicitly, so a test never races a loop.
- **Docker is required.** `dotnet test server/MoniPay.slnx` fails without a running daemon — that is
  the trade for testing against the real schema.

Reference: [Testcontainers for .NET](https://dotnet.testcontainers.org/) ·
[Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests) ·
[xUnit.net v3](https://xunit.net/docs/getting-started/v3/getting-started)
