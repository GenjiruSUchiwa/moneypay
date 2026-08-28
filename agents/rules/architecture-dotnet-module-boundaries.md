---
title: Public Contract, Internal Everything Else
impact: CRITICAL
impactDescription: Keeps modules replaceable and stops a shared "Common" project from becoming the new monolith
tags: architecture, dotnet, boundaries, internal, encapsulation, internalsvisibleto
---

## Public Contract, Internal Everything Else

**Impact: CRITICAL**

A `MoniPay.<Module>` project is a black box with a deliberately small lid. `public` is the module's
contract with the rest of the server; everything else is `internal`. C# defaults a top-level type to
`internal`, so the discipline is simply: *do not type `public` unless another project needs it.*

Because a module owns its own `Endpoints/` and `Contracts/`
([architecture-dotnet-modular-monolith](architecture-dotnet-modular-monolith.md)), that lid is
smaller than it would be otherwise: the host needs **two methods**, not a set of DTOs.

**What may be `public` in a module:**

- `<Module>Module` — `AddMoniPay<Module>` and `MapMoniPay<Module>`, the only things the host calls
- the service another *module* calls (`WalletService`, `FxRateProvider`)
- the ports it offers or requires (`ICollectFunds`, `IIssueCards`)
- the domain value types crossing a module boundary (`WalletEntryKind`, `TopUpStatus`)

**What stays `internal`:** endpoint classes, request and response records, entity configurations,
`DbSet` accessors, provider HTTP clients and their DTOs, mappers, validators, background workers,
and every helper.

**Request and response records are internal.** Nothing outside the module projects them — its own
endpoints do, and `System.Text.Json` serialises an internal type by its public properties without
complaint. A `public` contract record is an invitation for another module to build a response it
does not own.

**Incorrect (provider details and wire shapes as the contract):**

```csharp
namespace MoniPay.TopUps;

public sealed record CampayCollectRequest(string Amount, string Currency, string From);  // transport as contract
public sealed class CampayCollector { }
public sealed class TopUpItemConfiguration : IEntityTypeConfiguration<TopUp> { }
public static class TopUpEndpoints { public static void MapTopUpEndpoints(...) { } }     // host must know routes
```

```csharp
// MoniPay.Cards — compiles, and now Cards depends on Campay's JSON shape
var request = new CampayCollectRequest("25", "XAF", phone);
```

The day Campay is replaced, the change is no longer local to `TopUps`; and the host names route
classes, so every module leaks upward again.

**Correct (one port, one domain result, the rest sealed shut):**

```csharp
// server/src/MoniPay.TopUps/Domain/ICollectFunds.cs — the whole public surface of the provider
namespace MoniPay.TopUps;

/// <summary>Collects FCFA from a MoMo wallet. Implemented by Campay today, by anyone tomorrow.</summary>
public interface ICollectFunds
{
    Task<CollectionTicket> StartAsync(CollectionOrder order, CancellationToken cancellationToken);

    Task<CollectionOutcome> PollAsync(string providerReference, CancellationToken cancellationToken);
}

public readonly record struct CollectionOrder(Guid UserId, long AmountXaf, PhoneNumber From, string IdempotencyKey);

public enum CollectionOutcome { Pending, Succeeded, Failed }
```

```csharp
// server/src/MoniPay.TopUps/Providers/CampayCollector.cs — internal: nobody outside TopUps names it
internal sealed class CampayCollector(HttpClient http, IOptions<CampayOptions> options) : ICollectFunds
{
    private sealed record CollectRequest(string amount, string currency, string from, string external_reference);

    // Campay caps a sandbox collection at 25 XAF and answers 200 with a failure body; both quirks
    // are mapped to CollectionOutcome here and never travel further.
}
```

```csharp
// server/src/MoniPay.TopUps/Endpoints/TopUpEndpoints.cs — internal, reached only through the module
internal static class TopUpEndpoints
{
    public static IEndpointRouteBuilder MapTopUpEndpoints(this IEndpointRouteBuilder routes) { /* ... */ }
}
```

**`InternalsVisibleTo` is for tests, and for nothing else.** It is declared once, repo-wide, in
`server/Directory.Build.props`:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="MoniPay.Tests" />
</ItemGroup>
```

Adding `<InternalsVisibleTo Include="MoniPay.Cards" />` to `MoniPay.TopUps` is not a shortcut, it is
a deleted boundary. If `Cards` genuinely needs something from `TopUps`, either make it public and
own it as a contract, or move it down into `Kernel`.

**No `MoniPay.Common` dumping ground.** A project named `Common`, `Shared`, `Core`, or `Utils`
attracts every type nobody wanted to place, and within a quarter every module references it and the
graph is a star with a mud ball at the centre. `Kernel` is allowed to exist because it has a stated
job — primitives with no business rule (`Money`, `Currency`, `PhoneNumber`, the clock, the base
exceptions) — and a reviewer may reject an addition on that ground: *does this decide anything about
a top-up, a card, or a balance?* Then it belongs to the module that owns the decision.

**When two modules need the same type, it moves down.** `Money` and `Currency` are needed by
`Wallet`, `TopUps`, `Cards`, `Fx`, and `Transactions`, so they live in `Kernel` — not duplicated,
not passed sideways through an interface declared in the host.

**Enforcement:** the compiler already does most of it. `TreatWarningsAsErrors` plus the analyzers
catch an unused `public`. In review, a new `public` type without a caller outside its own project is
a change request.

Reference: [Access modifiers (C# reference)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/access-modifiers)
