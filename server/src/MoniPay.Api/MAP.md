# MoniPay.Api

The host, and only the host. It boots the process, composes the modules, and owns the
cross-cutting HTTP concerns. It contains no business logic and no module's endpoints.

- `Program.cs` — Serilog, one `Add<Module>Module()` per module, localization, middleware, then
  one `Map<Module>Endpoints()` per module.
- `MoniPayModules.cs` — the list of module assemblies, which is all the host knows about the
  modules as a group.
- `MoniPayConfiguration.cs` — every configuration key the host reads, and the environment names
  it branches on. A mistyped key silently returns the default, which is why none is a literal.
- `Localization.cs` — the supported cultures (`fr-CM`, `fr`, `en`), the French default, and the
  `Resources` folder convention every module follows.
- `HealthChecks.cs` — the registered check names and the `ready` tag readiness filters on.
- `Endpoints/HealthEndpoints.cs` + `HealthRoutes` / `HealthEndpointNames` / `HealthTags` /
  `HealthSummaries` — `/health`. Health belongs to the host because no module owns it.
- `Contracts/HealthResponse.cs` — the host's own contract, the only one that lives here.
- `OpenApi/` — document transformers. `SortedOpenApiDocumentTransformer` sorts paths so an
  unrelated change does not reshuffle the committed contract.

References every module.
