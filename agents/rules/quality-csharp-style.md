---
title: C# Style — Nullable On, Warnings Fatal, Sealed by Default
impact: HIGH
impactDescription: The compiler catches the whole class of null and style bugs before review does
tags: quality, csharp, nullable, analyzers, dotnet-format, records, sealed
---

## C# Style — Nullable On, Warnings Fatal, Sealed by Default

**Impact: HIGH**

The build settings are not negotiable per project: `server/Directory.Build.props` turns them on once
for every project in the monorepo, and no `.csproj` opts out.

```xml
<PropertyGroup>
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <AnalysisLevel>latest</AnalysisLevel>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```

A warning is a build failure, so it gets fixed while it is cheap. `#pragma warning disable`,
`<NoWarn>`, and a `.editorconfig` carve-out are all ways of not fixing it — if the rule is genuinely
wrong, change the shared config in its own PR with a reason.

**Incorrect (fighting the compiler instead of the code):**

```csharp
public class WalletService                                   // not sealed, not injected
{
    public User User { get; set; } = null!;                  // a lie the compiler was told to believe
    public dynamic Provider { get; set; }                    // no type, no analyzer, no help

    public decimal Balance(string userId)
    {
        var user = _db.Users.Find(userId);
        return _db.Entries.Where(e => e.UserId == user!.Id).Sum(e => e.Amount).Result; // sync-over-async
    }
}
```

`null!` moves the `NullReferenceException` from compile time to a customer's balance screen, and
`.Result` deadlocks or exhausts the thread pool under load.

**Correct (the types say what is true):**

```csharp
// server/src/MoniPay.Wallet/Domain/WalletService.cs
namespace MoniPay.Wallet;                                     // file-scoped namespace

public sealed class WalletService(MoniPayDbContext database, TimeProvider timeProvider)
{
    public async Task<long> BalanceXafAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await database.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new UserNotFoundException(userId);
        }

        return await database.WalletEntries
            .Where(entry => entry.UserId == user.Id)
            .SumAsync(entry => entry.AmountXaf, cancellationToken)
            .ConfigureAwait(false);
    }
}
```

**The conventions:**

- **File-scoped namespaces**, one public type per file, named after it.
- **`sealed` by default.** Unseal deliberately, when a type is designed for inheritance. It is
  faster, and it stops accidental extension of a service.
- **`record` for data, `class` for behaviour.** API contracts, provider DTOs, and events are
  `sealed record`s — value equality, immutable by default. `readonly record struct` for small value
  types like `Money` or `PhoneNumber`.
- **Primary constructors** for injected dependencies; assign to a `private readonly` field only when
  you need to unwrap (`options.Value`).
- **Async all the way.** Every I/O method is `async` and takes a `CancellationToken` as its last
  parameter, named `cancellationToken`. `.Result`, `.Wait()`, and `.GetAwaiter().GetResult()` are
  banned. In library code (`MoniPay.<Module>`) chain `.ConfigureAwait(false)`.
- **No `dynamic`, no reflection in a hot path, no `object` where a type exists.**
- **Guard public entry points** with `ArgumentNullException.ThrowIfNull(...)` and the
  `ArgumentOutOfRangeException.ThrowIf*` helpers.
- **Names in English**, descriptive, no Hungarian and no single letters — including LINQ lambdas:
  `entry => entry.AmountXaf`, not `e => e.A`. French belongs in the iOS UI copy, never in C#.
- **Logging is source-generated**: `[LoggerMessage]` on a `partial` method, not interpolated strings.
  **Never log a PAN, a CVV, a full phone number, an OTP, or a provider key** — mask to the last four.

**Exceptions vs results.** Throw a domain exception when the caller cannot reasonably continue
(`InsufficientFundsException`, `ProviderDeclinedException`); the API's `IExceptionHandler` maps it to
a `ProblemDetails` with a stable `type`, so endpoints stay free of `try`/`catch`. Return a result
type when the outcome is an ordinary branch the caller will handle either way — an
`AuthorizationDecision` of `Approved` / `Declined` is data, not a failure. Never use an exception for
flow control in a loop.

**Formatting is a command, not an opinion:**

```bash
dotnet format server/MoniPay.slnx                       # fix
dotnet format server/MoniPay.slnx --verify-no-changes   # what CI runs
```

Reference: [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) ·
[Nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references)
