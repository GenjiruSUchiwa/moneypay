---
title: The OpenAPI Document Is the Contract With iOS
impact: CRITICAL
impactDescription: A silent schema change compiles on the server and breaks the app in the store
tags: api, openapi, contract, swift-openapi-generator, versioning, ci
---

## The OpenAPI Document Is the Contract With iOS

**Impact: CRITICAL**

`docs/api/openapi.json` is generated from `MoniPay.Api` and consumed by
**swift-openapi-generator** inside `ios/Packages/ApiClient`. It is not documentation that drifts —
it is the single artefact both heads compile against. The server is the source of truth; the Swift
client is generated, never hand-written.

**The loop, every time an endpoint or a contract record changes:**

```bash
./scripts/update-openapi.sh          # builds with -p:GenerateOpenApiDocs=true, copies the document
```

The script writes `docs/api/openapi.json` and copies it to the iOS client package, so one command
keeps the two in step. Commit the regenerated document **in the same PR** as the endpoint change.
CI runs the script and then `git diff --exit-code -- docs/api/openapi.json`: a stale document fails
the build.

```bash
./scripts/check-openapi-sync.sh      # the copies match the committed contract
```

**Incorrect (renaming a field because the C# reads better):**

```csharp
// before
internal sealed record CardResponse(Guid Id, long BalanceUsdCents, string Last4);

// after — the server compiles, iOS 4.2 in the store starts decoding null
internal sealed record CardResponse(Guid Id, long Balance, string Last4);
```

Also breaking, and just as quiet: removing an enum case, making an optional property required,
narrowing a type (`long` to `int`), renaming an operation via `.WithName(...)`, or changing a route.
Every one of those regenerates a different Swift type.

**Correct (additive change, old field kept until the clients are gone):**

```csharp
internal sealed record CardResponse(
    Guid Id,
    long BalanceUsdCents,          // kept: shipped clients still read it
    long? SpendLimitUsdCents,      // new, and nullable, so old clients ignore it
    string Last4);
```

**Breaking-change policy:**

1. **Additive by default.** New properties are nullable or have a default. New enum cases are
   introduced only once every shipped client tolerates an unknown case — the Swift client decodes an
   unknown case as `.undocumented`, so treat an added case as breaking for any client older than the
   one that handles it.
2. **Never rename or remove** a property, an operation name, or a route inside a version.
3. **When behaviour must change incompatibly, add `/v2/<resource>`** and run both. `/v1` stays until
   the analytics show no client older than the cutover; six months is the floor for a released app,
   because a user may not update.
4. **A breaking change is a `feat!:` or `BREAKING CHANGE:` commit**, so the `monipay.api-v*` release
   picks up the major bump.

**Keep the document generator-friendly.** The Swift generator is stricter than the server:

- Every operation has an explicit `.WithName(...)`; that string becomes the Swift method name.
- Every response is a `TypedResults` union, so no operation documents a bare `object`.
- Money is an integer of minor units in the schema (`format: int64`), never `number`.
- Dates are `date-time` (`DateTimeOffset`), never a free string.
- Document transformers in `MoniPay.Api/OpenApi/` sort the document, so a regeneration produces a
  stable diff instead of a reshuffled 3000-line file.

**Reviewing an API PR means reading the `openapi.json` diff.** If it contains a removal or a rename,
the PR needs a version story before it merges.

Reference: [swift-openapi-generator](https://swiftpackageindex.com/apple/swift-openapi-generator/documentation) ·
[OpenAPI in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi)
