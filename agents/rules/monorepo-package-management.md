---
title: Every Dependency Version Is Declared Once, in One File Per Ecosystem
impact: HIGH
impactDescription: A version pinned in a project file produces a split dependency graph and a runtime type conflict
tags: monorepo, dependencies, nuget, cpm, swiftpm, tooling, versions
---

## Every Dependency Version Is Declared Once, in One File Per Ecosystem

**Impact: HIGH**

Four ecosystems feed this repository, and each one has exactly one place where a version is
decided:

| Ecosystem | Version lives in | Consumer names it without a version |
|---|---|---|
| NuGet | `server/Directory.Packages.props` | every `.csproj` |
| .NET tools | `dotnet-tools.json` | `dotnet husky …` |
| SwiftPM | each `ios/Packages/*/Package.swift`, pinned by `Package.resolved` | a target's `dependencies` |
| Developer tools | Homebrew (`xcodegen`, `swiftlint`) | `agents/commands.md` documents the version |

The failure this prevents is specific: two projects referencing the same package at two versions
produce one assembly loaded and one silently downgraded, which surfaces as a `MissingMethodException`
at runtime rather than an error at build time.

**Incorrect (a version in the project file):**

```xml
<!-- server/src/MoniPay.Wallet/MoniPay.Wallet.csproj -->
<ItemGroup>
  <!-- ❌ Central Package Management is on: this is a build error, and rightly so.
       Even where it built, MoniPay.Data at 10.0.10 and this at 10.0.3 is a split graph. -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.3" />
</ItemGroup>
```

**Correct (the project names the package; the root decides the version):**

```xml
<!-- server/src/MoniPay.Wallet/MoniPay.Wallet.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
  <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
</ItemGroup>
```

```xml
<!-- server/Directory.Packages.props — the one file that decides -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <!-- Pins transitive versions too: a dependency of a dependency cannot drift on its own. -->
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup Label="Runtime">
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.10" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
    <!-- 8.19.2 is the IdentityModel revision JwtBearer 10.0.10 depends on: one version for the graph. -->
    <PackageVersion Include="Microsoft.IdentityModel.JsonWebTokens" Version="8.19.2" />
  </ItemGroup>

  <ItemGroup Label="Test">
    <PackageVersion Include="xunit.v3" Version="3.2.2" />
  </ItemGroup>
</Project>
```

Group the entries (`Build and versioning`, `Runtime`, `Test`) and comment any version that is
pinned to satisfy another package, so the next upgrade knows what it is allowed to move.

**Cross-cutting properties are inherited, not copied.** `server/Directory.Build.props` sets
nullable, warnings-as-errors, analyzers and the MinVer prefix once; `src/` and `tests/` add only
the target framework:

```xml
<!-- server/src/Directory.Build.props -->
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

A property set in one `.csproj` and nowhere else is a smell: either it belongs to every project
(move it up) or it is a carve-out that needs a comment saying why.

**.NET tools are pinned in the manifest**, and `rollForward: false` keeps a new SDK from quietly
running a different tool:

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "husky": { "version": "0.9.1", "commands": [ "husky" ], "rollForward": false }
  }
}
```

`dotnet tool restore` runs automatically on the first build, from the husky bootstrap target.

**On the Swift side, the app has zero third-party packages.** Everything under `ios/Packages/` is
a **local** package referenced by path — that is the normal way this codebase is structured, not
a dependency:

```swift
// ios/Packages/WalletStore/Package.swift
dependencies: [
    .package(path: "../ApiClient"),
    .package(path: "../Money"),
    .package(path: "../Platform"),
],
```

Adding a **remote** SwiftPM package or a new NuGet package is an "ask first" decision, not a
judgement call — see the Boundaries section of `CLAUDE.md`. Ask what it buys that the standard
library and the existing packages do not, and whether the audit and upgrade cost is worth it.

**Never** work around a version conflict with `<NoWarn>`, a binding redirect, or a second
`PackageVersion` entry under a condition. Align the graph on one version, or drop the package
that forces the conflict.

Reference: [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
