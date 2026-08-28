---
title: Local Development — .NET 10, PostgreSQL, Docker
impact: LOW
impactDescription: Reference guide for getting the MoniPay server running locally
tags: reference, dotnet, setup, postgresql, docker, user-secrets
---

## Local Development — .NET 10, PostgreSQL, Docker

**Impact: LOW**

Everything below is run from the **repository root**.

### Prerequisites

| Tool | Why | Install |
|---|---|---|
| .NET SDK 10.0 | builds and runs the server | `brew install --cask dotnet-sdk` |
| PostgreSQL 17 | the local database | `brew install postgresql@17 && brew services start postgresql@17` |
| Docker | Testcontainers starts PostgreSQL for the test suite | Docker Desktop or `colima start` |

```bash
dotnet --version        # 10.0.x
docker info             # must answer; the suite fails without a daemon
```

### Create the local database

```bash
createuser --pwprompt monipay
createdb --owner monipay monipay
```

### Configure the secrets

The API refuses to boot without a connection string, an encryption key, and a session signing key.
Both keys must decode to 32 bytes. Nothing here is committed — see
[quality-secrets-and-config](quality-secrets-and-config.md).

```bash
dotnet user-secrets set "ConnectionStrings:MoniPay" \
  "Host=localhost;Port=5432;Database=monipay;Username=monipay;Password=<your local password>" \
  --project server/src/MoniPay.Api
dotnet user-secrets set "MoniPay:Encryption:KeyBase64" "$(openssl rand -base64 32)" \
  --project server/src/MoniPay.Api
dotnet user-secrets set "MoniPay:Sessions:SigningKeyBase64" "$(openssl rand -base64 32)" \
  --project server/src/MoniPay.Api
```

For the provider sandboxes — the same credentials `poc/.env` holds today:

```bash
# https://demo.campay.net -> sign up -> app -> credentials
dotnet user-secrets set "MoniPay:Campay:AppUsername" "<username>" --project server/src/MoniPay.Api
dotnet user-secrets set "MoniPay:Campay:AppPassword" "<password>" --project server/src/MoniPay.Api
# https://app.sudo.africa (sandbox) -> Developers -> API Keys
dotnet user-secrets set "MoniPay:Sudo:ApiKey" "<key>" --project server/src/MoniPay.Api
```

`dotnet user-secrets list --project server/src/MoniPay.Api` shows what is set.

### Build and run

```bash
dotnet restore server/MoniPay.slnx        # also installs the git hooks; HUSKY=0 skips them
dotnet build   server/MoniPay.slnx
dotnet run --project server/src/MoniPay.Api
```

The development profile listens on `http://localhost:5209`:

- OpenAPI document — `http://localhost:5209/openapi/v1.json`
- health — `/health` and `/health/ready`

`MoniPay:ApplyMigrationsOnStartup` is `true` in development, so a fresh clone gets a schema without
a manual step. To apply migrations by hand:

```bash
dotnet ef database update --project server/src/MoniPay.Data --startup-project server/src/MoniPay.Api
```

`dotnet ef` comes from the tool manifest: `dotnet tool restore` once per clone.

### Test

```bash
dotnet test server/MoniPay.slnx                                  # whole suite, needs Docker
dotnet test server/MoniPay.slnx --filter-class '*WalletTests'    # one class, MTP filter syntax
```

The suite starts one PostgreSQL container for the whole run and drives the real host over HTTP. It
does not touch your local `monipay` database.

### Format and contract

```bash
dotnet format server/MoniPay.slnx                       # apply
dotnet format server/MoniPay.slnx --verify-no-changes   # what CI runs
./scripts/update-openapi.sh                             # regenerate docs/api/openapi.json + the iOS copy
./scripts/check-openapi-sync.sh                         # the copies match the contract
```

### Container

```bash
docker build -t monipay-api server
docker run -p 8080:8080 \
  -e ConnectionStrings__MoniPay="Host=host.docker.internal;Database=monipay;Username=monipay;Password=…" \
  -e MoniPay__Encryption__KeyBase64="$(openssl rand -base64 32)" \
  -e MoniPay__Sessions__SigningKeyBase64="$(openssl rand -base64 32)" \
  monipay-api
```

The image listens on 8080 and runs as a user without privileges. The Alpine runtime installs
`icu-libs` and `tzdata`, and `InvariantGlobalization` stays `false`: without them `fr-CM` resolves
to the invariant culture and every `.resx` lookup falls back — see
[api-localization](api-localization.md).

### Troubleshooting

| Symptom | Cause |
|---|---|
| `dotnet test` reports success and runs nothing | `server/dotnet.config` missing — VSTest discovered no test |
| `Docker.DockerUnavailableException` | no daemon; start Docker Desktop or `colima start` |
| `ConnectionStrings:MoniPay is required` at startup | user-secrets not set, or set on the wrong `--project` |
| version is `0.0.0-preview.0` | shallow clone; MinVer needs the `monipay.api-v*` tag history |
| a git hook fires in a script or container | export `HUSKY=0` |

The Node POC (`node poc/server.js`, port 8743) stays the reference contract until the .NET server
replaces it; see `poc/README.md`.

Reference: [dotnet CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/) ·
[EF Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)
