---
title: Anatomy of a Local SwiftPM Package
impact: CRITICAL
impactDescription: A wrong manifest silently disables strict concurrency, localization, or test doubles for a whole feature
tags: architecture, swiftpm, package, manifest, testsupport, resources, naming
---

## Anatomy of a Local SwiftPM Package

**Impact: CRITICAL**

Every feature and every shared capability in MoniPay is a local SwiftPM package under `ios/Packages/<Name>/`.
The package name, the folder name, the product name, and the target name are all the same PascalCase word.
`ios/project.yml` (XcodeGen) is the source of truth for the app target; the generated `.xcodeproj` is
gitignored and never edited by hand.

**Canonical directory shape:**

```
ios/Packages/TopUp/
  Package.swift
  Sources/TopUp/
    TopUpRoot.swift                    // THE public entry view + its Dependencies struct
    Views/                             // internal SwiftUI views
    Models/                            // internal @Observable state
    Flows/                             // internal multi-step coordinators
    Resources/Localizable.xcstrings    // UI copy owned by this package (en keys, fr translations)
  Sources/TopUpTestSupport/            // optional: doubles this package exports to others
    StubTopUpService.swift
  Tests/TopUpTests/                    // mirrors Views/ Models/ Flows/
    Models/TopUpModelTests.swift
```

The `Root` / `Views` / `Models` / `Flows` / `Resources` split is mandatory for feature packages and is
specified in `architecture-feature-internal-layout.md`. Capability packages use the folders their content
warrants — `DesignSystem` uses `Tokens/`, `Foundations/`, `Components/<Family>/`
(`architecture-design-system-library.md`); `Money` and `Platform` stay flat, one type per file.

**Manifest template — copy this, change the name and the dependencies:**

```swift
// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "TopUp",
    defaultLocalization: "en",              // English keys are the source; fr is a translation
    platforms: [
        .iOS(.v26),
    ],
    products: [
        .library(name: "TopUp", targets: ["TopUp"]),
    ],
    dependencies: [
        .package(path: "../DesignSystem"),
        .package(path: "../Money"),
        .package(path: "../Platform"),
        .package(path: "../WalletStore"),
    ],
    targets: [
        .target(
            name: "TopUp",
            dependencies: [
                .product(name: "DesignSystem", package: "DesignSystem"),
                .product(name: "Money", package: "Money"),
                .product(name: "Platform", package: "Platform"),
                .product(name: "WalletStore", package: "WalletStore"),
            ],
            resources: [.process("Resources/Localizable.xcstrings")],
            swiftSettings: [.defaultIsolation(MainActor.self)]   // UI package: main actor by default
        ),
        .testTarget(
            name: "TopUpTests",
            dependencies: [
                "TopUp",
                .product(name: "PlatformTestSupport", package: "Platform"),
                .product(name: "WalletStoreTestSupport", package: "WalletStore"),
            ]
        ),
    ]
)
```

**Manifest rules**

1. **`// swift-tools-version: 6.2`** on line 1 of every manifest. Tools 6.2 means Swift 6 language mode and
   full strict concurrency — do not re-enable it with `.swiftLanguageMode(.v5)` to silence warnings.
2. **`platforms: [.iOS(.v26)]`** everywhere. Adding `.macOS(...)` is allowed only for a package that genuinely
   builds for macOS previews; a feature package does not.
3. **`swiftSettings: [.defaultIsolation(MainActor.self)]` on UI-bearing targets** (DesignSystem, every
   feature, WalletStore's observable state). Leave it **off** for `Money` and `ApiClient`: those are
   `nonisolated` value/actor code and must stay callable from any isolation
   (`architecture-main-actor-ui.md`).
4. **`defaultLocalization: "en"` + `resources: [.process("Resources/Localizable.xcstrings")]`** on any package
   that owns user-facing copy. Keys and code are English; French lives in the catalog as a translation, never
   in a Swift literal. Without `defaultLocalization`, SwiftPM refuses localized resources; with it missing on
   a UI package, strings silently fall back to the key. `DesignSystem` also processes its whole
   `Resources/` folder because it ships fonts:
   ```swift
   .target(name: "DesignSystem",
           resources: [.process("Resources")],
           swiftSettings: [.defaultIsolation(MainActor.self)])
   ```
5. **One product per package**, named like the package — plus a `<Name>TestSupport` product when the package
   exports test doubles.
6. **`.package(path: "../X")` only.** No remote dependency enters a feature package without an ADR in
   `docs/adr/`.
7. **Every package has a `Tests/<Name>Tests/` target**, even a thin one. A package with no tests is a package
   nobody can refactor.
8. **A feature package's manifest never lists another feature package**, and `Gallery` lists nothing but
   `DesignSystem` (`architecture-feature-boundaries.md`).

**`TestSupport` targets**

A `TestSupport` target holds the doubles *other packages* need. It is a real product, compiled in release, so
keep it free of `XCTest`/`Testing` imports.

```swift
// ios/Packages/Platform/Package.swift
products: [
    .library(name: "Platform", targets: ["Platform"]),
    .library(name: "PlatformTestSupport", targets: ["PlatformTestSupport"]),
],
targets: [
    .target(name: "Platform"),
    .target(name: "PlatformTestSupport", dependencies: ["Platform"]),
    .testTarget(name: "PlatformTests", dependencies: ["Platform"]),
]
```

Doubles used by only one package stay `internal` inside `Tests/<Name>Tests/` — do not export them.

**Naming**

| Thing | Convention | Example |
|---|---|---|
| Package / folder / product / target | PascalCase, identical | `WalletStore` |
| File | one main type per file, `TypeName.swift` | `VirtualCard.swift` |
| Feature entry point | `<Feature>Root.swift`, the package's only `public` view | `CardsRoot.swift` |
| Screen | `XxxView.swift` | `CardDetailView.swift` |
| Multi-step flow | `XxxFlow.swift` | `CreateCardFlow.swift` |
| Observable state | `XxxModel.swift` | `TopUpModel.swift` |
| Protocol | `Xxxing.swift` | `KeyValueStoring.swift`, `WalletStoring.swift` |
| Exported double | in `Sources/<Name>TestSupport/` | `FixedClock.swift` |
| DesignSystem component | `Components/<Family>/<Component>.swift` | `Components/Buttons/MPButton.swift` |
| Design token | `Tokens/<Dimension>.swift` | `Tokens/Colors.swift` |

**Registering the package**: add it to `ios/project.yml` (`packages:` + the app target's `dependencies:`),
regenerate with XcodeGen, and add nothing to `.xcodeproj` by hand. `ios/.swiftlint.yml` already includes
`Packages/*/Sources` and `Packages/*/Tests`, so a new package is linted the moment it exists.

Reference: [Package Manifest API](https://docs.swift.org/package-manager/PackageDescription/PackageDescription.html) ·
[SE-0466 default actor isolation](https://github.com/swiftlang/swift-evolution/blob/main/proposals/0466-control-default-actor-isolation.md)
