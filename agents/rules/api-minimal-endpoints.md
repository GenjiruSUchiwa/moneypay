---
title: Endpoints Live in Their Module and Stay Thin
impact: HIGH
impactDescription: Keeps HTTP out of the domain, and keeps every module out of the host
tags: api, dotnet, minimal-apis, typedresults, problemdetails, contracts, vertical-slice
---

## Endpoints Live in Their Module and Stay Thin

**Impact: HIGH**

Two rules, and they pull in the same direction.

**Where.** A route belongs to the module that owns the resource, not to `MoniPay.Api`. Wallet routes
live in `MoniPay.Wallet/Endpoints/`, their request and response records in
`MoniPay.Wallet/Contracts/`, and `WalletModule.MapMoniPayWallet` is the single method the host calls.
The host maps modules, never routes.

**How thin.** An endpoint binds the request, calls one service, shapes the response, returns a
status. Anything else — a balance check, an FX computation, a provider retry — belongs in the
module's `Domain/`. HTTP is a delivery mechanism: the same `WalletService` must be callable from a
background worker with no `HttpContext` in sight.

```
server/src/MoniPay.Wallet/
  Domain/       WalletEntry.cs  WalletEntryKind.cs  WalletService.cs
  Persistence/  WalletEntryConfiguration.cs  WalletSets.cs
  Contracts/    WalletResponse.cs  LedgerLineResponse.cs
  Endpoints/    WalletEndpoints.cs  WalletRoutes.cs  WalletEndpointNames.cs  WalletTags.cs
  WalletModule.cs
```

**Incorrect (business logic in the handler, raw results, routes in the host):**

```csharp
// MoniPay.Api/Endpoints/WalletEndpoints.cs
app.MapGet("/wallet", async (Guid userId, MoniPayDbContext db, HttpClient http) =>   // literal route
{
    var balance = await db.WalletEntries.Where(e => e.UserId == userId).SumAsync(e => e.AmountXaf);
    var rate = await http.GetFromJsonAsync<RateDto>("https://fx.example/latest");   // I/O in the handler
    if (balance < 0) return Results.BadRequest("negative balance");                 // bare string, no ProblemDetails
    return Results.Ok(new { balance, usd = balance / rate!.Value });                // untyped, float money
});
```

`Results.Ok` erases the response type, so OpenAPI documents `object` and the generated Swift client
gets nothing. The rule is also invisible to any caller that is not HTTP, and the host now knows the
wallet's table.

**Correct (a group in the module, one service call, `TypedResults`):**

```csharp
// server/src/MoniPay.Wallet/Endpoints/WalletEndpoints.cs
using Microsoft.AspNetCore.Http.HttpResults;
using MoniPay.Wallet.Contracts;

namespace MoniPay.Wallet.Endpoints;

internal static class WalletEndpoints
{
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder routes)
    {
        // Routes, names and tags are constants the module owns, never literals: see
        // api-no-magic-strings. A rename is then a compile error, in the tests too.
        var wallet = routes
            .MapGroup(WalletRoutes.Group)
            .WithTags(WalletTags.Wallet)
            .RequireAuthorization();

        wallet.MapGet(WalletRoutes.Balance, async Task<Ok<WalletResponse>> (
            ClaimsPrincipal principal,
            WalletService service,
            CancellationToken cancellationToken) =>
        {
            var balanceXaf = await service
                .BalanceXafAsync(principal.GetUserId(), cancellationToken)
                .ConfigureAwait(false);

            return TypedResults.Ok(new WalletResponse(balanceXaf));
        })
        .WithName(WalletEndpointNames.GetWallet)
        .WithSummary(WalletSummaries.GetWallet);

        wallet.MapGet(WalletRoutes.Entries, async Task<Ok<IReadOnlyList<LedgerLineResponse>>> (
            ClaimsPrincipal principal,
            WalletService service,
            int limit,
            CancellationToken cancellationToken) =>
        {
            var lines = await service
                .RecentEntriesAsync(principal.GetUserId(), Math.Clamp(limit, 1, 100), cancellationToken)
                .ConfigureAwait(false);

            return TypedResults.Ok(lines.Select(LedgerLineResponse.From).ToArray() as IReadOnlyList<LedgerLineResponse>);
        })
        .WithName(WalletEndpointNames.ListWalletEntries)
        .WithSummary(WalletSummaries.ListWalletEntries);

        return routes;
    }
}
```

```csharp
// server/src/MoniPay.Wallet/Contracts/WalletResponse.cs — internal: only this module projects it
namespace MoniPay.Wallet.Contracts;

/// <summary>Whole francs. FCFA has no minor unit, so this is an integer on the wire.</summary>
internal sealed record WalletResponse(long BalanceXaf);

internal sealed record LedgerLineResponse(Guid Id, long AmountXaf, string Kind, DateTimeOffset CreatedAt)
{
    public static LedgerLineResponse From(WalletEntry entry) =>
        new(entry.Id, entry.AmountXaf, entry.Kind.ToString(), entry.CreatedAt);
}
```

```csharp
// server/src/MoniPay.Wallet/WalletModule.cs
public static IEndpointRouteBuilder MapMoniPayWallet(this IEndpointRouteBuilder routes) =>
    routes.MapWalletEndpoints();
```

**Rules that follow from this shape:**

- **`TypedResults`, always**, with the union spelled in the return type
  (`Results<Ok<T>, NotFound>`, `Results<Accepted<T>, ValidationProblem>`). It is what makes the
  OpenAPI document — and therefore the Swift client — describe reality.
- **Never return an EF entity.** A `Contracts/` record is the boundary; see
  [data-dto-boundaries](data-dto-boundaries.md).
- **Amounts on the wire are minor units** — `amountXaf` as an integer number of francs,
  `amountUsdCents` as an integer number of cents. Never a JSON float, and never a formatted string:
  no separator, no `F` suffix, and dates as ISO-8601 UTC `DateTimeOffset`. The client formats for
  its locale; see [api-localization](api-localization.md).
- **Errors are `ProblemDetails`.** The host registers `AddProblemDetails()` and one
  `IExceptionHandler` mapping domain exceptions (`InsufficientFundsException`,
  `ProviderDeclinedException`) to a status and a stable `type` like
  `urn:monipay:error:insufficient-funds`. Endpoints contain no `try`/`catch` and no French prose —
  the iOS app localises from the `type`.
- **The group carries the cross-cutting policy**: `.RequireAuthorization()`, rate limiting, tags.
  Repeating them per route is how one endpoint ends up unauthenticated.
- **`.WithName(...)` on every route, from a constant** — it becomes the Swift operation name, so
  renaming one is a breaking client change; see [api-openapi-contract](api-openapi-contract.md) and
  [api-no-magic-strings](api-no-magic-strings.md).
- **Version by path** (`/v1/...`) only when a change cannot be made additively. The current surface
  is v1 and mirrors the POC contract in `poc/README.md`.

Reference: [Minimal APIs quick reference](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis) ·
[Route groups](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/route-handlers)
