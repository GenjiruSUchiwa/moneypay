---
title: Constructor Injection, Options Pattern, Typed Clients
impact: HIGH
impactDescription: Makes every module composable, every clock freezable, and every provider call fakeable
tags: patterns, dotnet, dependency-injection, options, timeprovider, httpclientfactory
---

## Constructor Injection, Options Pattern, Typed Clients

**Impact: HIGH**

Every dependency arrives through the constructor, is registered once in the owning
`<Module>Module.cs`, and is resolved by the container. Nothing news up a collaborator, reads
`IConfiguration` at call time, calls `DateTime.UtcNow`, or asks a service locator for what it needs.
This is what lets `MoniPay.Tests` compose one module against a fake Campay and a frozen clock.

**Incorrect (service locator, live config, ambient clock, hand-made `HttpClient`):**

```csharp
public sealed class TopUpService(IServiceProvider services, IConfiguration configuration)
{
    public async Task<TopUp> StartAsync(Guid userId, long amountXaf)
    {
        var wallet = services.GetRequiredService<WalletService>();          // hidden dependency
        var http = new HttpClient { BaseAddress = new Uri(configuration["Campay:BaseUrl"]!) }; // socket leak
        var minimum = int.Parse(configuration["Campay:MinimumXaf"]!);       // re-parsed per call, unvalidated

        var topUp = new TopUp { CreatedAt = DateTime.UtcNow };              // unfreezable in tests
        // ...
    }
}
```

Nothing here is testable without the network, and a typo in `Campay:MinimumXaf` fails on the first
customer request rather than at startup.

**Correct (primary constructor, `IOptions<T>`, `TimeProvider`, typed client):**

```csharp
// server/src/MoniPay.TopUps/Providers/CampayOptions.cs
public sealed class CampayOptions
{
    public const string SectionName = "MoniPay:Campay";

    [Required] public Uri BaseUrl { get; set; } = new("https://demo.campay.net/api/");

    [Required] public string AppUsername { get; set; } = string.Empty;

    [Required] public string AppPassword { get; set; } = string.Empty;

    /// <summary>The Campay demo environment refuses anything above 25 XAF.</summary>
    [Range(1, 1_000_000)] public long MaximumXaf { get; set; } = 25;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(3);
}
```

```csharp
// server/src/MoniPay.TopUps/TopUpsModule.cs
public static IServiceCollection AddMoniPayTopUps(this IServiceCollection services, IConfiguration configuration)
{
    services.AddOptions<CampayOptions>()
        .Bind(configuration.GetSection(CampayOptions.SectionName))
        .ValidateDataAnnotations()
        .Validate(options => options.PollInterval > TimeSpan.Zero, "Campay poll interval must be positive.")
        .ValidateOnStart();                 // a bad value stops the host, not a customer top-up

    services.AddHttpClient<ICollectFunds, CampayCollector>((provider, http) =>
    {
        var options = provider.GetRequiredService<IOptions<CampayOptions>>().Value;
        http.BaseAddress = options.BaseUrl;
        http.Timeout = TimeSpan.FromSeconds(20);
    });

    services.AddScoped<TopUpService>();

    return services;
}
```

```csharp
// server/src/MoniPay.TopUps/Domain/TopUpService.cs
public sealed class TopUpService(
    MoniPayDbContext database,
    ICollectFunds collector,
    WalletService wallet,
    IOptions<CampayOptions> options,
    TimeProvider timeProvider,
    ILogger<TopUpService> logger)
{
    private readonly CampayOptions options = options.Value;

    public async Task<TopUp> StartAsync(Guid userId, TopUpOrder order, CancellationToken cancellationToken)
    {
        if (order.AmountXaf > options.MaximumXaf)
        {
            throw new TopUpRefusedException(TopUpRefusal.AboveProviderCap, options.MaximumXaf);
        }

        var topUp = new TopUp
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            AmountXaf = order.AmountXaf,
            Status = TopUpStatus.Pending,
            CreatedAt = timeProvider.GetUtcNow(),
        };
        // ...
    }
}
```

**Lifetimes:**

| Lifetime | Use for |
|---|---|
| `Singleton` | `TimeProvider`, options, caches, `WorkSignal` |
| `Scoped` | `MoniPayDbContext` and every service that touches it — one per request or per worker cycle |
| `Transient` | cheap stateless helpers |

A singleton must never capture a scoped service. A `BackgroundService` is a singleton, so it creates
a scope per cycle with `IServiceScopeFactory` — see
[patterns-outbox-and-background-work](patterns-outbox-and-background-work.md).

**`IOptions<T>` variants:** `IOptions<T>` for values fixed at startup (the normal case),
`IOptionsMonitor<T>` only when a value must change while the process runs. Bind with
`AddOptions<T>().Bind(...).ValidateOnStart()`; never `configuration.GetValue<...>()` inside a method.

**`IHttpClientFactory`, always.** `AddHttpClient<TInterface, TImplementation>` gives the typed client
a pooled, correctly recycled handler, and it is where a resilience pipeline and the provider auth
header belong. `new HttpClient()` in a service exhausts sockets and pins stale DNS. A test replaces
the handler, not the collector: see [testing-dotnet-coverage](testing-dotnet-coverage.md).

**Clock and randomness are injected.** `TimeProvider` for time and delays, `Guid.CreateVersion7()`
for sortable ids. `DateTime.UtcNow`, `Task.Delay(t)` without a provider, and `Random.Shared` in
domain code are all untestable.

Reference: [Dependency injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) ·
[Options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) ·
[TimeProvider](https://learn.microsoft.com/en-us/dotnet/api/system.timeprovider)
