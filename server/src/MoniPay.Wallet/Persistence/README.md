# MoniPay.Wallet — Persistence

The EF Core `IEntityTypeConfiguration` types for the tables this module owns, one file per
entity (`WalletConfiguration.cs`, `HoldConfiguration.cs`, …).

`MoniPay.Data` discovers them: the host lists this assembly in `MoniPayModules.ModuleAssemblies`,
and `MoniPayDbContext.OnModelCreating` calls `ApplyConfigurationsFromAssembly` on each. No module
registers its tables by hand, and no module configures another module's tables.

The migrations themselves live in `MoniPay.Data/Migrations`, because a migration spans the whole
schema and there is one database.

Empty until the ledger has its first table.
