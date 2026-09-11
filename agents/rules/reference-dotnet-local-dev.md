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
| Docker | Compose runs PostgreSQL and Seq; Testcontainers starts PostgreSQL for the test suite | Docker Desktop or `colima start` |
| PostgreSQL 17 (optional) | the Homebrew alternative to the Compose database | `brew install postgresql@17 && brew services start postgresql@17` |

```bash
dotnet --version        # 10.0.x
docker info             # must answer; the suite fails without a daemon
```

### Start the local services

`server/compose.yaml` starts PostgreSQL 17 and Seq. The API stays outside Compose and runs with
`dotnet run`, so debugging and hot reload work.

```bash
export MONIPAY_POSTGRES_PASSWORD="$(openssl rand -base64 24)"
docker compose -f server/compose.yaml up -d --wait
docker compose -f server/compose.yaml ps
```

- PostgreSQL listens on `127.0.0.1:5432` with database `monipay` and user `monipay`.
- Seq listens on <http://localhost:5341> with authentication disabled, for local viewing only.
- The password comes from `MONIPAY_POSTGRES_PASSWORD`; nothing in `compose.yaml` is a credential.
- `docker compose -f server/compose.yaml down` stops the services and keeps the named volume.
  Add `-v` only when you want to delete the local database.

Without Docker, use the Homebrew server instead — stop the other PostgreSQL first, because both
bind port `5432`:

```bash
brew services start postgresql@17
createuser --pwprompt monipay
createdb --owner monipay monipay
```

### Configure the secrets

The API refuses to boot without a connection string, a token issuer and audience, the published
legal versions, an email sender, and five distinct encryption keys that each decode to 32 bytes.
Nothing here is committed — see [quality-secrets-and-config](quality-secrets-and-config.md).

```bash
dotnet user-secrets set "ConnectionStrings:MoniPay" \
  "Host=localhost;Port=5432;Database=monipay;Username=monipay;Password=$MONIPAY_POSTGRES_PASSWORD" \
  --project server/src/MoniPay.Api

for key in Sessions:SigningKeyBase64 Sessions:VerificationCodeKeyBase64 \
           Sessions:PersonalDataKeyBase64 Users:PersonalDataKeyBase64 \
           Notifications:DataKeyBase64; do
  dotnet user-secrets set "MoniPay:$key" "$(openssl rand -base64 32)" --project server/src/MoniPay.Api
done

dotnet user-secrets set "MoniPay:Sessions:Issuer" "https://api.monipay.local" --project server/src/MoniPay.Api
dotnet user-secrets set "MoniPay:Sessions:Audience" "monipay-ios" --project server/src/MoniPay.Api
dotnet user-secrets set "MoniPay:Sessions:Legal:TermsVersion" "<published terms version>" --project server/src/MoniPay.Api
dotnet user-secrets set "MoniPay:Sessions:Legal:PrivacyVersion" "<published privacy version>" --project server/src/MoniPay.Api
dotnet user-secrets set "MoniPay:Notifications:Email:ApiKey" "<Bird API key>" --project server/src/MoniPay.Api
dotnet user-secrets set "MoniPay:Notifications:Email:FromAddress" "no-reply@mail.example.com" --project server/src/MoniPay.Api
```

SMS is optional. It enables only when both the API key and the sender ID are set; with a single one,
the host still starts and SMS delivery stays off. A complete pair with an invalid base URL fails
startup.

```bash
dotnet user-secrets set "MoniPay:Notifications:Sms:ApiKey" "<Bird API key>" --project server/src/MoniPay.Api
dotnet user-secrets set "MoniPay:Notifications:Sms:SenderId" "<account sender ID>" --project server/src/MoniPay.Api
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
- Seq — <http://localhost:5341> receives the development log stream

`MoniPay:ApplyMigrationsOnStartup` is `false` in `appsettings.json`. A fresh database needs its
schema once:

```bash
dotnet ef database update --project server/src/MoniPay.Data --startup-project server/src/MoniPay.Api
```

For a throwaway database, let the host migrate on boot instead:

```bash
dotnet user-secrets set "MoniPay:ApplyMigrationsOnStartup" "true" --project server/src/MoniPay.Api
```

`dotnet ef` comes from the tool manifest: `dotnet tool restore` once per clone.

### Test

```bash
dotnet test server/MoniPay.slnx                                  # whole suite, needs Docker
dotnet test server/MoniPay.slnx -- --filter-class '*WalletEndpointsTests' # one class, MTP filter syntax
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
docker build -f server/Dockerfile -t monipay-api server
docker run --rm --read-only --tmpfs /tmp -p 8080:8080 \
  -e ConnectionStrings__MoniPay="Host=host.docker.internal;Port=5432;Database=monipay;Username=monipay;Password=…" \
  -e MoniPay__Sessions__Issuer="https://api.monipay.local" \
  -e MoniPay__Sessions__Audience="monipay-ios" \
  -e MoniPay__Sessions__SigningKeyBase64="…" \
  -e MoniPay__Sessions__VerificationCodeKeyBase64="…" \
  -e MoniPay__Sessions__PersonalDataKeyBase64="…" \
  -e MoniPay__Users__PersonalDataKeyBase64="…" \
  -e MoniPay__Notifications__DataKeyBase64="…" \
  -e MoniPay__Sessions__Legal__TermsVersion="…" \
  -e MoniPay__Sessions__Legal__PrivacyVersion="…" \
  -e MoniPay__Notifications__Email__ApiKey="…" \
  -e MoniPay__Notifications__Email__FromAddress="…" \
  monipay-api
```

The image listens on `8080`, runs as `monipay`, and installs `icu-libs` and `tzdata`; the
`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` environment makes `fr-CM` resolve through ICU rather
than the invariant culture. It excludes `appsettings.Development.json`, so every secret comes from
the environment — see [api-localization](api-localization.md).

In production, set `AllowedHosts` to the API host name instead of `*`, and list the edge addresses in
`MoniPay__ForwardedHeaders__KnownProxies` as a comma-separated list.

### Troubleshooting

| Symptom | Cause |
|---|---|
| `dotnet test` reports success and runs nothing | `server/dotnet.config` missing — VSTest discovered no test |
| `Docker.DockerUnavailableException` | no daemon; start Docker Desktop or `colima start` |
| `The MoniPay:Notifications:Sms … settings are invalid` at startup | both credentials are set but `Sms:BaseUrl` is not an absolute https URL |
| `/health/ready` reports `Unhealthy` | PostgreSQL is stopped, or `ConnectionStrings:MoniPay` does not match the running server |
| `MoniPay:Sessions:SigningKeyBase64 must hold a base64-encoded 32-byte key` | that key is missing, empty or not `openssl rand -base64 32` |
| `password authentication failed for user "monipay"` | `postgres` container reused an older volume; `docker compose … down -v` and set the password again |
| version is `0.0.0-preview.0` | shallow clone; MinVer needs the `monipay.api-v*` tag history |
| a git hook fires in a script or container | export `HUSKY=0` |

The Node POC (`node poc/server.js`, port 8743) stays the reference contract until the .NET server
replaces it; see `poc/README.md`.

Reference: [dotnet CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/) ·
[EF Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)
