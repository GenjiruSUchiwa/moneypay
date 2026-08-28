---
title: A Module Owns Its Whole Vertical Slice
impact: CRITICAL
impactDescription: The compiler enforces the module graph, and no module leaks into the host to get an HTTP route
tags: architecture, dotnet, modular-monolith, modules, vertical-slice, composition-root
---

## A Module Owns Its Whole Vertical Slice

**Impact: CRITICAL**

`server/` is a modular monolith: one deployable, many projects. Each bounded context is its own
class library `MoniPay.<Module>` holding **everything that context needs — HTTP surface included**:
its endpoints, its wire contracts, its domain, its EF mappings, and one `<Module>Module.cs` that
registers services *and* maps routes. Because a module is a *project*, the dependency graph is
checked by MSBuild — a `TopUps` file that reaches into `Cards` internals does not compile.

Adding a feature touches **one directory**. `MoniPay.Api` is a host, not a parking lot for every
module's controllers.

**Incorrect (endpoints and contracts hoisted into the host):**

```
server/src/MoniPay.Api/
  Endpoints/WalletEndpoints.cs      every module leaks into the host
  Endpoints/TopUpEndpoints.cs
  Contracts/ApiContracts.cs         one 900-line file the whole team edits
server/src/MoniPay.Wallet/
  WalletService.cs                  the module is half a module
```

`WalletEndpoints` now needs `WalletEntry` public so the host can project it; `ApiContracts.cs` is a
permanent merge conflict; and deleting the wallet feature means editing four files in three projects.

**Correct (the module is the slice):**

```
server/src/
  MoniPay.Kernel/                   primitives only — no business logic, depends on nothing
    Money.cs  Currency.cs  PhoneNumber.cs  WorkSignal.cs  MoniPayExceptions.cs  SecretCipher.cs
    MoniPayHeaders.cs  MoniPayPolicies.cs  MoniPayErrorTypes.cs   cross-cutting name constants
  MoniPay.Data/                     the context and the migrations — knows no entity
    MoniPayDbContext.cs  ModuleAssemblies.cs  PersistenceModule.cs  Migrations/
  MoniPay.Wallet/
    Domain/                         WalletEntry.cs  WalletEntryKind.cs  WalletService.cs
    Persistence/                    WalletEntryConfiguration.cs  WalletSets.cs
    Contracts/                      WalletResponse.cs  LedgerLineResponse.cs
    Endpoints/                      WalletEndpoints.cs  WalletRoutes.cs
                                    WalletEndpointNames.cs  WalletTags.cs  WalletSummaries.cs
    WalletModule.cs                 AddMoniPayWallet + MapMoniPayWallet
    MAP.md
  MoniPay.TopUps/                   same five, plus Providers/ for the Campay client
  MoniPay.Cards/  MoniPay.Fx/  MoniPay.Users/  MoniPay.Sessions/
  MoniPay.Transactions/  MoniPay.Kyc/  MoniPay.Notifications/  MoniPay.Outbox/
  MoniPay.Api/                      HOST ONLY
    Program.cs  MoniPayModules.cs  MoniPayExceptionHandler.cs  RateLimiting.cs
    SecurityHeaders.cs  OpenApi/  wwwroot/  appsettings*.json
server/tests/MoniPay.Tests/
```

**Roles you must not blur:**

- **`MoniPay.Kernel`** — primitives with no domain rule: `Money`, `Currency`, `PhoneNumber`, clock
  helpers, the base exception types, `WorkSignal`, the secret cipher, and the cross-cutting name
  constants several modules share ([api-no-magic-strings](api-no-magic-strings.md)). If it decides
  anything about a top-up, a card, or a balance, it does not belong here.
- **`MoniPay.Data`** — `MoniPayDbContext` plus `Migrations/`. It maps **no** entity: modules bring
  their own configurations and the context composes the assemblies it was handed.
- **`MoniPay.<Module>`** — `Domain/`, `Persistence/`, `Contracts/`, `Endpoints/`, `<Module>Module.cs`.
- **`MoniPay.Api`** — the process: `Program.cs`, the composition root, middleware, the exception
  handler, the OpenAPI document, static pages. It contains no route handler and no domain rule.

**The module file registers services and maps routes:**

```csharp
// server/src/MoniPay.Wallet/WalletModule.cs
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Wallet.Endpoints;

namespace MoniPay.Wallet;

/// <summary>The composition of the wallet module: its services, and its HTTP surface.</summary>
public static class WalletModule
{
    public static IServiceCollection AddMoniPayWallet(this IServiceCollection services) =>
        services.AddScoped<WalletService>();

    /// <summary>The host calls this; it never names an endpoint class of its own.</summary>
    public static IEndpointRouteBuilder MapMoniPayWallet(this IEndpointRouteBuilder routes) =>
        routes.MapWalletEndpoints();
}
```

A module that maps routes needs the ASP.NET Core surface, which a class library gets with a
framework reference — not a package:

```xml
<!-- server/src/MoniPay.Wallet/MoniPay.Wallet.csproj -->
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

**The composition root is two lists, one line per module:**

```csharp
// server/src/MoniPay.Api/MoniPayModules.cs
internal static class MoniPayModules
{
    /// <summary>
    /// The assemblies whose entity configurations compose the model: every module, whether or not
    /// it maps an entity today. A module missing here is a silently missing table the day it
    /// gains its first configuration.
    /// </summary>
    internal static readonly Assembly[] ModelAssemblies =
    [
        typeof(MoniPayDbContext).Assembly,
        typeof(UsersModule).Assembly,
        typeof(SessionsModule).Assembly,
        typeof(FxModule).Assembly,
        typeof(WalletModule).Assembly,
        typeof(TopUpsModule).Assembly,
        typeof(CardsModule).Assembly,
        typeof(TransactionsModule).Assembly,
        typeof(KycModule).Assembly,
        typeof(NotificationsModule).Assembly,
        typeof(OutboxModule).Assembly,
    ];

    public static IServiceCollection AddMoniPay(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        return services
            .AddMoniPayPersistence(configuration, ModelAssemblies)
            .AddMoniPayKernel(configuration)
            .AddMoniPayUsers()
            .AddMoniPaySessions(configuration)
            .AddMoniPayFx(configuration)
            .AddMoniPayWallet()
            .AddMoniPayTopUps(configuration)
            .AddMoniPayCards(configuration)
            .AddMoniPayTransactions()
            .AddMoniPayKyc()
            .AddMoniPayOutbox(configuration)
            .AddMoniPayNotifications(configuration);
    }

    /// <summary>The host knows module names, never route names.</summary>
    public static IEndpointRouteBuilder MapMoniPay(this IEndpointRouteBuilder routes)
    {
        routes.MapMoniPayUsers();
        routes.MapMoniPaySessions();
        routes.MapMoniPayWallet();
        routes.MapMoniPayTopUps();
        routes.MapMoniPayCards();
        routes.MapMoniPayTransactions();
        routes.MapMoniPayKyc();

        return routes;
    }
}
```

```csharp
// server/src/MoniPay.Api/Program.cs — the whole HTTP surface, in one line
app.MapMoniPay();
```

**Direction of dependencies** (a module may reference only what is above it):
`Kernel` -> `Data` -> `Users` -> `Fx` -> `Wallet` -> `TopUps` / `Cards` / `Kyc` -> `Transactions` /
`Notifications` -> `Api`. `Outbox` sits beside `Data`. Two modules that need the same type push it
**down** into `Kernel`, never sideways.

**No module opens another module's tables.** `TopUps` credits the wallet by calling
`WalletService.AppendAsync(...)`, never by writing a `wallet_entries` row — see
[data-money-ledger](data-money-ledger.md).

**Adding a module means:** a `server/src/MoniPay.<Module>/` project with those four folders, a
`<Module>Module.cs`, an entry in `MoniPay.slnx`, a `ProjectReference` from `MoniPay.Api`, one line in
`AddMoniPay` and one in `MapMoniPay`, an entry in `ModelAssemblies`, a `COPY` line in
`server/Dockerfile`, and a `MAP.md` — purpose in one sentence, public API, and who calls it.

Reference: [Common web application architectures — .NET](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
