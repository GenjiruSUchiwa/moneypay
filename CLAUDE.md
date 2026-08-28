# MoniPay Development Guide for AI Agents

You are a senior engineer on the MoniPay **SwiftUI + .NET monorepo**: a fintech product for the
CEMAC zone (FCFA wallet, MTN MoMo / Orange Money top-ups, USD virtual cards), with a SwiftUI app
in `ios/` and an ASP.NET Core modular monolith in `server/`. You target **iOS 26 / Swift 6.3**
under strict concurrency and **.NET 10 / C#** with nullable enabled and warnings as errors. You
never represent money as a floating-point number, and you ship small, reviewable diffs.
**Everything you write is English** — identifiers, comments, tests, logs, commits, docs. The app
speaks French to its users, and that French lives only in the localization catalogs, keyed by
English source strings.

Work on one component at a time: a change to `ios/` and a change to `server/` are two PRs unless
they are the two halves of one API contract change.

## Do

### Everywhere

- Model money as integer minor units in Swift (`Int` FCFA — XAF has **0** decimals — and `Int` cents for USD) and as `decimal` + an ISO currency code in C#, stored in minor units — see [data-money-representation](agents/rules/data-money-representation.md)
- Use `Decimal` / `decimal` (never `Double` / `double`) for FX rates, margins and any value that gets rounded into money
- Write everything in English — code, comments, tests, logs, commits, PR titles, documentation — and put user-facing copy in the localization catalogs with English source keys — see [quality-english-only-code](agents/rules/quality-english-only-code.md)
- Localize at the edge: translations are data, and money, dates and numbers are formatted against the user's locale, never assembled by hand — see [Internationalization](#internationalization), [quality-localization](agents/rules/quality-localization.md), [api-localization](agents/rules/api-localization.md)
- Use conventional commits with a component scope: `feat(api): …`, `feat(ios): …` — enforced by the husky `commit-msg` hook and the `pr-title` workflow
- Regenerate and commit the OpenAPI document when you change an endpoint or a contract — see [monorepo-openapi-contract](agents/rules/monorepo-openapi-contract.md)

### Swift / iOS

- Use `@Observable` + `@Environment` / `@Bindable` for state — see [patterns-observable-state](agents/rules/patterns-observable-state.md)
- Keep every view and view-owned state `@MainActor`; push I/O and computation into `actor`s or `nonisolated` async functions
- Make domain models `Sendable` value types (`struct`, `enum`) so they cross isolation boundaries for free
- Use typed throws (`throws(TopUpError)`) when the caller must switch exhaustively on the failure
- Use `guard` + early return instead of nesting: `guard let card else { return }`
- Check the `swiftui-skills` docs before using an iOS 26 API — invoke the skill first, cite the doc, never guess a signature (see [Apple API source of truth](#apple-api-source-of-truth))
- Use `NavigationStack` with a typed path; never `NavigationView`
- Put every feature in its own SwiftPM package under `ios/Packages/<Feature>/`, laid out as `<Feature>Root.swift` + `Views/` + `Models/` + `Flows/`; expose exactly one public entry view
- Reuse `DesignSystem` components — never re-create a button, row, field, pill or section header inside a feature, and never restyle one inline
- Promote a visual pattern into `DesignSystem` before its second use ships, one component per file with variants as enums and a `#Preview` covering them all
- Use semantic tokens (`.surface`, `.action`, `.inkQuiet`) for every colour, font, spacing and radius; raw hex exists only in `DesignSystem/Tokens/`
- Talk to the backend through a protocol owned by `ApiClient`, with a live and a preview implementation — see [data-repository-pattern](agents/rules/data-repository-pattern.md)
- Write tests with Swift Testing (`@Test`, `#expect`, `#require`) — see [testing-swift-testing](agents/rules/testing-swift-testing.md)
- Add a `#Preview` for every new view, using in-memory sample data
- Run `swiftlint` before pushing; run `xcodegen generate` from `ios/` after adding a package or a file the app target references
- Open PRs in draft by default — see [ci-git-workflow](agents/rules/ci-git-workflow.md)
- Only add comments that explain **why**, not **what** — see [quality-code-comments](agents/rules/quality-code-comments.md)
- Prefer deleting code to adding an abstraction — see [quality-simplicity](agents/rules/quality-simplicity.md)

### C# / .NET

- Keep nullable reference types enabled and every warning an error; fix the code, never suppress the diagnostic — see [quality-csharp-style](agents/rules/quality-csharp-style.md)
- Put every bounded context in its own `MoniPay.<Module>` project owning `Endpoints/`, `Contracts/`, `Domain/`, `Persistence/` and a `<Module>Module.cs` that registers both DI and routes — see [architecture-dotnet-modular-monolith](agents/rules/architecture-dotnet-modular-monolith.md)
- Keep `MoniPay.Api` host-only (composition, middleware, OpenAPI), `MoniPay.Data` for the `DbContext` and migrations, `MoniPay.Kernel` for primitives with no business logic
- Use `record` types for API contracts (requests, responses, events): value equality, immutable by default
- Keep endpoints thin: minimal-API handlers map HTTP to a module service and back, nothing more — see [api-minimal-endpoints](agents/rules/api-minimal-endpoints.md)
- Treat the generated OpenAPI document as **the** contract between `server/` and the iOS `ApiClient`
- Declare package versions centrally in `server/Directory.Packages.props`; a `.csproj` names the package without a version — see [monorepo-package-management](agents/rules/monorepo-package-management.md)
- Write tests with xunit.v3 on Microsoft.Testing.Platform, against a real database through Testcontainers — see [testing-xunit-testcontainers](agents/rules/testing-xunit-testcontainers.md)
- Keep secrets in `dotnet user-secrets` locally and in environment variables (`MoniPay__Section__Key`) elsewhere
- Add an EF Core migration for every schema change and review the generated SQL — see [data-efcore-migrations](agents/rules/data-efcore-migrations.md)

## Don't

- **Never bypass a linter or a compiler diagnostic.** No `// swiftlint:disable`, no `#pragma warning disable`, no `[SuppressMessage]`, no `<NoWarn>`, no `.editorconfig` or `.swiftlint.yml` carve-out, no raised complexity threshold, no lenient flag. Fix the root cause; if the rule itself is wrong, change the shared config in its own PR and say why. Warnings are errors on both sides — iOS (`SWIFT_TREAT_WARNINGS_AS_ERRORS`, `swiftlint --strict`) and backend (`TreatWarningsAsErrors`, `dotnet format --verify-no-changes`). See [quality-zero-warnings](agents/rules/quality-zero-warnings.md) and [quality-cyclomatic-complexity](agents/rules/quality-cyclomatic-complexity.md)
- Never use `Double` or `Float` for a balance, an amount, a fee or a limit
- Never force unwrap (`!`), force try (`try!`) or force cast (`as!`) in shipped code
- Never use `ObservableObject` / `@StateObject` / `@Published` — this codebase is `@Observable` only
- Never use `@unchecked Sendable` to silence a concurrency error; fix the ownership instead
- Never block the main actor: no `DispatchSemaphore`, no synchronous network call, no `Thread.sleep`
- Never put networking, `URLSession` or provider-specific JSON in a `View`
- **Never write French in code.** Identifiers, comments, doc comments, commit messages, PR titles, test names, log messages, error messages and API contracts are English. User-facing copy lives in `Localizable.xcstrings` — English source keys, French translation, `defaultLocalization` / `developmentLanguage: en` — and in the server's resource files. See [quality-english-only-code](agents/rules/quality-english-only-code.md)
- Never put user-facing copy of any language in `Money` or `Platform` — domain and capability packages stay presentation-free
- Never format money, a date or a number by hand, and never concatenate a sentence from fragments — use a `FormatStyle` with the caller's `Locale`, and one interpolated key per sentence
- Never return pre-formatted or pre-translated values from an API endpoint — send the amount with its currency code, the timestamp as ISO-8601 UTC, and a stable machine code; the client formats
- Never import one feature package from another; if two features need the same thing, it moves down a layer
- Never re-create a design-system component in a feature — no local `struct PrimaryButton`, no hand-rolled row, no ad-hoc field
- Never inline-style a component at the call site (`.background(Color(red:…))`, a literal corner radius, a hardcoded font size); add a variant to the component instead
- Never write a raw colour, font, spacing or radius literal outside `DesignSystem/Tokens/`
- Never make a feature's internal view or model `public` — only `<Feature>Root` is
- Never put feature logic in `ios/App/` — it is a composition root, nothing else
- Never commit secrets: `poc/.env` holds the Campay and Sudo sandbox keys and is git-ignored
- Never edit or commit `MoniPay.xcodeproj` — it is git-ignored and regenerated from `ios/project.yml` by XcodeGen
- Never add a dependency without asking; the app has **zero** third-party packages today
- Never use APIs deprecated before iOS 26 (`NavigationView`, `.onChange(of:perform:)`, `UIScreen.main`, Combine for view state)
- Never invent an iOS 26 API signature from memory — if it is not in the `swiftui-skills` docs, say so and propose a documented alternative
- Never use `double` or `float` for money in C#, and never round a monetary `decimal` implicitly
- Never let an analyzer diagnostic through by widening its scope — a suppression, a `<NoWarn>` or a baseline entry all mean the same thing, and none of them are allowed (see the first Don't)
- Never use `null!`, `!` (null-forgiving) or `.Result` / `.Wait()` on a `Task`
- Never reference one `MoniPay.<Module>` project from a sibling module; go through `Kernel` or the composition root in `MoniPay.Api`
- Never add an `Endpoints/` or `Contracts/` folder to `MoniPay.Api` — a module's HTTP surface belongs to the module
- Never put business logic in `MoniPay.Kernel`, and never put a module's entity configuration in `MoniPay.Data`
- Never write routes, endpoint names, summaries, tags, policy names or configuration keys as inline string literals — declare them in per-module static classes (`WalletRoutes`, `WalletEndpointNames`, `WalletTags`) and reference those from the endpoints, the tests and the OpenAPI metadata — see [api-no-magic-strings](agents/rules/api-no-magic-strings.md)
- Never hand-edit `docs/api/openapi.json` — it is generated from the API
- Never pin a package version inside a `.csproj` — versions live in `Directory.Packages.props`
- Never commit a connection string, a signing key or a provider secret, in any file
- Never create large PRs (>500 lines or >10 code files) — split them instead
- Never mix an `ios/` change and a `server/` change in one PR unless they are the two halves of one contract change

## PR Size Guidelines

Large PRs are hard to review, hide regressions, and slow everyone down. Aim for small,
self-contained PRs.

### Size limits

- **Lines changed**: under 500 (additions + deletions)
- **Files changed**: under 10 code files
- **Single responsibility**: one PR does one thing

These limits cover code only. Documentation (`docs/`, `*.md`) and asset catalogs are excluded
from the count; the `.xcodeproj` is git-ignored and never appears in a diff at all.

### How to split large changes

1. **By component**: `server/` and `ios/` are separate PRs — the API contract lands first
2. **By layer**: domain model + tests first, then persistence / repository, then UI
3. **By module or package**: one `MoniPay.<Module>` or one Swift package per PR
4. **By refactor vs feature**: land the preparatory refactor before the behavior change
5. **By dependency order**: what must merge first, merges first

### Example split

Instead of one large "live MoMo top-up" PR:

- PR 1 (`api`): `MoniPay.TopUps` module — collection request, states, xunit coverage
- PR 2 (`api`): the `/topups` endpoints and contracts + regenerated `docs/api/openapi.json`
- PR 3 (`ios`): `TopUpRequest` / `TopUpError` in the `Money` package + Swift Testing coverage
- PR 4 (`ios`): `TopUpCollecting` and its live implementation in `ApiClient`
- PR 5 (`ios`): `TopUp` feature package wired to it, with pending/polling UI states
- PR 6: failure surfaces (declined, timeout, cap exceeded) and analytics

## Commands

See [agents/commands.md](agents/commands.md) for the full reference.

iOS — from `ios/`:

```bash
xcodegen generate                                   # regenerate MoniPay.xcodeproj from project.yml
xcodebuild -scheme MoniPay \
  -destination 'platform=iOS Simulator,name=iPhone 17 Pro' build
xcodebuild -scheme MoniPay \
  -destination 'platform=iOS Simulator,name=iPhone 17 Pro' test
swift test --package-path Packages/Platform         # fastest loop — UIKit-free packages only (Platform, ApiClient)
xcodebuild -scheme Money \
  -destination 'platform=iOS Simulator,name=iPhone 17 Pro' test   # from Packages/Money: any package that imports DesignSystem
swiftlint                                           # lint (swiftlint --fix to autocorrect)
```

Backend — from the repository root:

```bash
dotnet build server/MoniPay.slnx --configuration Release
dotnet test server/MoniPay.slnx                     # needs a Docker daemon (Testcontainers)
dotnet format server/MoniPay.slnx --verify-no-changes
dotnet run --project server/src/MoniPay.Api
./scripts/update-openapi.sh                         # regenerate docs/api/openapi.json
node poc/server.js                                  # the Node POC backend on :8743
```

## Boundaries

### Always do

- Run the package you touched on its own — it is far faster than the app scheme. `swift test --package-path Packages/<Name>` works only for the UIKit-free packages (`Platform`, `ApiClient`): the host `swift` toolchain builds for macOS, and `DesignSystem` imports UIKit. Every other package (`Money` included) is tested with `xcodebuild test -scheme <Name> -destination '…'` run from `Packages/<Name>`, exactly as CI does
- Run `dotnet test server/MoniPay.slnx` for a backend change, and `dotnet format --verify-no-changes` before pushing
- Build before concluding a change compiles
- Run `swiftlint` before pushing an iOS change
- Regenerate the project (`xcodegen generate` from `ios/`) whenever you add a package or a file the app target references
- Regenerate the OpenAPI document after an endpoint or contract change
- Keep money in minor units end to end

### Ask first

- Adding a **third-party** dependency, on either side (local Swift packages under `ios/Packages/` are the normal way to structure code)
- Adding a new Swift package or a new `MoniPay.<Module>` project, or moving code between them
- Changing `ios/project.yml`, `ios/Config/*.xcconfig`, `server/Directory.*.props` or `server/MoniPay.slnx`
- A database schema change or a new EF Core migration
- A breaking API change (any change that makes an older app version fail)
- Changing the FX rate, the margin or any pricing constant
- Touching `prototype/` or `poc/` (the HTML mockup and the reference POC backend)

### Never do

- Commit `poc/.env`, connection strings, signing keys, API keys, or a real PAN / CVV
- Log a full card number, CVV, phone number or OTP
- Force push or rebase `main`
- Commit or hand-edit generated files (`ios/MoniPay.xcodeproj`, `docs/api/openapi.json`, `prototype/dist/index.html`, `CHANGELOG.md`)
- Bypass a git hook with `--no-verify`, or a linter with a suppression

## Monorepo Layout

Two shipping components — the iOS app and the API — plus the shared scaffolding that keeps them
honest. Each component builds, tests, versions and releases independently.
See [monorepo-layout](agents/rules/monorepo-layout.md).

```
moneypay/
  ios/                     # SwiftUI app (see "iOS app" below)
  server/                  # .NET modular monolith: the HTTP API (see "Backend" below)
  poc/                     # Node reference POC: Campay MoMo -> FCFA -> Sudo USD cards.
                           # Defines today's API contract; stays until server/ replaces it.
  prototype/               # HTML/CSS/JS design prototype (reference for the visual design)
  docs/                    # adr/, architecture/, api/ (openapi.json), contracts/,
                           # handoff/, design/, ops/
  agents/                  # this documentation set (rules, commands, knowledge base)
  scripts/                 # update-openapi.sh, check-openapi-sync.sh, …
  .husky/                  # git hooks: commit-msg (conventional commits), pre-commit
                           # (dotnet format on staged C#, swiftlint on staged Swift)
  .github/workflows/       # one CI + release workflow per component, plus pr-title
  dotnet-tools.json        # .NET local tool manifest (husky, …)
  .editorconfig            # shared formatting and C# code-style rules
  AGENTS.md / CLAUDE.md    # this guide (byte-identical copies)
```

Workflows are named per component: `monipay-api-ci` / `monipay-api-release`,
`monipay-ios-ci` / `monipay-ios-release` / `monipay-ios-version`, and `pr-title`. A workflow only
runs for paths belonging to its component — see
[monorepo-ci-per-component](agents/rules/monorepo-ci-per-component.md).

The first `dotnet restore` installs the git hooks, so a fresh clone is convention-enforcing
without a setup step. `HUSKY=0` disables them — that is for CI and container builds, never for
your own commits.

## iOS app (`ios/`)

The app lives entirely under `ios/`. Every feature and every shared capability is a **local
SwiftPM package**; the app target is only a composition root.

```
ios/
  project.yml            # XcodeGen manifest — the source of truth for the Xcode project
  MoniPay.xcodeproj      # GENERATED and GIT-IGNORED — never commit it, never edit it
  App/                   # composition root only: MoniPayApp.swift, RootView.swift,
                         # MainTabView.swift, AppConfiguration.swift, Assets.xcassets
  Config/                # Debug.xcconfig, Release.xcconfig (API_BASE_URL -> Info.plist
                         # key APIBaseURL), PrivacyInfo.xcprivacy, entitlements
  Packages/              # one SwiftPM package per feature / capability (see below)
  Tests/                 # app-target tests ONLY (composition root); package logic is
                         # tested inside its own package
  scripts/               # helper scripts
  .swiftlint.yml         # included: App, Tests, Packages/*/Sources, Packages/*/Tests
```

### Packages

```
ios/Packages/
  Platform/        # Clock, Logging, KeyValueStoring (+ PlatformTestSupport: FixedClock,
                   # InMemoryKeyValueStoring). Depends on nothing.
  DesignSystem/    # the component library: Tokens, Foundations, Components, fonts. SwiftUI only.
  Money/           # Money, Currency, FXRate + domain models: VirtualCard, Transaction,
                   # TxStatus/TxKind/TxCategory, TopUpMethod, User, AppNotification.
  ApiClient/       # transport to the POC backend: DTOs mirroring poc/README.md, URLSession,
                   # error mapping. Depends on Money (+ Platform).
  WalletStore/     # Wallet actor, Store (@Observable app state), SampleData for previews.
                   # Depends on Money + ApiClient + Platform.
  Onboarding/      # Splash, Welcome, SignUp
  Home/            # Home, Insights, AuthorizationSheet
  Cards/           # CardsList, CardDetail, CardControls, CreateCardFlow
  TopUp/  Convert/  Transactions/  KYC/  Settings/
  Gallery/         # dev-only component catalog — depends on DesignSystem only
```

### Package anatomy

```
ios/Packages/TopUp/
  Package.swift
  Sources/TopUp/
    TopUpRoot.swift                # the single public entry view + its dependencies struct
    Views/                         # internal SwiftUI views
    Models/                        # @Observable state and view models
    Flows/                         # multi-step coordinators
    Resources/Localizable.xcstrings  # when the package owns UI copy
  Sources/TopUpTestSupport/        # optional: test doubles for other packages
  Tests/TopUpTests/                # mirrors Views/ Models/ Flows/
```

A feature package exposes **one** public entry point — `<Feature>Root` — plus the dependency
struct it needs. Everything else is `internal`: the composition root wires the feature by
constructing its root view, never by reaching into one of its screens. Tests mirror the source
folders, so a reader finds `Tests/TopUpTests/Models/TopUpAmountModelTests.swift` where they
expect it.

### DesignSystem — the component library

Every reusable visual element lives in `DesignSystem`, one component per file. Features compose
components; they never re-create one and never restyle one inline.

```
ios/Packages/DesignSystem/Sources/DesignSystem/
  Tokens/          # Colors.swift, Typography.swift, Spacing.swift, Radii.swift, Motion.swift
                   # semantic names only: .surface, .action, .inkQuiet — raw hex lives here and
                   # nowhere else
  Foundations/     # Theme.swift (environment / colour scheme), Surfaces.swift (card and glass
                   # modifiers), Press.swift (ButtonStyle), shared ViewModifiers
  Components/
    Buttons/       # MPButton, QuickAction, IconTile
    Typography/    # MoneyText, Eyebrow, SectionHead
    Lists/         # Row, RowValue, RuledStack, Rule
    Feedback/      # StatusPill, Chip, Toast, EmptyNote, SuccessMark
    Inputs/        # Field, Segments, Keypad
    Navigation/    # NavBar
    Cards/         # CardArt
    DataViz/       # Meter, Viz
  Resources/       # fonts, Localizable.xcstrings
```

Every component must have:

- a `public` type and a `public init` with labeled parameters
- variants expressed as enums (`Tone`, `Size`), never as a pile of boolean flags
- an accessibility label and the right traits
- a `#Preview` showing **all** variants side by side
- a `///` doc comment stating when to use it — and when to use a different component instead

**Rule of promotion**: a visual pattern that appears in a second feature moves into
`DesignSystem` *before* that second use ships. Two near-identical row styles in two features is a
bug, not a coincidence.

The `Gallery` package is the **component catalog**: one screen per component family showing every
component and every variant, for visual review. It depends on `DesignSystem` only — if a catalog
screen needs a feature type, the component belongs in the feature, not in the library.

### Dependency direction

A package may only depend on packages **above** it in this list. There are no cycles and no
sideways edges:

1. `Platform` (nothing) and `DesignSystem` (SwiftUI only)
2. `Money`
3. `ApiClient`
4. `WalletStore`
5. Feature packages — depend on `DesignSystem` + `Money` + `WalletStore` (and `ApiClient` /
   `Platform` when needed), and **never on each other**
6. `App` — depends on everything, owns no feature logic

If two features need the same type, it moves **down** a layer; it never gets imported sideways.
See [architecture-feature-boundaries](agents/rules/architecture-feature-boundaries.md).

### Naming

- Package name == folder name == product name, PascalCase (`TopUp`, `DesignSystem`)
- One main type per file, named after it: `TypeName.swift`
- A feature's public entry view is `<Feature>Root.swift`
- Views `XxxView.swift`, multi-step flows `XxxFlow.swift`, observable state `XxxModel.swift`
- Design-system components carry no feature name: `Row`, `StatusPill`, `MoneyText` — a component
  called `TopUpRow` is a sign it belongs in the feature instead
- Protocols are named for the capability, `-ing` style: `KeyValueStoring`, `Logging`, `TopUpCollecting`
- Test doubles live in `Sources/<Name>TestSupport/`, never in the shipping target
- Tests: `Tests/<Name>Tests/`

### Apple API source of truth

**Before you write or review any SwiftUI / iOS 26 code, invoke the `swiftui-skills` skill** and
ground every API you use in its `docs/*.md`. These are Apple's own AdditionalDocumentation files
extracted from Xcode 26 — they are the authority for anything introduced in this release, and
they exist precisely because iOS 26 APIs are new enough to be hallucinated. If an API is not in
those docs, say so and offer a documented alternative instead of inventing a signature. See
[reference-apple-docs-skill](agents/rules/reference-apple-docs-skill.md).

The skill lives at `~/.claude/skills/swiftui-skills`. If `docs/` is empty, the Xcode
documentation has not been extracted yet — run `~/.claude/skills/swiftui-skills/setup.sh` and do
not give SwiftUI guidance until it is populated.

The 20 documents, and when to open each:

| Document | Use when |
|---|---|
| `SwiftUI-Implementing-Liquid-Glass-Design.md` | any `.glassEffect`, `GlassEffectContainer`, `glassEffectID`, morphing or tinted glass — our whole design system leans on it |
| `UIKit-Implementing-Liquid-Glass-Design.md` | glass in a UIKit-hosted surface |
| `AppKit-Implementing-Liquid-Glass-Design.md` | glass on macOS (not our target; reference only) |
| `WidgetKit-Implementing-Liquid-Glass-Design.md` | a balance or card widget |
| `SwiftUI-New-Toolbar-Features.md` | toolbars, toolbar spacers, bottom bars, the new placement API |
| `SwiftUI-Styled-Text-Editing.md` | styled text display and editing, rich `TextEditor` |
| `SwiftUI-WebKit-Integration.md` | embedding web content (3-D Secure, provider consent pages) |
| `SwiftUI-AlarmKit-Integration.md` | scheduled alerts and alarms |
| `Swift-Concurrency-Updates.md` | Swift 6.2 concurrency: default isolation, `nonisolated(nonsending)`, data-race safety — read before touching an actor or an `async` boundary |
| `Swift-InlineArray-Span.md` | `InlineArray` / `Span` in a hot path |
| `SwiftData-Class-Inheritance.md` | if and when persistence lands on SwiftData |
| `Foundation-AttributedString-Updates.md` | building or transforming `AttributedString` (amount and merchant formatting) |
| `Swift-Charts-3D-Visualization.md` | Swift Charts, including the 3-D chart types, for the Analyse screen |
| `FoundationModels-Using-on-device-LLM-in-your-app.md` | on-device model work (categorisation, insights) |
| `AppIntents-Updates.md` | Shortcuts, Spotlight and system-wide app actions |
| `StoreKit-Updates.md` | in-app purchases and subscription merchandising views |
| `MapKit-GeoToolbox-PlaceDescriptors.md` | `PlaceDescriptor` / MapKit work (agent and merchant locations) |
| `Implementing-Assistive-Access-in-iOS.md` | an Assistive Access experience |
| `Implementing-Visual-Intelligence-in-iOS.md` | visual search integration |
| `Widgets-for-visionOS.md` | visionOS widgets (not our target; reference only) |

Working method the skill expects: name the docs you used, plan in a few bullets, then write
compile-ready Swift, and state which doc backs each non-obvious API choice.

### Key files

- App entry: `ios/App/MoniPayApp.swift`
- Phase machine (splash → welcome → signup → KYC → main): `ios/App/RootView.swift`
- Tab shell: `ios/App/MainTabView.swift`
- Environment / API base URL: `ios/App/AppConfiguration.swift`, `ios/Config/*.xcconfig`
- Money and FX: `ios/Packages/Money/Sources/Money/`
- Authorization / holds ledger and app state: `ios/Packages/WalletStore/Sources/WalletStore/`
- Design tokens: `ios/Packages/DesignSystem/Sources/DesignSystem/Tokens/`
- Components: `ios/Packages/DesignSystem/Sources/DesignSystem/Components/<Family>/`
- Component catalog: `ios/Packages/Gallery/`
- Project generation: `ios/project.yml`
- API host and composition root: `server/src/MoniPay.Api/Program.cs`, `MoniPayModules.cs`
- Shared MSBuild properties and package versions: `server/Directory.Build.props`, `server/Directory.Packages.props`
- API contract: `docs/api/openapi.json` (generated); today's reference contract: `poc/README.md`
- Handoffs, decisions and design history: `docs/handoff/`, `docs/adr/`, `docs/design/`

## Tech Stack (iOS)

- **Language**: Swift 6.3 (Xcode 26.6), strict concurrency, main-actor-by-default isolation
- **UI**: SwiftUI on iOS 26 — Liquid Glass (`.glassEffect`, `GlassEffectContainer`), `NavigationStack`, `#Preview`
- **State**: `@Observable` / `@Bindable` / `@Environment`; `actor` for the wallet ledger
- **Concurrency**: structured `async`/`await`, `Sendable` value types, `AsyncSequence` for streams
- **Modularization**: local SwiftPM packages (`swift-tools-version: 6.2`, `platforms: [.iOS(.v26)]`)
- **Project generation**: XcodeGen (`ios/project.yml`); the `.xcodeproj` is git-ignored
- **Localization**: String Catalogs (`Localizable.xcstrings`) per package that owns UI copy
- **Lint**: SwiftLint (`/opt/homebrew/bin/swiftlint`); swiftformat is **not** installed
- **Tests**: Swift Testing (`@Test`, `#expect`) — XCTest only where Swift Testing has no equivalent
- **Providers**: Campay (MTN / Orange Money collection, Cameroon), Sudo Africa (USD virtual cards)
- **Persistence**: none yet — the store is in memory. Use SwiftData if persistence is introduced.

## Backend (`server/`)

A modular monolith that serves the HTTP API for the iOS app. Until it exists, `poc/server.js`
(Node, zero dependency, `:8743`) is the reference implementation and defines the current
contract — see `poc/README.md`. The POC is **not** replaced piecemeal; it stays until the .NET
server takes over.

### Tech stack

| Part | Technology |
|---|---|
| Runtime | .NET 10 (`net10.0`), C# with nullable reference types, warnings as errors |
| Web | ASP.NET Core minimal APIs |
| Database | PostgreSQL through Npgsql and Entity Framework Core |
| Logs | Serilog |
| Tests | xunit.v3 on Microsoft.Testing.Platform, with Testcontainers |
| Packages | Central Package Management (`Directory.Packages.props`) |
| Versions | MinVer, from git tags prefixed `monipay.api-v` |
| Contract | OpenAPI document generated from the API into `docs/api/openapi.json` |

### Layout

```
server/
  MoniPay.slnx               # solution
  Directory.Build.props      # nullable, warnings as errors, MinVer, husky bootstrap
  Directory.Packages.props   # central package versions — .csproj files carry none
  dotnet.config              # selects the Microsoft.Testing.Platform test runner
  cliff.toml                 # git-cliff config for server/CHANGELOG.md
  CHANGELOG.md               # generated per release, never hand-edited
  Dockerfile                 # runtime image
  src/MoniPay.<Module>/      # one class library per bounded context (see below)
  src/MoniPay.Api/           # the HTTP host, and nothing else
  src/MoniPay.Data/          # DbContext + migrations
  src/MoniPay.Kernel/        # primitives shared by every module
  tests/MoniPay.Tests/       # Fakes/, Support/, one folder per module
```

Modules follow the domain: `Users`, `Sessions` (auth / PIN), `Wallet` (FCFA ledger), `TopUps`
(Campay collection, webhooks / polling), `Cards` (Sudo Africa issuing), `Transactions`, `Fx`
(rates and conversion), `Kyc`, `Notifications`, `Outbox` — plus the three infrastructure
projects `Kernel`, `Data` and `Api`.

### A module owns its whole vertical slice

```
server/src/MoniPay.TopUps/
  Endpoints/            # IEndpointRouteBuilder extension: the module's HTTP surface
    TopUpRoutes.cs      # route templates — no path literal anywhere else
    TopUpEndpointNames.cs, TopUpTags.cs   # endpoint names, OpenAPI tags, policy keys
  Contracts/            # request / response records for those endpoints
  Domain/               # entities, value objects, services — the business logic
  Persistence/          # IEntityTypeConfiguration for the module's own tables
  TopUpsModule.cs       # DI registration + endpoint mapping, the module's only entry point
  MAP.md
```

Routes, endpoint names, OpenAPI tags and summaries, authorization policy names and configuration
keys are **constants on per-module static classes**, never literals at the call site: an endpoint,
its test and the OpenAPI metadata then name the same symbol, and a renamed route breaks the build
instead of a running client. See [api-no-magic-strings](agents/rules/api-no-magic-strings.md).

The split of responsibilities, and the reason for each:

- **`MoniPay.<Module>`** owns its endpoints, its contracts, its domain and its EF
  configurations. Adding a feature touches one project. A module **never** references a sibling
  module — shared concepts move into `Kernel`, and cross-module orchestration happens at the
  host.
- **`MoniPay.Api`** is host-only: `Program.cs`, module composition, middleware, error handling,
  rate limiting, the OpenAPI document, `wwwroot`. It has no `Endpoints/` and no `Contracts/`
  folder of its own — if every module's HTTP surface lived here, every module would leak into
  the host.
- **`MoniPay.Data`** owns the `DbContext` and the migrations, and composes the modules'
  `IEntityTypeConfiguration` implementations by scanning their assemblies. One database, one
  migration history, but each module still describes its own tables.
- **`MoniPay.Kernel`** holds primitives only — `Money`, `Clock`, error types, common
  abstractions. No business logic ever goes in here; if it is about top-ups, it belongs to
  `TopUps`.

See [architecture-dotnet-modular-monolith](agents/rules/architecture-dotnet-modular-monolith.md)
and [api-minimal-endpoints](agents/rules/api-minimal-endpoints.md).

### Money on the backend

`decimal` plus an ISO currency code, stored in **minor units** (XAF has none, so a XAF minor unit
is one franc; USD is cents). Never `double`, never `float`, and never a bare number without its
currency. The rounding direction is a business decision, not a formatting detail — see
[data-money-representation](agents/rules/data-money-representation.md).

### Commands

From the repository root:

```bash
dotnet restore server/MoniPay.slnx                  # also installs the git hooks
dotnet build server/MoniPay.slnx --configuration Release
dotnet test server/MoniPay.slnx                     # needs a Docker daemon (Testcontainers)
dotnet format server/MoniPay.slnx --verify-no-changes
dotnet run --project server/src/MoniPay.Api
dotnet ef migrations add <Name> --project server/src/MoniPay.Data \
  --startup-project server/src/MoniPay.Api
./scripts/update-openapi.sh                         # regenerate docs/api/openapi.json
./scripts/check-openapi-sync.sh                     # fail if the committed document is stale
```

Local secrets never live in a file that git can see:

```bash
dotnet user-secrets set "ConnectionStrings:MoniPay" "Host=localhost;…" \
  --project server/src/MoniPay.Api
```

Elsewhere the same values come from environment variables, with `__` between the parts:
`MoniPay__Sessions__SigningKeyBase64`.

## Versioning & Releases

Each component versions on its own, from git tags:

| Component | Tag prefix | Version source | Changelog |
|---|---|---|---|
| API | `monipay.api-v*` | MinVer (`MinVerTagPrefix` in `server/Directory.Build.props`) | `server/CHANGELOG.md` |
| iOS app | `monipay.ios-v*` | the `monipay-ios-version` workflow | `ios/CHANGELOG.md` |

- Tagging the API bumps only the API; an iOS tag never rebuilds the server, and vice versa. See
  [monorepo-versioning-tags](agents/rules/monorepo-versioning-tags.md).
- Changelogs are generated by **git-cliff** from conventional commits, one `cliff.toml` per
  component. The header of each `CHANGELOG.md` is pinned — never move it by hand, never write an
  entry by hand. See [monorepo-changelog-git-cliff](agents/rules/monorepo-changelog-git-cliff.md).
- Conventional commits are enforced twice: the husky `commit-msg` hook locally, and the
  `pr-title` workflow on the PR title (a squash merge makes the PR title the commit on `main`).
  Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`,
  `revert`. Scopes: `api`, `ios`, `mobile`, `build`, `ci`, `docs`, `release`. See
  [monorepo-conventional-commits-husky](agents/rules/monorepo-conventional-commits-husky.md).
- Write PR titles as user-facing changelog lines — they end up verbatim in `CHANGELOG.md`.

## Internationalization

The product speaks French to its users and English to its engineers. That split is mechanical,
not a matter of taste: source strings are English keys, translations are data, and no French
literal ever appears in code. See [quality-localization](agents/rules/quality-localization.md)
and [api-localization](agents/rules/api-localization.md).

### iOS

- **String Catalogs per package.** Every package that owns UI copy carries its own
  `Sources/<Name>/Resources/Localizable.xcstrings`, declares `defaultLocalization: "en"` in
  `Package.swift`, and the app declares `developmentLanguage: en` in `ios/project.yml`. A
  package's strings resolve against its own bundle — pass `bundle: .module` when you build a
  `String(localized:)` inside a package.
- **English source, French translation.** The key *is* the English text
  (`String(localized: "Available balance", bundle: .module)`); the French lives in the catalog
  next to it. Adding a screen means adding English strings and their French translations in the
  same PR — an untranslated key ships as English and is a bug.
- **Never build a sentence by concatenation.** Use one key with interpolation so the translation
  can reorder it: `"Top up \(amount)"`, not `"Top up " + amount`. Give plurals a plural variation
  in the catalog rather than an `if count == 1`.
- **Format with `FormatStyle` and the user's `Locale`, never by hand.** Money, dates and numbers
  are formatted at the edge:

  ```swift
  // The currency code carries the fraction digits: XAF has none, USD has two.
  amountXAF.formatted(.currency(code: "XAF").locale(.current))
  date.formatted(.dateTime.day().month().year())
  ```

  No manual `String(format:)` for an amount, no hardcoded thousands separator, no hardcoded
  `"FCFA"` suffix.
- **DesignSystem components accept localized text, they never contain copy.** A component takes a
  `LocalizedStringKey` or an already-localized `String` from its caller and stays copy-free —
  which is also why the catalog for a screen's words lives in the feature package, not in
  `DesignSystem`. The only strings `DesignSystem` owns are its own accessibility fallbacks.

### Backend

- **Negotiate the culture from `Accept-Language`.** `MoniPay.Api` configures ASP.NET Core
  `RequestLocalization` with supported cultures `fr-CM`, `fr` and `en`, defaulting to **`fr`** —
  a client that sends nothing gets French, because that is who the product serves.
- **`IStringLocalizer` + `.resx` per module.** Each `MoniPay.<Module>` owns the resources for the
  messages it produces; the host owns only its own. Never centralize every string in `Api`, for
  the same reason endpoints are not centralized there.
- **Contracts carry raw values; clients format them.** A response returns an amount **plus its
  currency code** in minor units, timestamps as **ISO-8601 UTC**, and states as stable machine
  codes (`"DECLINE_INSUFFICIENT_FUNDS"`) — never a pre-formatted `"15 700 FCFA"` or a localized
  date. The API is consumed by an app that knows the user's locale; the server does not.
- **Errors carry both.** A stable `code` the client can switch on, and a localized `message` for
  the rare case where there is nothing better to show. The client prefers the code.
- Log messages and OpenAPI descriptions are English, always — they are for engineers.

## iOS Architecture

Package boundaries **are** the architecture: the compiler enforces the layering, so a violation
is a build error rather than a code-review comment.

- **`Platform`** — process-level capabilities behind protocols (`Clock`, `Logging`,
  `KeyValueStoring`) with fakes in `PlatformTestSupport` (`FixedClock`,
  `InMemoryKeyValueStoring`). Nothing depends on a concrete `Date()` or `UserDefaults`.
- **`DesignSystem`** — the component library: semantic tokens, foundations (theme, surfaces,
  press styles) and one file per component, grouped by family. It is the only place that knows
  what a button looks like. Features compose it; they never restyle it.
- **`Money`** — money arithmetic and `Sendable` domain models. No SwiftUI, no networking, no
  user-facing copy.
- **`ApiClient`** — the only package that knows the backend exists. Today it targets the POC
  server (`POST /signup`, `POST /topup`, `POST /card`, `POST /simtopup`, `GET /user`) with DTOs
  mirroring `poc/README.md`; once `server/` ships, its types come from `docs/api/openapi.json`.
  Provider quirks (Campay caps, Sudo funding sources, `data`-wrapped bodies) are mapped to domain
  errors here and never leak upward.
- **`WalletStore`** — the `Wallet` actor that owns the balance, the holds and the just-in-time
  authorization decision, plus `Store`, the `@Observable` app state injected with
  `.environment(store)`, and `SampleData` for previews and tests.
- **Feature packages** — one per user-facing area, each self-contained: a single public
  `<Feature>Root`, internal `Views/`, `@Observable` state in `Models/`, and multi-step
  coordinators in `Flows/`. They never import each other. See
  [architecture-feature-boundaries](agents/rules/architecture-feature-boundaries.md).
- **`App`** — composition root: builds the dependency graph, injects it into the environment,
  and routes between phases. No business logic, no networking.

## Code Examples (iOS)

### Add a feature = add a package

A new feature is never a folder inside an existing package. Create
`ios/Packages/<Feature>/`:

```
ios/Packages/Referral/
  Package.swift
  Sources/Referral/ReferralRoot.swift              # the one public entry view
  Sources/Referral/Views/ReferralInviteView.swift
  Sources/Referral/Models/ReferralModel.swift
  Sources/Referral/Flows/ReferralFlow.swift
  Sources/Referral/Resources/Localizable.xcstrings
  Tests/ReferralTests/Models/ReferralModelTests.swift
```

```swift
// ios/Packages/Referral/Package.swift
// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "Referral",
    defaultLocalization: "fr",
    platforms: [.iOS(.v26)],
    products: [
        .library(name: "Referral", targets: ["Referral"]),
    ],
    dependencies: [
        .package(path: "../DesignSystem"),
        .package(path: "../Money"),
        .package(path: "../WalletStore"),
    ],
    targets: [
        .target(
            name: "Referral",
            dependencies: [
                .product(name: "DesignSystem", package: "DesignSystem"),
                .product(name: "Money", package: "Money"),
                .product(name: "WalletStore", package: "WalletStore"),
            ],
            resources: [.process("Resources/Localizable.xcstrings")]
        ),
        .testTarget(name: "ReferralTests", dependencies: ["Referral"]),
    ]
)
```

Then register it in `ios/project.yml` — once under `packages:` and once as a dependency of the
`MoniPay` target:

```yaml
packages:
  Referral:
    path: Packages/Referral

targets:
  MoniPay:
    dependencies:
      - package: Referral
```

Finally run `xcodegen generate` from `ios/` and wire the entry point in `ios/App/`. Test the
package on its own from `Packages/Referral` with
`xcodebuild -scheme Referral -destination 'platform=iOS Simulator,name=iPhone 17 Pro' test` —
it imports `DesignSystem`, so the macOS-hosted `swift test` cannot build it.

### Add a domain model

Domain types are `Sendable` value types with no SwiftUI import and no user-facing copy.

```swift
// ios/Packages/Money/Sources/Money/TopUpRequest.swift
/// A MoMo collection request. FCFA has no minor unit, so `amountXAF` is whole francs.
struct TopUpRequest: Sendable, Hashable {
    let amountXAF: Int
    let method: TopUpMethod.ID
    let phone: PhoneNumber

    init?(amountXAF: Int, method: TopUpMethod.ID, phone: PhoneNumber) {
        guard amountXAF > 0 else { return nil }   // an invalid amount cannot be constructed
        self.amountXAF = amountXAF
        self.method = method
        self.phone = phone
    }
}

enum TopUpError: Error, Sendable {
    case belowMinimum(minimumXAF: Int)
    case providerDeclined(reason: String)
    case timedOut
}
```

### Add a repository

The protocol and its live implementation live in `ApiClient`; features depend on the protocol
only, and previews and tests use an in-memory double. See
[data-repository-pattern](agents/rules/data-repository-pattern.md).

```swift
// ios/Packages/ApiClient/Sources/ApiClient/TopUpCollecting.swift
import Money

public protocol TopUpCollecting: Sendable {
    func collect(_ request: TopUpRequest) async throws(TopUpError) -> TopUpReceipt
}

/// Talks to poc/server.js (`POST /topup`). Swapping in the production issuer must not
/// require touching a single view.
public struct HTTPTopUpClient: TopUpCollecting {
    let baseURL: URL          // from AppConfiguration / APIBaseURL
    let session: URLSession

    public func collect(_ request: TopUpRequest) async throws(TopUpError) -> TopUpReceipt {
        // encode, POST, decode; map transport and provider failures onto TopUpError
    }
}
```

### Add a feature screen

```swift
// ios/Packages/TopUp/Sources/TopUp/Views/TopUpAmountView.swift
import DesignSystem
import Money
import SwiftUI
import WalletStore

// internal: only TopUpRoot is public
struct TopUpAmountView: View {
    @Environment(Store.self) private var store
    @State private var amountXAF = 0

    var body: some View {
        VStack(spacing: .spacingLarge) {
            MoneyText(amountXAF, currency: .xaf, size: .display)   // DesignSystem
            Keypad(value: $amountXAF)                              // DesignSystem
            MPButton("Continue", tone: .primary) { /* advance the flow */ }   // English source key
                .disabled(amountXAF < TopUpRules.minimumXAF)
        }
        .padding(.spacingMedium)
        .background(Color.surface)      // semantic token, never a literal
    }
}

#Preview {
    TopUpAmountView().environment(Store())
}
```

Rules of thumb: one decision per screen; state owned by the smallest view that needs it; every
visual element comes from `DesignSystem`; semantic tokens instead of literals; no `URLSession`
inside a view.

### Add a design-system component

A component earns its place when a second feature needs it. Add it under the right family, one
file, with variants as an enum and a preview covering them:

```swift
// ios/Packages/DesignSystem/Sources/DesignSystem/Components/Feedback/StatusPill.swift
import SwiftUI

/// A short status label — use it for a transaction or card state.
/// For an interactive filter, use `Chip` instead.
public struct StatusPill: View {
    public enum Tone: Sendable { case success, pending, failure, neutral }

    private let title: String
    private let tone: Tone

    public init(_ title: String, tone: Tone) {
        self.title = title
        self.tone = tone
    }

    public var body: some View {
        Text(title)
            .font(.labelSmall)
            .padding(.horizontal, .spacingSmall)
            .padding(.vertical, .spacingXSmall)
            .background(tone.background, in: .capsule)
            .foregroundStyle(tone.foreground)
            .accessibilityLabel(title)
            .accessibilityAddTraits(.isStaticText)
    }
}

#Preview("StatusPill — all tones") {
    HStack(spacing: .spacingSmall) {
        StatusPill("Approved", tone: .success)      // source keys are English;
        StatusPill("Pending", tone: .pending)       // the French copy lives in
        StatusPill("Declined", tone: .failure)      // Localizable.xcstrings
        StatusPill("Refunded", tone: .neutral)
    }
    .padding()
}
```

Then add it to the matching `Gallery` catalog screen so it shows up in visual review.

### Add a test

Tests live with the code they cover: package logic in `ios/Packages/<Name>/Tests/<Name>Tests/`,
composition-root behaviour in `ios/Tests/`.

```swift
// ios/Packages/WalletStore/Tests/WalletStoreTests/WalletTests.swift
import Money
import Testing
@testable import WalletStore

@Test("A replayed authorization does not double-hold funds")
func authorizationIsIdempotent() async {
    let wallet = Wallet(ownerId: "u1", balanceXAF: 100_000)

    let first = await wallet.authorize(authId: "auth-1", amountUSDCents: 1_000, spendLimitUSDCents: nil)
    let replay = await wallet.authorize(authId: "auth-1", amountUSDCents: 1_000, spendLimitUSDCents: nil)

    #expect(first == .approved)
    #expect(replay == .approved)
    #expect(await wallet.availableXAF == 100_000 - FXRate().xaf(fromUSDCents: 1_000))
}
```

## PR Checklist

- [ ] Title follows conventional commits: `feat(cards): ...`
- [ ] `xcodegen generate` run (from `ios/`) if a package or app-target file was added
- [ ] No `.xcodeproj` file in the diff — it is git-ignored
- [ ] **iOS zero-warnings gate**: builds for the simulator with **no warnings at all** (`SWIFT_TREAT_WARNINGS_AS_ERRORS`) and `swiftlint --strict` passes
- [ ] Package tests pass (`swift test --package-path` for `Platform` / `ApiClient`, `xcodebuild test -scheme <Name>` from `Packages/<Name>` for the rest), plus the app scheme if the composition root changed
- [ ] No new sideways dependency between feature packages or between `MoniPay.<Module>` projects
- [ ] No component re-created in a feature, no inline styling, no colour / spacing / font literal outside `DesignSystem/Tokens/`
- [ ] A new or changed component has variants as an enum, an accessibility label, a `#Preview` of all variants, and a `Gallery` entry
- [ ] Only `<Feature>Root` is public in a feature package
- [ ] **Backend zero-warnings gate**: `dotnet build` clean (`TreatWarningsAsErrors`), `dotnet format --verify-no-changes` clean, `dotnet test` green
- [ ] `docs/api/openapi.json` regenerated and committed if an endpoint or contract changed
- [ ] EF Core migration added and its SQL reviewed, if the schema changed
- [ ] No floating-point type introduced for a monetary amount
- [ ] No force unwrap / force try / force cast (Swift), no `null!`, `!` or `.Result` (C#)
- [ ] No suppression anywhere: no `swiftlint:disable`, `#pragma warning disable`, `[SuppressMessage]`, `<NoWarn>`, raised threshold, lenient flag or `--no-verify` (see [quality-zero-warnings](agents/rules/quality-zero-warnings.md))
- [ ] No function pushed past the complexity threshold — the fix is a smaller function, not a bigger limit (see [quality-cyclomatic-complexity](agents/rules/quality-cyclomatic-complexity.md))
- [ ] Diff is small and focused (<500 lines, <10 code files), and touches one component
- [ ] No secrets, connection strings, PAN, CVV or OTP committed or logged
- [ ] No French outside the localization catalogs; new UI copy added as an English source key with its French translation
- [ ] Opened as a draft PR

## When Stuck

- Ask a clarifying question before a large speculative change
- Propose a short plan for anything touching the wallet ledger, the FX math or the API contract
- Fix compiler, concurrency and analyzer errors before test failures — they are usually the root cause
- Re-read `poc/README.md` before guessing at a backend contract; the provider quirks are documented there
- Check `docs/handoff/` and `docs/adr/` for decisions already made — do not relitigate them
- Invoke the `swiftui-skills` skill and read the relevant `docs/*.md` before reaching for an
  iOS 26 API — see [Apple API source of truth](#apple-api-source-of-truth)

## Extended Documentation

- **[agents/README.md](agents/README.md)** — rules index and architecture overview
- **[agents/rules/](agents/rules/)** — modular engineering rules, grouped by section prefix:
  `architecture-*`, `quality-*`, `data-*`, `api-*`, `performance-*`, `testing-*`, `patterns-*`,
  `culture-*`, `ci-*`, `monorepo-*`, `reference-*`. Rules are per stack; the filename says which
  (e.g. `architecture-dotnet-modular-monolith`, `testing-swift-testing`).
- **[agents/commands.md](agents/commands.md)** — complete command reference
- **[agents/knowledge-base.md](agents/knowledge-base.md)** — domain knowledge and business rules
- **`docs/`** — project history and decisions: `adr/` (architecture decision records),
  `handoff/` (session handoffs), `architecture/`, `api/` (the generated OpenAPI document),
  `contracts/`, `design/`, `ops/`

### Rules for the backend and the monorepo

- [architecture-dotnet-modular-monolith](agents/rules/architecture-dotnet-modular-monolith.md) — module boundaries and registration
- [api-minimal-endpoints](agents/rules/api-minimal-endpoints.md) — thin minimal-API endpoints
- [data-efcore-migrations](agents/rules/data-efcore-migrations.md) — schema changes and migrations
- [quality-csharp-style](agents/rules/quality-csharp-style.md) — nullable, warnings as errors, style
- [testing-xunit-testcontainers](agents/rules/testing-xunit-testcontainers.md) — xunit.v3 and Testcontainers
- [ci-dotnet-pipeline](agents/rules/ci-dotnet-pipeline.md) — the API build and release pipeline
- [monorepo-layout](agents/rules/monorepo-layout.md) — where things live and why
- [monorepo-versioning-tags](agents/rules/monorepo-versioning-tags.md) — per-component tags and MinVer
- [monorepo-changelog-git-cliff](agents/rules/monorepo-changelog-git-cliff.md) — generated changelogs
- [monorepo-conventional-commits-husky](agents/rules/monorepo-conventional-commits-husky.md) — commit and PR-title conventions
- [monorepo-ci-per-component](agents/rules/monorepo-ci-per-component.md) — path-scoped workflows
- [monorepo-openapi-contract](agents/rules/monorepo-openapi-contract.md) — the shared API contract
- [monorepo-package-management](agents/rules/monorepo-package-management.md) — CPM, SwiftPM pinning, tool manifest, Homebrew
