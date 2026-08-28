---
title: The API Pipeline — Restore, Build, Contract, Format, Test
impact: HIGH
impactDescription: The same five commands run locally and in CI, so a green branch means a green pipeline
tags: ci, dotnet, github-actions, docker, minver, testcontainers, release
---

## The API Pipeline — Restore, Build, Contract, Format, Test

**Impact: HIGH**

`monipay-api-ci` runs exactly the commands you can run locally, in the same order, from the
repository root. There is no CI-only step and no local-only step; when the pipeline fails, you
reproduce it in one line.

```bash
dotnet restore server/MoniPay.slnx
dotnet build   server/MoniPay.slnx --no-restore --configuration Release   # warnings are errors
./scripts/update-openapi.sh && git diff --exit-code -- docs/api/openapi.json
dotnet format  server/MoniPay.slnx --no-restore --verify-no-changes
dotnet test    server/MoniPay.slnx --no-build --configuration Release      # needs Docker
```

**Incorrect (a workflow that cannot be reproduced and hides drift):**

```yaml
- run: dotnet build server/MoniPay.slnx -warnaserror:false   # green here, broken on the next machine
- run: dotnet test  server/MoniPay.slnx || true              # a failing suite that reports success
# no OpenAPI check: the iOS client silently drifts from the server
```

**Correct (`.github/workflows/monipay-api-ci.yml`):**

```yaml
name: monipay-api-ci

# Manual, like every workflow here: the same checks run locally before a merge.
on: workflow_dispatch

concurrency:
  group: monipay-api-ci-${{ github.ref }}
  cancel-in-progress: true

permissions:
  contents: read

env:
  CI: true
  HUSKY: 0                       # the commit hooks belong to a working clone, not a runner
  DOTNET_NOLOGO: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  build-and-test:
    name: build, test and format
    runs-on: ubuntu-latest       # the runner ships a Docker daemon, which Testcontainers needs
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0         # MinVer derives the version from tag history

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - run: dotnet restore server/MoniPay.slnx
      - run: dotnet build server/MoniPay.slnx --no-restore --configuration Release

      # The committed document is the Swift client's contract; drift must fail here.
      - name: Verify OpenAPI contract
        run: |
          ./scripts/update-openapi.sh
          git diff --exit-code -- docs/api/openapi.json

      - run: dotnet format server/MoniPay.slnx --no-restore --verify-no-changes
      - run: dotnet test server/MoniPay.slnx --no-build --configuration Release
```

**Why each detail is there:**

- **`HUSKY=0`** — `Directory.Build.props` installs the git hooks on restore. A runner is not a
  working clone; the Dockerfile sets it too.
- **`fetch-depth: 0`** — MinVer reads `monipay.api-v*` tags to compute the assembly version. A
  shallow clone silently produces `0.0.0-preview.0`.
- **`--no-restore` / `--no-build`** — later steps must test the artefacts the build produced, not
  rebuild with different inputs.
- **Ubuntu, not a container job** — Testcontainers starts PostgreSQL through the host daemon.
- **The OpenAPI check runs before format**, because a contract change is the failure most worth
  seeing first.

**Release — `monipay-api-release.yml`, manual.** git-cliff computes the next `monipay.api-v*`
version from the API commits since the last tag (`ios/**` is excluded in `server/cliff.toml`),
refreshes `server/CHANGELOG.md`, tags, and publishes the GitHub release. Then the job builds and
pushes the runtime image, tagged with the same version MinVer derived from that tag:

```yaml
      - name: Build and push the image
        if: steps.bump.outputs.release == 'true'
        env:
          TAG: ${{ steps.bump.outputs.tag }}
        run: |
          version="${TAG#monipay.api-v}"
          docker build -t "ghcr.io/${{ github.repository }}/monipay-api:$version" \
                       -t "ghcr.io/${{ github.repository }}/monipay-api:latest" server
          docker push "ghcr.io/${{ github.repository }}/monipay-api:$version"
          docker push "ghcr.io/${{ github.repository }}/monipay-api:latest"
```

The image version and the git tag are therefore the same number by construction, never by
hand-editing a `<Version>` in a `.csproj` — MinVer is the only source, and a hardcoded version is a
review rejection.

**A red pipeline is fixed, never re-run.** `|| true`, `continue-on-error`, and a disabled test are
all ways of shipping the failure; see [ci-check-failures](ci-check-failures.md).

Reference: [MinVer](https://github.com/adamralph/minver) ·
[dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format)
