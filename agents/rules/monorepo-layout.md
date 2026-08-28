---
title: One Repository, Two Components, No Third Home
impact: HIGH
impactDescription: A file in the wrong tree breaks the per-component CI, versioning and changelog at once
tags: monorepo, layout, structure, ios, server, organization
---

## One Repository, Two Components, No Third Home

**Impact: HIGH**

MoniPay is one repository with exactly two shipping components: the SwiftUI app in `ios/` and the
.NET modular monolith in `server/`. Everything else at the root is shared infrastructure —
documentation, hooks, scripts, workflows — and belongs to neither.

This is not filing preference. Four mechanisms key off the tree a file sits in:

| Mechanism | How it reads the tree |
|---|---|
| CI | `monipay-api-ci` builds `server/`; the iOS workflow builds `ios/` |
| Versioning | MinVer reads `monipay.api-v*`; the iOS workflow reads `monipay.ios-v*` |
| Changelog | `server/cliff.toml` excludes `ios/**`; `ios/cliff.toml` includes only `ios/**` |
| Hooks | `.husky/task-runner.json` runs `dotnet format` on `server/**/*.cs`, `swiftlint` on `ios/**/*.swift` |

A Swift file under `server/`, or a C# file under `ios/`, is invisible to all four.

```
monipay/
  ios/                     # SwiftUI app. project.yml (XcodeGen) is the source of truth
    project.yml            # the .xcodeproj is GENERATED and git-ignored
    App/                   # composition root only
    Config/                # xcconfig, entitlements, PrivacyInfo
    Packages/<Name>/       # one local SwiftPM package per feature or capability
    Tests/                 # app-target tests only
    cliff.toml             # changelog config for the ios component
    CHANGELOG.md           # generated, never hand-edited
  server/                  # .NET modular monolith
    MoniPay.slnx
    Directory.Build.props  # nullable, warnings as errors, MinVer, husky bootstrap
    Directory.Packages.props
    src/MAP.md             # the module map: who owns what, who may reference whom
    src/MoniPay.<Module>/  # one project per bounded context, owning its whole vertical slice:
                           #   <Module>Module.cs  Endpoints/  Contracts/  Domain/  Persistence/
    src/MoniPay.Api/       # the HTTP host ONLY: Program.cs, middleware, OpenAPI, health
    tests/MoniPay.Tests/
    cliff.toml
    CHANGELOG.md
  agents/                  # these rules, commands.md, knowledge-base.md
  docs/                    # see "Where documentation goes" below
  poc/                     # the Node POC — the reference contract until server/ ships
  prototype/               # the HTML design prototype
  scripts/                 # update-openapi.sh, check-openapi-sync.sh
  .husky/                  # commit-msg, pre-commit, task-runner.json
  .github/workflows/       # one workflow per component, named monipay-<component>-<job>
  CONTEXT.md               # the ubiquitous language, shared by both components
  AGENTS.md / CLAUDE.md    # byte-identical copies of the agent guide
```

**Incorrect (a shared thing given a component's home):**

```bash
# The domain glossary belongs to both components, so it cannot live inside one of them.
server/CONTEXT.md                      # ❌ invisible to anyone working in ios/
ios/Packages/ApiClient/openapi-tools/  # ❌ the contract pipeline is not an iOS concern

# A backend helper filed under the app tree: ios/cliff.toml will put it in the APP changelog.
ios/scripts/seed-database.sh           # ❌
```

**Correct (root for shared, component tree for component-owned):**

```bash
CONTEXT.md                             # ✅ one glossary, both components
scripts/update-openapi.sh              # ✅ crosses the boundary, so it sits above it
docs/api/openapi.json                  # ✅ the contract is shared, not owned by either side
server/scripts/seed-database.sh        # ✅ backend-only, so it lives in the backend tree
```

**Where does a new file go?** Ask which component breaks if it disappears:

- Only the app breaks → `ios/`.
- Only the API breaks → `server/`.
- Both break, or neither builds it → the root (`docs/`, `scripts/`, `agents/`, `.github/`).

**A change never spans both trees in one PR**, unless it is the two halves of one API contract
change — and even then the server half lands first. See
[monorepo-ci-per-component](monorepo-ci-per-component.md) and
[monorepo-openapi-contract](monorepo-openapi-contract.md).

Two directories are deliberately frozen: `poc/` (the Node backend that defines today's contract)
and `prototype/` (the HTML mockup). They are reference material, not components. Nothing in
`ios/` or `server/` may import from them, and they are excluded from both changelogs.

## Where documentation goes

`docs/` has one folder per kind of document, because "when may I delete this?" has a different
answer for each:

```
docs/
  adr/            # architecture decision records: why a choice was made. Append-only.
                  # A superseded ADR is marked superseded, never edited or deleted.
  architecture/   # how the system is shaped today. Rewritten as the system changes.
  api/            # openapi.json — GENERATED. Never hand-edited.
  design/         # the visual system: DESIGN.md, screen specs, design history.
  handoff/        # dated session handoffs. Historical record; never rewritten.
  ops/            # runbooks, deployment, incident procedure. Kept current or deleted.
```

Three of these are load-bearing and the rest are prose:

- **`docs/api/openapi.json` is generated.** Editing it is a lie that CI catches — see
  [monorepo-openapi-contract](monorepo-openapi-contract.md).
- **`docs/adr/` and `docs/handoff/` are append-only.** They record what was decided and what
  happened. Re-reading a handoff to learn a decision only works if nobody has tidied it. Before
  reopening a settled question, check both — do not relitigate what an ADR already answered.
- **`docs/architecture/` and `docs/ops/` describe the present.** A stale runbook is worse than a
  missing one, so an out-of-date file there gets fixed or removed, not left "for reference".

Two documents live at the root because they belong to the whole repository, not to `docs/`:
`CONTEXT.md` (the ubiquitous language, in French — the words the code must use) and
`AGENTS.md` / `CLAUDE.md` (byte-identical copies of the agent guide).

## MAP.md: what a directory owns, in a paragraph

Every module project carries a `MAP.md`, and `server/src/MAP.md` indexes them. It is plain
markdown — no tool reads it, nothing generates it, and it costs one paragraph per module.

It exists because a directory listing shows file names, not responsibilities. Given
`MoniPay.Wallet/` and `MoniPay.TopUps/`, nothing on disk says which one owns the decision to
accept an authorization. `MAP.md` answers that in the place you are already looking.

Each one states three things and stops:

```markdown
# MoniPay.Wallet

The FCFA ledger: the balance, the holds placed by accepted authorizations, and the
just-in-time decision to accept or refuse one. The wallet is the single source of truth
for what the user can spend — a card has no balance of its own.

- `Endpoints/WalletEndpoints.cs` — `MapWalletEndpoints`. `GET /wallet` today.
- `Domain/WalletBalance.cs` — balance minus held is available. An authorization compares
  against available, never against the balance.

References `Kernel`, `Data`.
```

1. **What this module owns** — the domain responsibility, in the words of `CONTEXT.md`.
2. **The files worth knowing about** — not a listing; the two or three that carry the decisions.
3. **What it references** — so a wrong dependency is obvious while reading, not at build time.

Keep it under thirty lines. A `MAP.md` that grows into a design document has stopped being a map,
and it goes stale the moment nobody reads it. Update it in the same commit that changes what the
module owns — never as a follow-up.

The convention stops at module boundaries: `Endpoints/`, `Contracts/`, `Domain/` and
`Persistence/` mean the same thing in every module, so they need no local explanation. The iOS
side gets the same treatment when a package's purpose is not obvious from its name.
