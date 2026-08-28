# MoniPay Agent Documentation Index

- **[../AGENTS.md](../AGENTS.md)** - Main guide (monorepo layout, iOS + .NET stack, commands, examples)
- **[commands.md](commands.md)** - Command reference (xcodegen, xcodebuild, swiftlint, dotnet, husky, git-cliff)
- **[knowledge-base.md](knowledge-base.md)** - Domain knowledge and business rules (FCFA wallet, MoMo top-ups, USD cards)
- **[rules/README.md](rules/README.md)** - How rules are written (`_sections.md`, `_template.md`)

## Rules Index (78 rules)

### Architecture

- [architecture-circular-dependencies](rules/architecture-circular-dependencies.md) - SwiftPM Forbids Cycles — Break Them by Moving Code Down (CRITICAL)
- [architecture-design-system-library](rules/architecture-design-system-library.md) - DesignSystem Is the Component Library (CRITICAL)
- [architecture-dotnet-modular-monolith](rules/architecture-dotnet-modular-monolith.md) - A Module Owns Its Whole Vertical Slice (CRITICAL)
- [architecture-dotnet-module-boundaries](rules/architecture-dotnet-module-boundaries.md) - Public Contract, Internal Everything Else (CRITICAL)
- [architecture-feature-boundaries](rules/architecture-feature-boundaries.md) - Feature Packages Never Depend on Each Other (CRITICAL)
- [architecture-feature-internal-layout](rules/architecture-feature-internal-layout.md) - Inside a Feature Package — Root, Views, Models, Flows, Resources (HIGH)
- [architecture-layering](rules/architecture-layering.md) - Package Layers and the Dependency Direction (CRITICAL)
- [architecture-main-actor-ui](rules/architecture-main-actor-ui.md) - UI and Observable State on @MainActor, Work in Actors (CRITICAL)
- [architecture-package-anatomy](rules/architecture-package-anatomy.md) - Anatomy of a Local SwiftPM Package (CRITICAL)
- [architecture-vertical-slices](rules/architecture-vertical-slices.md) - One Feature, One Local SwiftPM Package (CRITICAL)

### Code Quality

- [quality-code-comments](rules/quality-code-comments.md) - Comments Explain Why; Doc Comments Document the Contract (MEDIUM)
- [quality-csharp-style](rules/quality-csharp-style.md) - C# Style — Nullable On, Warnings Fatal, Sealed by Default (HIGH)
- [quality-cyclomatic-complexity](rules/quality-cyclomatic-complexity.md) - Keep Cyclomatic Complexity Under the Threshold (HIGH)
- [quality-english-only-code](rules/quality-english-only-code.md) - Code Is English; French Lives Only in the Catalog (CRITICAL)
- [quality-error-handling](rules/quality-error-handling.md) - Typed Throws and Domain Error Enums (CRITICAL)
- [quality-i18n-formatting](rules/quality-i18n-formatting.md) - Internationalize Formatting, Layout, and Component APIs (CRITICAL)
- [quality-imports](rules/quality-imports.md) - Import Only What the File Needs; Keep Package Boundaries Honest (MEDIUM)
- [quality-localization](rules/quality-localization.md) - Every User-Facing String Goes Through Its Package's String Catalog (CRITICAL)
- [quality-no-followup-prs](rules/quality-no-followup-prs.md) - Minimize Follow-up PRs for Small Refactors (HIGH)
- [quality-no-force-unwrap](rules/quality-no-force-unwrap.md) - Never Force-Unwrap, Force-Try, or Force-Cast (CRITICAL)
- [quality-no-inline-styling](rules/quality-no-inline-styling.md) - No Inline Styling — Use DesignSystem Tokens and Components (CRITICAL)
- [quality-optionals-and-types](rules/quality-optionals-and-types.md) - Make Illegal States Unrepresentable (CRITICAL)
- [quality-pr-creation](rules/quality-pr-creation.md) - PR Creation Best Practices (HIGH)
- [quality-review-checklist](rules/quality-review-checklist.md) - Swift Code Review Checklist (HIGH)
- [quality-secrets-and-config](rules/quality-secrets-and-config.md) - Secrets Live in User-Secrets and Environment Variables, Never in Git (CRITICAL)
- [quality-simplicity](rules/quality-simplicity.md) - Prefer the Simplest Swift That Solves Today's Problem (CRITICAL)
- [quality-swiftlint](rules/quality-swiftlint.md) - SwiftLint Must Pass Before You Claim Done (CRITICAL)
- [quality-thorough-code-review](rules/quality-thorough-code-review.md) - Address Every Nit Before Merging (HIGH)
- [quality-zero-warnings](rules/quality-zero-warnings.md) - Zero Warnings — Fix, Never Suppress (CRITICAL)

### Data Layer

- [data-dto-boundaries](rules/data-dto-boundaries.md) - Map DTOs to Domain Models at the Boundary (HIGH)
- [data-efcore-conventions](rules/data-efcore-conventions.md) - EF Core Conventions — One Context, Module-Owned Persistence (HIGH)
- [data-efcore-migrations](rules/data-efcore-migrations.md) - Migrations Are Reviewed SQL, Not Generated Noise (HIGH)
- [data-money-ledger](rules/data-money-ledger.md) - The Wallet Is an Append-Only Ledger in Minor Units (CRITICAL)
- [data-money-representation](rules/data-money-representation.md) - Money Is Never a Double (CRITICAL)
- [data-persistence](rules/data-persistence.md) - SwiftData for Cache, Keychain for Secrets, UserDefaults for Preferences (HIGH)
- [data-repository-methods](rules/data-repository-methods.md) - Repository Method Conventions (HIGH)
- [data-repository-pattern](rules/data-repository-pattern.md) - Isolate Providers Behind Protocol Repositories (HIGH)

### API Design

- [api-backend-contract](rules/api-backend-contract.md) - The POC Backend Contract Is the Source of Truth (HIGH)
- [api-localization](rules/api-localization.md) - Localize on the Server, Format on the Client (HIGH)
- [api-minimal-endpoints](rules/api-minimal-endpoints.md) - Endpoints Live in Their Module and Stay Thin (HIGH)
- [api-no-magic-strings](rules/api-no-magic-strings.md) - No Magic Strings in the HTTP Surface (HIGH)
- [api-openapi-contract](rules/api-openapi-contract.md) - The OpenAPI Document Is the Contract With iOS (CRITICAL)
- [api-thin-client](rules/api-thin-client.md) - The API Client Does Transport and Decoding — Nothing Else (HIGH)

### Performance

- [performance-avoid-quadratic](rules/performance-avoid-quadratic.md) - Avoid O(n²) — Reach for Set and Dictionary (CRITICAL)
- [performance-concurrency](rules/performance-concurrency.md) - Never Block the Main Actor; Use Structured Concurrency (HIGH)
- [performance-date-handling](rules/performance-date-handling.md) - Date Handling — Calendar, FormatStyle, and Africa/Douala (HIGH)
- [performance-swiftui-body](rules/performance-swiftui-body.md) - Keep SwiftUI `body` Cheap (HIGH)

### Testing

- [testing-coverage-requirements](rules/testing-coverage-requirements.md) - Test the Money, Not the Pixels (HIGH)
- [testing-dotnet-coverage](rules/testing-dotnet-coverage.md) - Test the Money, the Contract, and the Provider Edges (HIGH)
- [testing-incremental](rules/testing-incremental.md) - Fix Failing Tests One Package at a Time (MEDIUM)
- [testing-mocking](rules/testing-mocking.md) - Protocol-Based Fakes, No Mocking Framework (HIGH)
- [testing-swift-testing](rules/testing-swift-testing.md) - Use Swift Testing for Unit Tests, Not XCTest (HIGH)
- [testing-timezone-locale](rules/testing-timezone-locale.md) - Pin TimeZone and Locale in Tests — Never Read the Host's (HIGH)
- [testing-ui-tests](rules/testing-ui-tests.md) - XCUITest Sparingly, #Preview Constantly (MEDIUM)
- [testing-xunit-testcontainers](rules/testing-xunit-testcontainers.md) - One Test Project, a Real PostgreSQL, a Frozen Clock (HIGH)

### Design Patterns

- [patterns-async-flows](rules/patterns-async-flows.md) - Structured Concurrency for Async Flows (MEDIUM)
- [patterns-dependency-injection](rules/patterns-dependency-injection.md) - Inject Dependencies; Ship Doubles in TestSupport Targets (MEDIUM)
- [patterns-dotnet-dependency-injection](rules/patterns-dotnet-dependency-injection.md) - Constructor Injection, Options Pattern, Typed Clients (HIGH)
- [patterns-navigation](rules/patterns-navigation.md) - NavigationStack with Typed Routes and Explicit Flow State (MEDIUM)
- [patterns-observable-state](rules/patterns-observable-state.md) - State with @Observable, @State Ownership, and @Bindable (MEDIUM)
- [patterns-outbox-and-background-work](rules/patterns-outbox-and-background-work.md) - Outbox and Background Workers — Record First, Call the Provider Later (HIGH)

### Team Culture

- [culture-accountability](rules/culture-accountability.md) - Hold Each Other Accountable for Quality (MEDIUM)
- [culture-leverage-ai](rules/culture-leverage-ai.md) - Leverage AI — But Verify With the Toolchain Before Claiming Done (MEDIUM)

### CI/CD

- [ci-build-first](rules/ci-build-first.md) - Build the Package First, the App Second — Warnings Are Errors (HIGH)
- [ci-check-failures](rules/ci-check-failures.md) - Triage a Red CI by Reproducing It Locally, Exactly (HIGH)
- [ci-dotnet-pipeline](rules/ci-dotnet-pipeline.md) - The API Pipeline — Restore, Build, Contract, Format, Test (HIGH)
- [ci-git-workflow](rules/ci-git-workflow.md) - Git Workflow — Branches, Conventional Commits, and a Generated Project (HIGH)

### Monorepo

- [monorepo-changelog-git-cliff](rules/monorepo-changelog-git-cliff.md) - Changelogs Are Generated, Never Written (MEDIUM)
- [monorepo-ci-per-component](rules/monorepo-ci-per-component.md) - One Workflow Per Component, Never a Repository-Wide Build (MEDIUM)
- [monorepo-conventional-commits-husky](rules/monorepo-conventional-commits-husky.md) - Conventional Commits, Enforced Locally and on the PR Title (HIGH)
- [monorepo-layout](rules/monorepo-layout.md) - One Repository, Two Components, No Third Home (HIGH)
- [monorepo-openapi-contract](rules/monorepo-openapi-contract.md) - The OpenAPI Document Is the Contract Between server/ and ios/ (CRITICAL)
- [monorepo-package-management](rules/monorepo-package-management.md) - Every Dependency Version Is Declared Once, in One File Per Ecosystem (HIGH)
- [monorepo-versioning-tags](rules/monorepo-versioning-tags.md) - Each Component Versions From Its Own Tag Prefix (HIGH)

### Reference

- [reference-apple-docs-skill](rules/reference-apple-docs-skill.md) - Ground iOS 26 APIs in the swiftui-skills docs (HIGH)
- [reference-dotnet-local-dev](rules/reference-dotnet-local-dev.md) - Local Development — .NET 10, PostgreSQL, Docker (LOW)
- [reference-file-locations](rules/reference-file-locations.md) - Key File Locations (LOW)
- [reference-local-dev](rules/reference-local-dev.md) - Local Development Setup (LOW)
