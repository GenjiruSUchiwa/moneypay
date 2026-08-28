---
title: Key File Locations
impact: LOW
impactDescription: Quick map of the MoniPay repository
tags: reference, navigation, file-locations, swiftpm
---

# Key File Locations

## Repository shape

```
ios/                    the iOS app, fully modularized into local SwiftPM packages
  project.yml           SOURCE OF TRUTH for the Xcode project (XcodeGen)
  .swiftlint.yml        included: App, Tests, Packages/*/Sources, Packages/*/Tests
  App/                  composition root ONLY
  Config/               xcconfig, PrivacyInfo.xcprivacy, entitlements
  Packages/<Name>/      one package per capability and per feature
  Tests/                app-target tests (composition root) only
  scripts/              helper scripts
docs/                   adr/, handoff/, architecture/, design/, api/
poc/                    Node POC backend (the API contract)
prototype/              HTML/JS mockup
agents/                 these rules
```

`ios/MoniPay.xcodeproj` **is generated and gitignored**. It never appears in a diff; regenerate it
with `cd ios && xcodegen generate` (see `ci-git-workflow`).

## `ios/App/` — composition root only

`MoniPayApp.swift`, `RootView.swift`, `MainTabView.swift`, `AppConfiguration.swift`,
`Assets.xcassets`. **No feature code, no domain logic, no networking lives here.** If you are about
to add a type to `App/`, it belongs in a package.

## `ios/Packages/` — one package per capability, strict dependency direction

A package may only depend on packages **above** it in this list. Features never depend on each other.

| Package | Owns |
|---|---|
| `Platform` | `Clock`, `Logging`, `KeyValueStoring` + `PlatformTestSupport` (`FixedClock`, `InMemoryKeyValueStoring`) |
| `DesignSystem` | `Theme`, `Components`, `Keypad`, `CardArt`, `Viz`, fonts — depends on SwiftUI only |
| `Money` | `Money`, `Currency`, `FXRate` and the domain models: `VirtualCard`, `Transaction`, `TxStatus`/`TxKind`/`TxCategory`, `TopUpMethod`, `User`, `AppNotification` |
| `ApiClient` | Transport to the POC backend: DTOs mirroring `poc/README.md`, `URLSession`, error mapping |
| `WalletStore` | `actor Wallet`, `@Observable Store`, `SampleData` — depends on `Money` + `ApiClient` + `Platform` |
| `Onboarding` | `SplashView`, `WelcomeView`, `SignUpFlow` |
| `Home` | `HomeView`, `InsightsView`, `AuthorizationSheet` |
| `Cards` | `CardsListView`, `CardDetailView`, `CardControlsView`, `CreateCardFlow` |
| `TopUp` | `TopUpFlow` (MoMo top-up) |
| `Convert` | `ConvertView` (FCFA ↔ USD) |
| `Transactions` | `TransactionsView` |
| `KYC` | `KYCFlow` |
| `Settings` | `SettingsView`, `SubScreens` |
| `Gallery` | `GalleryView`, `ScreenCatalog` — dev-only screen catalog |

**Package internals** (identical everywhere; package name == folder name == product name):

```
ios/Packages/Money/
  Package.swift                     swift-tools-version 6.2, platforms: .iOS(.v26)
  Sources/Money/                    product code
  Sources/MoneyTestSupport/         reusable test doubles (optional)
  Sources/Money/Resources/Localizable.xcstrings   when the package owns UI copy
  Tests/MoneyTests/                 the package's tests
  Package.resolved                  committed as soon as the package has a remote dependency
```

## Money, specifically

`ios/Packages/Money/Sources/Money/` is where FCFA and USD live: **XAF has 0 decimals, USD has
cents. Never `Double` for a balance.** `FXRate` rounds up in the fintech's favour. The just-in-time
authorization ledger (`Wallet`, `AuthDecision`, holds/capture/void) is in
`ios/Packages/WalletStore/Sources/WalletStore/Wallet.swift`.

## `ios/Config/`

`Debug.xcconfig` / `Release.xcconfig` define `API_BASE_URL`, surfaced to the app as the Info.plist
key `APIBaseURL` and read by `App/AppConfiguration.swift`. Also `PrivacyInfo.xcprivacy` and the
entitlements files. The generated `Config/MoniPay-Info.plist` is gitignored.

## Backend POC and prototype (unchanged, at the repo root)

- **Wire contract: `poc/README.md`** — see `api-backend-contract`
- Server (single file, zero dependency, Node 18+): `poc/server.js`; smoke script `poc/smoke-sudo.js`
- Sandbox credentials: `poc/.env` — **never committed**
- HTML prototype on `:8742`: `prototype/index.html`, `app.js`, `styles.css`, `build.py`

## Documents

`docs/handoff/` (the former `HANDOFF*.md`, incl. the Campay → wallet → Sudo money POC),
`docs/design/` (the former `DESIGN.md`), plus `docs/adr/`, `docs/architecture/`, `docs/api/`.
Agent rules: `agents/rules/`.

## Naming conventions

- Package == folder == product name, PascalCase.
- One main type per file, named after it: `Wallet.swift`, `FXRate.swift`.
- Views `XxxView.swift`, flows `XxxFlow.swift`, observable state `XxxModel.swift`,
  protocols `Xxxing.swift` (`KeyValueStoring`, `Logging`, `CardIssuing`).
- Tests mirror sources: `Sources/Money/FXRate.swift` → `Tests/MoneyTests/FXRateTests.swift`.
- Test doubles live in `Sources/<Name>TestSupport/`, prefixed `Fixed…`, `InMemory…`, `Stub…`.
- UI-test accessibility identifiers are English and dotted: `topup.confirm.button`.
