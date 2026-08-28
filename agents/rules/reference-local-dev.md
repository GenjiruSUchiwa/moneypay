---
title: Local Development Setup
impact: LOW
impactDescription: Reference guide for getting MoniPay building, testing and running locally
tags: reference, development, setup, xcodegen, swiftpm, simulator
---

# Local Development Setup

## Prerequisites

| Tool | Version | Install |
|---|---|---|
| Xcode | **26.6** (build 17F113) — Swift 6.3 toolchain | App Store / Apple Developer downloads |
| iOS Simulator runtime | **iOS 26.x** (26.4 is what CI uses) | Xcode → Settings → Components |
| XcodeGen | latest | `brew install xcodegen` |
| SwiftLint | latest | `brew install swiftlint` |
| Node | 18+ (POC backend only) | `brew install node` |

```sh
xcodebuild -version && swift --version && xcodegen --version && swiftlint version
xcrun simctl list runtimes | grep iOS    # doit contenir « iOS 26.x »
```

## Two loops: packages (fast) and app (slow)

Almost all MoniPay code lives in SwiftPM packages under `ios/Packages/`. **Work in the package
loop** — it needs no Xcode project, no simulator, and takes seconds. Reach for `xcodebuild` only
when you touch `ios/App/`, `ios/Config/`, `ios/project.yml`, or a SwiftUI screen you want to see.

### Package loop (the default)

```sh
cd ios
swift build --package-path Packages/Money                        # compiling IS the type check
swift test  --package-path Packages/Money                        # the package's tests
swift test  --package-path Packages/Money --filter FXRateTests   # a single suite
swift test  --package-path Packages/WalletStore                  # pulls its local deps
```

`swift build`/`swift test` resolve the local path dependencies declared in each `Package.swift`;
nothing else is needed. Artefacts land in `ios/Packages/<Name>/.build/`, which is gitignored.

### App loop

`ios/MoniPay.xcodeproj` is **generated and gitignored**: it does not exist in a fresh clone.
Generate it, and regenerate after every `project.yml` change, every new package, and every `git
pull` that touched either. Then select scheme **MoniPay**, destination **iPhone 17 Pro (iOS 26.4)**.

```sh
cd ios
xcodegen generate && open MoniPay.xcodeproj

# Build the app (aggregates every package)
xcodebuild build -project MoniPay.xcodeproj -scheme MoniPay \
  -destination 'platform=iOS Simulator,name=iPhone 17 Pro,OS=26.4' -quiet

# App-target tests (composition root, ios/Tests/)
xcodebuild test -project MoniPay.xcodeproj -scheme MoniPay \
  -destination 'platform=iOS Simulator,name=iPhone 17 Pro,OS=26.4'

# Lint: config lives in ios/.swiftlint.yml
swiftlint --strict
```

## Adding a package

1. `mkdir -p ios/Packages/<Name>/Sources/<Name> ios/Packages/<Name>/Tests/<Name>Tests`
2. Write `Package.swift` (`swift-tools-version: 6.2`, `platforms: [.iOS(.v26)]`), exporting
   `.library(name: "<Name>", targets: ["<Name>"])` and, when it publishes test doubles,
   `.library(name: "<Name>TestSupport", targets: ["<Name>TestSupport"])`.
3. Declare it in `ios/project.yml` under `packages:` **and** in the `MoniPay` target's
   `dependencies:`.
4. `cd ios && xcodegen generate`.

Respect the dependency direction in `reference-file-locations`: a feature package never imports
another feature package.

## Configuration

`API_BASE_URL` lives in `ios/Config/Debug.xcconfig` / `Release.xcconfig` and reaches the app as the
Info.plist key `APIBaseURL` (read by `App/AppConfiguration.swift`). Point Debug at the local POC
backend — note the `$()` splice, because `//` starts a comment in an xcconfig file:
`API_BASE_URL = http:/$()/localhost:8743`.

## Running the POC backend

Dependency-free, single file. MOCK mode is the default and needs no credentials.

```sh
node poc/server.js --demo   # self-checking demo of the full flow, in mock mode
node poc/server.js          # server on http://localhost:8743
```

The simulator reaches the host's `localhost` directly — no special address needed.

### Real sandbox mode

Create `poc/.env` (**never commit it**):

```
SUDO_API_KEY=...          # https://app.sudo.africa (sandbox) → Developers → API Keys
CAMPAY_APP_USERNAME=...   # https://demo.campay.net → inscription → app → identifiants
CAMPAY_APP_PASSWORD=...
```

Each provider switches from mock to live independently, as soon as its key is present.

**Sandbox limits to expect (they are not bugs):** Campay demo caps a collection at **25 XAF** and
triggers a **real USSD push on the account's phone number** (use a real MTN/Orange number, confirm
on the handset); Sudo cards draw from the account's default funding source, so funding one needs
`/simtopup` and a purchase is simulated from Dashboard → Simulator; POC state is in memory, so
**restarting `server.js` wipes every user, balance and card.**

## HTML prototype

`python3 prototype/build.py` then `python3 -m http.server 8742 --directory prototype`. With
`poc/server.js` running, the top-up and new-card buttons hit the real backend; stopped, the
prototype stays fully simulated. Reset the stored account with
`localStorage.removeItem("moniProfile")` in the browser console.

## Troubleshooting

- **`MoniPay.xcodeproj` missing** → normal, it is gitignored. `cd ios && xcodegen generate`.
- **`Unable to find a destination matching …`** → the iOS 26 runtime is missing.
  Xcode → Settings → Components.
- **A new package is not visible from the app** → it is missing from `packages:` or from the target
  `dependencies:` in `ios/project.yml`, or you did not regenerate.
- **A new file is not compiled** → packages pick files up from `Sources/<Name>/` automatically; if
  it is in `ios/App/`, regenerate.
- **Stale weirdness after a rename** → `rm -rf ios/Packages/<Name>/.build`, and for the app
  `xcodegen generate && xcodebuild clean`.

Reference: [XcodeGen project spec](https://github.com/yonaskolb/XcodeGen/blob/master/Docs/ProjectSpec.md) ·
[Swift Package Manager](https://www.swift.org/documentation/package-manager/)
