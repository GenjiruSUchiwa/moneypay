# Build, Test & Development Commands

This is a monorepo. Where a command runs matters:

| Component | Working directory |
|---|---|
| iOS (`xcodebuild`, `swift`, `swiftlint`, `xcodegen`) | `ios/` |
| Backend (`dotnet`) | repository root, with an explicit `server/…` path |
| POC backend, prototype, scripts, git | repository root |

## iOS: project generation (XcodeGen)

`ios/MoniPay.xcodeproj` is **generated** from `ios/project.yml` and **git-ignored**. Never edit
it, never commit it. Regenerate it after adding a package or a file the app target references.

```sh
cd ios
xcodegen generate                                  # regenerate MoniPay.xcodeproj
xcodegen generate --spec project.yml --project .   # same, explicit paths
xcodegen dump --type json                          # inspect the resolved spec
```

Install if missing: `brew install xcodegen` (or `mint install yonaskolb/XcodeGen`).

A new `.swift` file inside an existing package is picked up by SwiftPM automatically. A new
**package** must be declared twice in `ios/project.yml` — under `packages:` and as a dependency
of the `MoniPay` target — and then regenerated.

## iOS: build

From `ios/`:

- `xcodebuild -scheme MoniPay -destination 'platform=iOS Simulator,name=iPhone 17 Pro' build` — build for the simulator
- `xcodebuild -scheme MoniPay -destination 'generic/platform=iOS' build` — build without a concrete simulator
- `xcodebuild -scheme MoniPay -configuration Release -destination 'generic/platform=iOS' build` — release build
- `xcodebuild -scheme MoniPay clean` — clean build products
- `xcodebuild -list -project MoniPay.xcodeproj` — list schemes, targets and configurations
- `swift build --package-path Packages/<Name>` — compile one package without Xcode (fastest check; `Platform` and `ApiClient` only — every other package imports `DesignSystem`, which imports UIKit, and the host toolchain builds for macOS)
- `cd Packages/<Name> && xcodebuild -scheme <Name> -destination 'platform=iOS Simulator,name=iPhone 17 Pro' build` — compile one UIKit-dependent package on its own

Add `-quiet` to keep the output readable, and pipe through `xcbeautify` if it is installed.

## iOS: simulators

- `xcrun simctl list devices available` — list bootable simulators
- `xcrun simctl list devices available | grep iPhone` — iPhone runtimes only
- `xcrun simctl boot 'iPhone 17 Pro'` — boot a simulator
- `xcrun simctl shutdown all` — shut everything down

Destination syntax used everywhere: `-destination 'platform=iOS Simulator,name=iPhone 17 Pro'`.
Pin the OS when several runtimes are installed: `...,name=iPhone 17 Pro,OS=26.0`.

The app supports a screenshot shortcut — launch it with `-screen <key>` to open a single screen
from the `Gallery` package's screen catalog:

```sh
xcrun simctl launch --console booted com.monipay.app -screen home
```

## iOS: test

Tests use **Swift Testing** (`@Test` / `#expect`). Package logic is tested **inside its own
package** (`ios/Packages/<Name>/Tests/<Name>Tests/`); `ios/Tests/` holds app-target tests for
the composition root only.

### Per package — the normal loop

`swift test` runs on the macOS host, where UIKit does not exist. It therefore works only for the
two UIKit-free packages; everything that imports `DesignSystem` (that is every other package,
`Money` included) is tested through its own scheme on the simulator, run from the package directory —
the same command CI's *Test packages* step uses.

- `swift test --package-path Packages/Platform` — a UIKit-free package (`Platform`, `ApiClient`)
- `swift test --package-path Packages/Platform --filter PlatformTests` — one suite
- `cd Packages/WalletStore && xcodebuild -scheme WalletStore -destination 'platform=iOS Simulator,name=iPhone 17 Pro' test` — one UIKit-dependent package
- `xcodebuild ... test -only-testing:WalletStoreTests/WalletTests` — one suite
- `xcodebuild ... test -only-testing:WalletStoreTests/WalletTests/authorizationIsIdempotent` — one test

### Whole app, on the simulator

- `xcodebuild -scheme MoniPay -destination 'platform=iOS Simulator,name=iPhone 17 Pro' test` — app target + every package suite
- `xcodebuild ... test -only-testing:MoniPayTests/RootViewTests` — one app-target suite
- `xcodebuild ... test -only-testing:WalletStoreTests` — one package suite through the scheme
- `xcodebuild ... test-without-building` — reuse the last build
- `xcodebuild ... test -resultBundlePath /tmp/MoniPay.xcresult` — keep a result bundle for inspection

Use the package's own tests while iterating and the app scheme before pushing: only the app scheme run covers the
composition root and the simulator runtime.

## iOS: lint

SwiftLint is installed at `/opt/homebrew/bin/swiftlint`. **swiftformat is not installed** — do
not invoke it.

Run these from `ios/`:

- `swiftlint` — lint everything the config includes
- `swiftlint lint --strict` — treat warnings as errors (what CI does)
- `swiftlint --fix` — apply autocorrectable fixes, then re-run `swiftlint` to see what remains
- `swiftlint lint --path Packages/TopUp/Sources` — lint one package
- `swiftlint rules` — list available rules and their status

Configuration lives in `ios/.swiftlint.yml`, which includes `App`, `Tests`, `Packages/*/Sources`
and `Packages/*/Tests`. Linting is deliberately **not** a build phase, so a machine without
SwiftLint can still build; the `monipay-ios-ci` workflow runs
`swiftlint lint --strict --reporter github-actions-logging`, then `xcodegen generate`,
`xcodebuild build` and `xcodebuild test` on macOS 26.

The husky `pre-commit` hook also runs `swiftlint lint` on staged Swift files, so a lint failure
stops the commit rather than the pipeline.

## Backend (`server/`)

Run every `dotnet` command from the **repository root**, with an explicit `server/…` path.
Prerequisites: .NET SDK 10, PostgreSQL for a local run, Docker for the test suite.

### Build and run

- `dotnet restore server/MoniPay.slnx` — restore; also installs the git hooks
- `dotnet build server/MoniPay.slnx --configuration Release` — build (warnings are errors)
- `dotnet run --project server/src/MoniPay.Api` — run the API locally
- `dotnet build server/src/MoniPay.Api -p:GenerateOpenApiDocs=true` — emit the OpenAPI document
- `docker build -t monipay-api server && docker run -p 8080:8080 monipay-api` — run the container

### Test

The suite drives the real host over HTTP against a real database; Testcontainers starts one
PostgreSQL container for the whole run, so **a Docker daemon must be running**.
`server/dotnet.config` selects the Microsoft.Testing.Platform runner — without it `dotnet test`
finds no test and still reports success.

- `dotnet test server/MoniPay.slnx` — the whole suite
- `dotnet test server/MoniPay.slnx --filter "FullyQualifiedName~WalletTests"` — one class
- `dotnet test server/MoniPay.slnx --filter "FullyQualifiedName~WalletTests.HoldReleasesOnVoid"` — one test
- `dotnet test server/MoniPay.slnx --no-build` — reuse the last build

### Format and analyze

- `dotnet format server/MoniPay.slnx` — apply the `.editorconfig` style
- `dotnet format server/MoniPay.slnx --verify-no-changes` — fail instead of rewriting (what CI and the pre-commit hook do)
- `dotnet format server/MoniPay.slnx --include <paths>` — format specific files

Never silence a warning: `TreatWarningsAsErrors` is on by design.

### Database and migrations

- `dotnet ef migrations add <Name> --project server/src/MoniPay.Data --startup-project server/src/MoniPay.Api` — add a migration
- `dotnet ef migrations list --project server/src/MoniPay.Data --startup-project server/src/MoniPay.Api` — list them
- `dotnet ef migrations script --idempotent --project server/src/MoniPay.Data --startup-project server/src/MoniPay.Api` — review the SQL before applying
- `dotnet ef database update --project server/src/MoniPay.Data --startup-project server/src/MoniPay.Api` — apply locally

### Secrets

Local secrets go to the .NET secret manager, never to a file git can see:

```sh
dotnet user-secrets set "ConnectionStrings:MoniPay" \
  "Host=localhost;Port=5432;Database=monipay;Username=monipay;Password=<local password>" \
  --project server/src/MoniPay.Api
dotnet user-secrets list --project server/src/MoniPay.Api
```

On a host without a secret store, pass the same values as environment variables with `__`
between the parts: `MoniPay__Sessions__SigningKeyBase64`.

### Packages

Versions are central: `server/Directory.Packages.props` holds every version and a `.csproj`
names the package without one.

- `dotnet add server/src/MoniPay.Api package <Name>` — then move the version into `Directory.Packages.props`
- `dotnet list server/MoniPay.slnx package --outdated` — check for updates
- `dotnet tool restore` — install the local tools from `dotnet-tools.json`

## OpenAPI contract

The generated document in `docs/api/openapi.json` is the contract between `server/` and the iOS
`ApiClient`. It is generated, never hand-edited.

- `./scripts/update-openapi.sh` — regenerate the document and every client copy
- `./scripts/check-openapi-sync.sh` — fail if the committed document is stale (CI runs this)

## Git hooks (husky)

Hooks live in `.husky/` and are installed by the first `dotnet restore`, so a fresh clone
enforces the conventions with no setup step.

- `dotnet tool restore && dotnet husky install` — install the hooks by hand
- `dotnet husky run --group pre-commit` — run the pre-commit tasks now
- `HUSKY=0 dotnet build …` — disable the bootstrap (for CI and container builds only)

What they do: `commit-msg` rejects a message that is not a conventional commit;
`pre-commit` runs `dotnet format --verify-no-changes` on staged C# and `swiftlint lint` on
staged Swift. Never bypass them with `git commit --no-verify`.

## Changelogs (git-cliff)

One changelog per component, generated from conventional commits. The header of each
`CHANGELOG.md` is pinned and must stay byte-identical — never hand-write an entry.

- `git-cliff --config server/cliff.toml --output server/CHANGELOG.md` — regenerate the API changelog
- `git-cliff --config ios/cliff.toml --output ios/CHANGELOG.md` — regenerate the iOS changelog
- `git-cliff --unreleased` — preview what the next release would contain

Install if missing: `brew install git-cliff`.

## POC backend

From the **repository root**. The Node backend that the app talks to in live mode. Zero
dependency, Node 18+. Full contract, endpoints and provider limits: **`poc/README.md`** (source
of truth). The app reads its base URL from `API_BASE_URL` in `ios/Config/Debug.xcconfig`, exposed
as the Info.plist key `APIBaseURL`.

- `node poc/server.js` — start the server on `:8743`
- `node poc/server.js --demo` — self-checking mock run of the whole flow (no network, no keys)
- `node --check poc/server.js` — syntax check before running
- `lsof -ti :8743 | xargs kill` — free the port if a stale process holds it

Endpoints: `POST /signup`, `POST /topup`, `POST /simtopup`, `POST /card`, `GET /user?id=<id>`.

Provider keys go in `poc/.env` (git-ignored — never commit it, never print its values):
`SUDO_API_KEY`, `CAMPAY_APP_USERNAME`, `CAMPAY_APP_PASSWORD`. Without keys each provider falls
back to mock mode independently.

## HTML prototype

The visual reference for the SwiftUI implementation. Do not change it while doing Swift work.

- `python3 build.py` — from `prototype/`, assemble `dist/index.html` (run after every edit)
- `python3 -m http.server 8742` — from `prototype/`, serve the prototype on `:8742`
- `node --check app.js` — from `prototype/`, syntax check

## Git conventions

- **Conventional commits**, enforced by the `commit-msg` hook:
  `<type>[(<scope>)][!]: <description>`, 1–72 characters.
  Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`,
  `revert`. Scopes: `api`, `ios`, `mobile`, `build`, `ci`, `docs`, `release`.
  Example: `feat(api): collect a MoMo top-up (#123)`.
- A squash merge makes the **PR title** the commit on `main` and the changelog line, so the same
  convention is checked on PR titles by the `pr-title` workflow. Write the title as a
  user-facing changelog entry; keep issue references in the body (`Closes #N`).
- **Everything is English**: commit messages, PR titles, branch names, code, comments and
  documentation. French exists only as translated user-facing copy in the localization catalogs.
  See [rules/quality-english-only-code.md](rules/quality-english-only-code.md).
- Branch off `main`: `git checkout -b feat/topup-live`
- Never force push or rebase `main`
- Create pull requests in **draft**: `gh pr create --draft --title "feat(api): ..." --body "..."`
- Keep PRs under 500 changed lines and 10 code files, and touching one component (see `../AGENTS.md`)
- Never bypass a hook with `--no-verify`
- Never commit `poc/.env`, connection strings, API keys, or captured card data
- Never commit `ios/MoniPay.xcodeproj` or `docs/api/openapi.json` edits by hand — the first is
  git-ignored, the second is generated; if the `.xcodeproj` shows up in `git status`, the ignore
  rule was broken, do not `git add -f` it
- Release tags are per component: `monipay.api-v*` and `monipay.ios-v*`

Useful checks before pushing:

```sh
git status --short          # from the repo root — no .xcodeproj should appear
git diff --stat

# backend change
dotnet format server/MoniPay.slnx --verify-no-changes
dotnet build server/MoniPay.slnx --configuration Release
dotnet test server/MoniPay.slnx
./scripts/check-openapi-sync.sh

# iOS change
cd ios
swiftlint lint --strict
swift test --package-path Packages/<Name>          # Platform / ApiClient
(cd Packages/<Name> && xcodebuild -scheme <Name> \
  -destination 'platform=iOS Simulator,name=iPhone 17 Pro' test)   # any package importing DesignSystem
xcodebuild -scheme MoniPay \
  -destination 'platform=iOS Simulator,name=iPhone 17 Pro' test -quiet
```

## CI workflows

One CI and release workflow per component, plus a shared PR-title check. A workflow only runs
for paths belonging to its component.

- `monipay-api-ci` — `dotnet format --verify-no-changes`, build, test, OpenAPI sync check
- `monipay-api-release` — tag `monipay.api-v*`, MinVer version, git-cliff changelog
- `monipay-ios-ci` — swiftlint, `xcodegen generate`, build and test on macOS 26
- `monipay-ios-release` / `monipay-ios-version` — tag `monipay.ios-v*`, version stamping
- `pr-title` — the conventional-commit check on the PR title

Inspect a run with `gh run list --workflow monipay-api-ci` and `gh run view <id> --log-failed`.

## Skills

Some tooling ships as an agent skill rather than a shell command. Invoke it with the Skill tool,
not with `bash`.

### swiftui-skills — Apple's Xcode 26 documentation

**Invoke this before writing or reviewing SwiftUI / iOS 26 code.** It carries Apple-authored
AdditionalDocumentation extracted from Xcode 26 — Liquid Glass (SwiftUI, UIKit, AppKit,
WidgetKit), Swift 6.2 concurrency, the new toolbar API, styled text editing, SwiftData class
inheritance, `AttributedString`, Foundation Models, Swift Charts 3-D, `InlineArray` / `Span`,
AppIntents, StoreKit, WebKit, AlarmKit, MapKit place descriptors and more. Ground every API you
use in those files; do not write a signature from memory.

```sh
ls ~/.claude/skills/swiftui-skills/docs        # the 20 documents
~/.claude/skills/swiftui-skills/setup.sh       # extract them from Xcode if docs/ is empty
```

If `docs/` holds no `.md` files, the extraction has not run: run `setup.sh` and do not give
SwiftUI guidance until it is populated. The per-document "use when" table lives in
`../AGENTS.md` under **Apple API source of truth**, and the rule in
[rules/reference-apple-docs-skill.md](rules/reference-apple-docs-skill.md).

## Documentation layout

- `docs/adr/` — architecture decision records
- `docs/handoff/` — session handoffs (the former root-level `HANDOFF*.md`)
- `docs/design/` — design system and visual history (the former root-level `DESIGN.md`)
- `docs/architecture/` — architecture notes
- `docs/api/openapi.json` — the generated API contract; `docs/contracts/` for the rest
- `docs/ops/` — operations and deployment notes
- `agents/` — this documentation set, for agents
