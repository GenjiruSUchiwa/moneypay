---
title: One Workflow Per Component, Never a Repository-Wide Build
impact: MEDIUM
impactDescription: A shared pipeline makes an app change wait on a backend runner, and hides which component actually failed
tags: monorepo, ci, github-actions, workflows, releases
---

## One Workflow Per Component, Never a Repository-Wide Build

**Impact: MEDIUM**

The API and the app need different runners, different toolchains and different release
mechanics: `server/` builds on `ubuntu-latest` with the .NET SDK, `ios/` needs a macOS runner
with Xcode. A single workflow that does both would run a macOS runner for a C# change and leave
"CI failed" saying nothing about which half broke.

So each component gets its own file, named `monipay-<component>-<job>.yml`:

| Workflow | Component | What it does |
|---|---|---|
| `monipay-api-ci` | `server/` | restore, build, verify the contract, verify format, test |
| `monipay-api-release` | `server/` | compute the next `monipay.api-v*`, changelog, tag, release |
| `monipay-ios-ci` | `ios/` | xcodegen, swiftlint, build and test the app |
| `monipay-ios-version` | `ios/` | compute the next `monipay.ios-v*`, changelog, tag, release |
| `monipay-ios-release` | `ios/` | escape hatch: release a hand-pushed iOS tag |
| `pr-title` | both | the PR title follows Conventional Commits |

**Incorrect (one job for the whole repository):**

```yaml
jobs:
  build:
    runs-on: macos-latest        # ❌ a macOS runner to compile C#
    steps:
      - run: dotnet test server/MoniPay.slnx
      - run: xcodebuild -scheme MoniPay test   # ❌ one red X for two unrelated failures
```

**Correct (a component-shaped workflow with its own concurrency group):**

```yaml
name: monipay-api-ci

# Manual only: every workflow in this repo runs on demand, from the Actions tab or
# with `gh workflow run monipay-api-ci.yml --ref <branch>`.
on:
  workflow_dispatch:

concurrency:
  group: monipay-api-ci-${{ github.ref }}   # per component AND per ref, so two branches
  cancel-in-progress: true                  # do not cancel each other

permissions:
  contents: read                            # least privilege; the release workflow asks for write

env:
  CI: true
  HUSKY: 0                                  # the commit hooks belong to a working clone
  DOTNET_NOLOGO: true

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0                    # MinVer walks tag history
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore server/MoniPay.slnx
      - run: dotnet build server/MoniPay.slnx --no-restore --configuration Release
      - name: Verify OpenAPI contract
        run: |
          ./scripts/update-openapi.sh
          git diff --exit-code -- docs/api/openapi.json
      - run: dotnet format server/MoniPay.slnx --no-restore --verify-no-changes
      - run: dotnet test server/MoniPay.slnx --no-build --configuration Release
```

**The rules each workflow follows:**

1. **`workflow_dispatch` only.** Every workflow in this repository runs on demand; there are no
   `push` or `pull_request` triggers. The same checks run locally before a merge. Do not add an
   automatic trigger to one workflow alone — that makes the CI story inconsistent.
2. **`fetch-depth: 0`.** MinVer and git-cliff both walk tag history. A shallow clone stamps
   `0.0.0` on a release artefact and generates an empty changelog.
3. **`HUSKY: 0`.** The hook bootstrap in `Directory.Build.props` fires on restore. A runner has
   no commits to lint.
4. **A concurrency group per component and ref**, so a rerun on one branch cancels only itself.
5. **`permissions` at the top, read by default.** Only the release workflows ask for
   `contents: write`.
6. **Reuse `--no-restore` / `--no-build`** down the step chain: build once, test the artefact you
   built, not a second implicit build with different flags.

**CI runs exactly the commands a developer runs.** Every step above has a local twin in
`agents/commands.md`. When CI catches something a developer could not have caught locally, the
gap is the bug — fix the local command, do not add a CI-only step.

Reference: [Workflow syntax for GitHub Actions](https://docs.github.com/en/actions/reference/workflow-syntax-for-github-actions)
