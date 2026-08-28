---
title: The OpenAPI Document Is the Contract Between server/ and ios/
impact: CRITICAL
impactDescription: A hand-written client type drifts silently and ships a decoding failure to production
tags: monorepo, openapi, api, contract, codegen, apiclient, ci
---

## The OpenAPI Document Is the Contract Between server/ and ios/

**Impact: CRITICAL**

`docs/api/openapi.json` is the single agreed description of the HTTP API. It is **generated** from
`MoniPay.Api` — never written, never patched — and copied into the iOS `ApiClient` package, which
generates its Swift types from it.

That makes contract drift a build failure instead of a runtime one. A field renamed in a C#
`record` changes the document, which changes the generated Swift type, which fails to compile
against the call site that still uses the old name. Hand-writing either side throws that away:
the mismatch survives until a user hits the endpoint and gets a decoding error.

**Incorrect (a Swift type typed by hand from memory of the C# one):**

```swift
// ios/Packages/ApiClient/Sources/ApiClient/TopUpDTO.swift
struct TopUpDTO: Decodable {          // ❌ nothing checks this against the server
    let amountFcfa: Int               // ❌ server renamed it to amountXaf three weeks ago
    let status: String                // ❌ server sends an enum with a case this cannot express
}
```

```bash
# ❌ Editing the artefact to match the client, instead of the API that produces it.
$EDITOR docs/api/openapi.json
```

**Correct (change the API, regenerate, commit both):**

```csharp
// server/src/MoniPay.Api/Contracts/TopUpResponse.cs — the contract is a record in Contracts/
namespace MoniPay.Api.Contracts;

/// <summary>What POST /topups answers. XAF has no minor unit, so AmountXaf is whole francs.</summary>
public sealed record TopUpResponse(Guid Id, long AmountXaf, TopUpStatus Status);
```

```bash
./scripts/update-openapi.sh     # builds the API with -p:GenerateOpenApiDocs=true and copies it
git add docs/api/openapi.json ios/Packages/ApiClient/Sources/ApiClient/openapi.json
```

The generated document and the code change land in the **same commit**. A regenerated contract
committed on its own is a change nobody can review.

**The generation is opt-in**, because producing the document boots the host and would otherwise
run on every incremental build:

```xml
<!-- server/src/MoniPay.Api/MoniPay.Api.csproj -->
<PropertyGroup>
  <OpenApiGenerateDocuments Condition="'$(GenerateOpenApiDocs)' != 'true'">false</OpenApiGenerateDocuments>
</PropertyGroup>

<PropertyGroup Condition="'$(GenerateOpenApiDocs)' == 'true'">
  <OpenApiGenerateDocuments>true</OpenApiGenerateDocuments>
  <OpenApiDocumentsDirectory>$(MSBuildThisFileDirectory)obj/openapi</OpenApiDocumentsDirectory>
  <OpenApiGenerateDocumentsOptions>--file-name openapi</OpenApiGenerateDocumentsOptions>
</PropertyGroup>
```

**The document must be stable across unrelated builds.** ASP.NET Core emits paths in discovery
order, so adding one endpoint reshuffles the whole file and every contract diff becomes
unreadable. A document transformer sorts it:

```csharp
// server/src/MoniPay.Api/OpenApi/SortedOpenApiDocumentTransformer.cs
internal sealed class SortedOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        var sorted = document.Paths.OrderBy(path => path.Key, StringComparer.Ordinal).ToList();
        document.Paths.Clear();

        foreach (var (key, value) in sorted)
        {
            document.Paths.Add(key, value);
        }

        return Task.CompletedTask;
    }
}
```

**CI fails on drift**, so a forgotten regeneration cannot merge:

```yaml
- name: Verify OpenAPI contract
  run: |
    ./scripts/update-openapi.sh
    git diff --exit-code -- docs/api/openapi.json
```

`./scripts/check-openapi-sync.sh` is the cheap local version: it diffs the committed contract
against the iOS copy without rebuilding.

**Until `server/` ships, `poc/README.md` is the contract.** The Node POC (`POST /signup`,
`POST /topup`, `POST /card`, `POST /simtopup`, `GET /user`) defines what `ApiClient` talks to
today, and its DTOs mirror that README by hand. That is a temporary exception with an end date:
the moment an endpoint exists in `MoniPay.Api`, its Swift type comes from the generated document
and the hand-written DTO is deleted, not kept "just in case".

**Provider quirks never reach the contract.** A Campay sandbox cap, a Sudo response with
`{statusCode: 400}` inside an HTTP 200, a `data`-wrapped body — these are mapped to a domain
error inside the module that owns the provider. The OpenAPI document describes MoniPay's API, not
a third party's.

Reference: [OpenAPI document generation at build time in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi#generate-openapi-documents-at-build-time)
