---
title: No Magic Strings in the HTTP Surface
impact: HIGH
impactDescription: A renamed route or operation is a compile error instead of a client that 404s in production
tags: api, dotnet, constants, nameof, routes, options, testing
---

## No Magic Strings in the HTTP Surface

**Impact: HIGH**

Every string the HTTP surface is built from — a route template, an operation name, a tag, a summary,
an authorization policy, a header, a configuration section key — is declared **once**, as a `const`
in a class the module owns, and referenced everywhere else. A literal typed twice is a literal that
will one day differ, and neither the compiler nor a test will notice: the endpoint moves, the app
keeps calling the old path, and the failure surfaces as a 404 for a user trying to top up.

The operation name matters even more than the route here, because
[the OpenAPI document is the contract](api-openapi-contract.md) and `.WithName(...)` becomes the
generated Swift method.

**Incorrect (four literals, none of them checked by anything):**

```csharp
app.MapGet("/wallet", GetWallet)
   .WithName("GetWallet")
   .WithSummary("Returns the balance")
   .WithTags("Wallet")
   .RequireAuthorization("KycVerified");

// …and elsewhere, silently free to drift:
var response = await client.GetAsync("/wallet/");          // trailing slash, different route
Assert.Equal("Wallet", document.Paths["/wallet"].Tags[0]); // the test restates the literal
```

Rename the route to `/wallets` and everything still compiles. Misspell `"KycVerified"` and the
policy silently fails closed — or worse, throws only when that endpoint is first called.

**Correct (constants the module owns, referenced by endpoint and test alike):**

```csharp
// server/src/MoniPay.Wallet/Endpoints/WalletRoutes.cs
namespace MoniPay.Wallet.Endpoints;

internal static class WalletRoutes
{
    public const string Group = "/wallet";
    public const string Balance = "/";
    public const string Entries = "/entries";
    public const string EntryById = "/entries/{entryId:guid}";
}

/// <summary>Operation ids. They become the Swift client's method names, so they never drift.</summary>
internal static class WalletEndpointNames
{
    public const string GetWallet = nameof(GetWallet);
    public const string ListWalletEntries = nameof(ListWalletEntries);
    public const string GetWalletEntry = nameof(GetWalletEntry);
}

internal static class WalletTags
{
    public const string Wallet = "Wallet";
}

internal static class WalletSummaries
{
    public const string GetWallet = "Returns the FCFA balance of the signed-in user.";
    public const string ListWalletEntries = "Returns the most recent ledger entries, newest first.";
}
```

```csharp
// server/src/MoniPay.Wallet/Endpoints/WalletEndpoints.cs
var wallet = routes
    .MapGroup(WalletRoutes.Group)
    .WithTags(WalletTags.Wallet)
    .RequireAuthorization(MoniPayPolicies.KycVerified);

wallet.MapGet(WalletRoutes.Balance, /* handler */)
    .WithName(WalletEndpointNames.GetWallet)
    .WithSummary(WalletSummaries.GetWallet);
```

**`nameof`, not a quoted copy.** `nameof(GetWallet)` on the constant itself ties the value to the
identifier, so renaming the constant renames the operation and every use follows. Use `nameof`
wherever a string names a code element: operation ids, claim types backed by a property, log scopes.

**Options section keys are a `const` on the options class** — the class and its configuration path
belong together, so nothing can bind a section that no longer exists:

```csharp
public sealed class WalletOptions
{
    /// <summary>The one place this section's path is written. Always named SectionName.</summary>
    public const string SectionName = "MoniPay:Wallet";

    public long MaximumHoldXaf { get; set; } = 5_000_000;
}

services.AddOptions<WalletOptions>()
    .Bind(configuration.GetSection(WalletOptions.SectionName))
    .ValidateOnStart();
```

**Cross-cutting names live in `MoniPay.Kernel`**, because the host and several modules use them:

```csharp
// server/src/MoniPay.Kernel/MoniPayHeaders.cs
public static class MoniPayHeaders
{
    public const string IdempotencyKey = "Idempotency-Key";
    public const string ClientVersion = "X-MoniPay-Client";
}

// server/src/MoniPay.Kernel/MoniPayPolicies.cs
public static class MoniPayPolicies
{
    public const string KycVerified = nameof(KycVerified);
}

// server/src/MoniPay.Kernel/MoniPayErrorTypes.cs
public static class MoniPayErrorTypes
{
    public const string Prefix = "urn:monipay:error:";
    public const string InsufficientFunds = Prefix + "insufficient-funds";
}
```

**Tests reference the same constants.** This is the point of the whole rule: a test that hardcodes
`"/wallet"` passes on the old path and proves nothing about the rename. `MoniPay.Tests` sees module
internals through the repo-wide `InternalsVisibleTo`, so it names the constant:

```csharp
var response = await Api.CreateClient().GetAsync(WalletRoutes.Group + WalletRoutes.Balance);

Assert.Equal(HttpStatusCode.OK, response.StatusCode);
```

Rename `WalletRoutes.Group` and the suite fails to compile — which is exactly when you want to find
out, rather than after the app ships against a path that moved.

**Where the line is.** Constants are for strings that are part of a contract or a lookup: routes,
names, tags, policies, headers, claim types, section keys, error codes, cache keys, HTTP client
names. They are *not* for one-off prose. A summary may live in a constant, or come from a resource
when it must be localized — see [api-localization](api-localization.md); user-facing prose belongs
in `.resx` either way, never inline in a handler.

Reference: [nameof expression (C# reference)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/nameof) ·
[Route groups](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/route-handlers)
