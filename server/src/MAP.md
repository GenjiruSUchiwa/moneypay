# server/src — module map

One project per bounded context. Each module owns its whole vertical slice: the tables it
writes, the domain that decides, the contracts it speaks, and the HTTP routes that expose it.
`MoniPay.Api` is only the host.

| Project | Owns | May reference |
|---|---|---|
| `MoniPay.Kernel` | Primitives: `Money`, `Currency`, the clock, shared error types. No business logic. | nothing |
| `MoniPay.Data` | `MoniPayDbContext`, the connection, `Migrations/`. Applies the modules' entity configurations; references no module. | `Kernel` |
| `MoniPay.Users` | The user profile: normalized contact data, locale, legal consent. | `Kernel`, `Data` |
| `MoniPay.Wallet` | The FCFA ledger: balance, holds, authorization. | `Kernel`, `Data` |
| `MoniPay.Api` | The host: `Program.cs`, middleware, the OpenAPI document, health. No business logic. | every module |

Planned, not yet created: `Sessions`, `TopUps`, `Cards`, `Transactions`, `Fx`, `Kyc`,
`Notifications`, `Outbox`.

## Rules

- **A module never references a sibling module.** Shared types move down into `Kernel`; shared
  behaviour is wired in the host. A `ProjectReference` from `MoniPay.Cards` to `MoniPay.Wallet`
  is the mistake this layout exists to make impossible.
- **Anatomy of a module** — every one has the same shape:

  ```
  src/MoniPay.<Module>/
    <Module>Module.cs      # Add<Module>Module(services, configuration): the module's own DI
    Endpoints/             # Map<Module>Endpoints(IEndpointRouteBuilder): its HTTP routes
    Contracts/             # request/response records — the wire shape, records only
    Domain/                # the types that decide; no ASP.NET Core, no EF attributes
    Persistence/           # IEntityTypeConfiguration for its own tables
    MAP.md                 # what this module owns, in a paragraph
  ```

- **Adding a module** means: create the project, add it to `MoniPay.slnx` and the `Dockerfile`
  restore list, add its assembly to `MoniPayModules.ModuleAssemblies`, and add two calls in
  `Program.cs` (`Add<Module>Module()`, `Map<Module>Endpoints()`). Nothing inside another module
  changes.
