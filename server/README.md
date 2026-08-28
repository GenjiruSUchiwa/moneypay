# MoniPay server

MoniPay holds a FCFA balance for a user in the CEMAC zone, tops it up from MTN MoMo or Orange Money, and issues USD virtual cards against it.

This directory holds the backend: a modular monolith that serves the HTTP API for the iOS app. `CONTEXT.md` at the repository root defines the domain words. Until this server ships, `poc/server.js` is the reference implementation and defines the current contract; see `poc/README.md`.

## Tech stack

| Part | Technology |
|---|---|
| Runtime | .NET 10 (`net10.0`), C# with nullable reference types |
| Web | ASP.NET Core minimal APIs |
| Database | PostgreSQL through Npgsql and Entity Framework Core 10 |
| Providers | Campay (MoMo collection, Cameroon), Sudo Africa (USD card issuing) |
| Logs | Serilog to the console, and to Seq in production |
| Tests | xunit.v3 on Microsoft.Testing.Platform |
| Versions | MinVer, from git tags with the prefix `monipay.api-v` |

Package versions are central. `Directory.Packages.props` holds every version, and a project file names the package without one.

## Prerequisites

- .NET SDK 10.0.
- PostgreSQL, for a local run.

## Configure

The API reads its connection string from the `MoniPay` connection string name.

Set the local secrets with the .NET secret manager:

```bash
dotnet user-secrets set "ConnectionStrings:MoniPay" \
  "Host=localhost;Port=5432;Database=monipay;Username=monipay;Password=<your local password>" \
  --project server/src/MoniPay.Api
```

For a host without a secret store, give the same values as environment variables, with `__` between the parts: `MoniPay__ApplyMigrationsOnStartup`. Never commit a connection string or a provider key.

### Configuration keys

`src/MoniPay.Api/appsettings.json` holds the defaults. Every key sits under the `MoniPay` section:

- `ApplyMigrationsOnStartup`: Apply the EF Core migrations when the host starts.
- `OpenApi:Enabled`: Serve the OpenAPI document. On by default in Development only.

## Build

Run the commands that follow from the repository root:

```bash
dotnet restore server/MoniPay.slnx
dotnet build server/MoniPay.slnx --configuration Release
```

The build treats warnings as errors and applies the code style rules. The first restore also installs the git hooks. To skip the hooks, set `HUSKY=0`.

Check the format with `dotnet format server/MoniPay.slnx --verify-no-changes`.

## Run

```bash
dotnet run --project server/src/MoniPay.Api
```

The development profile listens on `http://localhost:5209`. It serves the OpenAPI document at `/openapi/v1.json`. The health endpoints are `/health` and `/health/ready`.

To run the container instead, build the image from this directory:

```bash
docker build -t monipay-api server
docker run -p 8080:8080 monipay-api
```

The container listens on port 8080 and runs as a user without privileges.

## Test

```bash
dotnet test server/MoniPay.slnx
```

`dotnet.config` selects the Microsoft.Testing.Platform runner. Without it, `dotnet test` finds no test and still reports success.

## Project layout

```
server/
  MoniPay.slnx                Solution
  Directory.Build.props       Shared MSBuild properties
  Directory.Packages.props    Central package versions
  dotnet.config               Test runner selection
  cliff.toml                  git-cliff config for CHANGELOG.md
  Dockerfile                  Runtime image
  src/MAP.md                  Module map: who owns what, who may reference whom
  src/MoniPay.*/              One project per module (class libraries)
  src/MoniPay.Api/            The HTTP host (ASP.NET Core)
  tests/MoniPay.Tests/        Integration and unit tests
```

**A module owns its whole vertical slice.** Endpoints do not collect in the host; each module
project has the same shape:

```
src/MoniPay.<Module>/
  <Module>Module.cs      Add<Module>Module(services, configuration) — the module's own DI
  Endpoints/             Map<Module>Endpoints(IEndpointRouteBuilder) — its HTTP routes
  Contracts/             request/response records — the wire shape
  Domain/                the types that decide; no ASP.NET Core, no EF attributes
  Persistence/           IEntityTypeConfiguration for its own tables
  MAP.md                 what this module owns, in a paragraph
```

A module never references a sibling module, so the compiler checks the dependency graph. Shared
types move down into `MoniPay.Kernel`; cross-module wiring happens in the host.

`src/MoniPay.Api` is **only** the host: `Program.cs`, the middleware pipeline, the OpenAPI
document, and health. Its `MoniPayModules.cs` holds the list of module assemblies — all the host
knows about the modules as a group. Composing the monolith reads as one line per module:

```csharp
builder.Services
    .AddKernelModule(builder.Configuration)
    .AddDataModule(builder.Configuration, MoniPayModules.ModuleAssemblies)
    .AddWalletModule(builder.Configuration);

app.MapHealthEndpoints();
app.MapWalletEndpoints();
```

Adding a module means creating the project, listing it in `MoniPay.slnx`, the `Dockerfile`
restore list and `MoniPayModules.ModuleAssemblies`, then adding those two calls. Nothing inside
another module changes, and a test can compose one module alone.

`src/MoniPay.Data` owns the `DbContext` and the connection but references no module: it applies
the `IEntityTypeConfiguration` types found in the assemblies the host hands it. The EF Core
migrations live in `src/MoniPay.Data/Migrations`, because a migration spans the whole schema and
there is one database.

## The API contract

`docs/api/openapi.json` at the repository root is the contract that the Swift client reads. Regenerate it after a change to an endpoint:

```bash
./scripts/update-openapi.sh
```

The script builds the API with `-p:GenerateOpenApiDocs=true`. Then it copies the document to `docs/api/openapi.json` and to the iOS client package. CI fails when the committed document and the build differ.
