# Handoff — repo setup for implementation (2026-08-28, afternoon)

Commit `0883dc6` set the monorepo up. Read `AGENTS.md` first, then `agents/README.md` (78 rules).

## What exists now
- `ios/` — SwiftUI app split into 14 local SwiftPM packages (see `AGENTS.md` → iOS). Build: `cd ios && xcodegen generate && ./scripts/build.sh`; lint: `swiftlint lint --strict`.
- `server/` — .NET 10 skeleton (Kernel, Data, Api host, Wallet module, tests). `HUSKY=0 dotnet build -warnaserror && dotnet test`.
- `agents/` — rules, commands, knowledge base. `CONTEXT.md` — ubiquitous language. `.husky/`, `.github/workflows/` per component.
- `poc/` (Node POC, still the live backend contract) and `prototype/` (HTML mockup) are unchanged.

## Gates (all green at this commit)
iOS: 0 errors, 0 warnings (warnings as errors in every package), SwiftLint strict 0, MainActor default isolation.
Server: analyzers + warnings as errors, `dotnet format --verify-no-changes`, 11 tests.

## Follow-ups, in the order they were discussed
1. **Localization extraction (iOS)**: 416 French UI literals in 29 files must move to per-package `Localizable.xcstrings`
   (English keys, French translation). `Fmt` in DesignSystem/Foundations/Formatting.swift hard-codes `fr_FR` and
   returns "Aujourd'hui"/"Hier"; SubscriptionsView sample data carries French day names. See `quality-localization`, `quality-english-only-code`.
2. **Gallery**: still depends on all feature packages (it is a screen index). Split into a DesignSystem-only component catalog
   and a screen catalog kept in App/. See `architecture-design-system-library`.
3. **Money package** imports DesignSystem, so `swift test --package-path Packages/Money` cannot run on macOS; tests run on the simulator in CI.
   Break that dependency (Money must not know UI) — see `architecture-layering`.
4. **Local simulator runtime**: Xcode 26.6 ships the iOS 26.5 SDK, only the 26.4 runtime is installed, so `xcodebuild test`
   finds no destination locally. Fix: `xcodebuild -downloadPlatform iOS` (~9 GB). CI (macos-26) is unaffected.
5. **Server**: replace the Wallet stub with the real ledger (`data-money-ledger`), Campay/Sudo typed clients, then generate the
   OpenAPI document and the iOS `ApiClient` from it (`monorepo-openapi-contract`, `scripts/update-openapi.sh`).
6. Previous commits went straight to `main`; from now on follow `ci-git-workflow` (branch + PR, draft by default).
