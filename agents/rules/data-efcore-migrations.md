---
title: Migrations Are Reviewed SQL, Not Generated Noise
impact: HIGH
impactDescription: An unreviewed migration is a production outage or a silently dropped money column
tags: data, efcore, migrations, postgresql, deployment
---

## Migrations Are Reviewed SQL, Not Generated Noise

**Impact: HIGH**

Every schema change ships as an EF Core migration in `server/src/MoniPay.Data/Migrations/`. The
generator writes a first draft; you read it, and you are accountable for what it does to a table
holding real balances.

**The workflow, from the repository root:**

```bash
# 1. Change the entity and its IEntityTypeConfiguration in the owning module.
# 2. Generate the migration. --project holds the migrations, --startup-project builds the model.
dotnet ef migrations add AddWalletHolds \
  --project server/src/MoniPay.Data \
  --startup-project server/src/MoniPay.Api

# 3. Read the generated SQL before you read the C#.
dotnet ef migrations script --idempotent \
  --project server/src/MoniPay.Data \
  --startup-project server/src/MoniPay.Api

# 4. Apply locally and run the suite.
dotnet ef database update --project server/src/MoniPay.Data --startup-project server/src/MoniPay.Api
dotnet test server/MoniPay.slnx
```

**Naming.** The timestamp prefix is automatic; the name is a `PascalCase` noun phrase describing the
change, not the ticket: `AddWalletHolds`, `CardSpendLimits`, `TopUpIdempotencyKey`. Never
`Update1`, `Fix`, or `Migration2`.

**Incorrect (a rename that EF turns into data loss):**

```csharp
// The entity property AmountXaf was renamed to AmountFcfa. EF cannot see a rename, so:
migrationBuilder.DropColumn(name: "amount_xaf", table: "wallet_entries");
migrationBuilder.AddColumn<long>(name: "amount_fcfa", table: "wallet_entries", nullable: false, defaultValue: 0L);
// Every historical entry is now zero. The ledger is gone, and the balance recomputes to nothing.
```

**Correct (hand-edit the migration to preserve the data):**

```csharp
public partial class RenameAmountColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.RenameColumn(
            name: "amount_xaf", table: "wallet_entries", newName: "amount_fcfa");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.RenameColumn(
            name: "amount_fcfa", table: "wallet_entries", newName: "amount_xaf");
}
```

**What to check in every generated migration:**

- **No `DropColumn` or `DropTable` you did not intend.** Money tables are append-only; a drop there
  needs an explicit decision recorded in `docs/adr/`.
- **A new non-nullable column on a populated table needs a default or a backfill.** Prefer: add
  nullable, backfill with an `Sql(...)` statement, then alter to non-nullable in the same migration.
- **Indexes on the columns the new query filters on.** A migration that adds a lookup without its
  index is a slow query waiting for the first thousand users.
- **`Down` actually reverses `Up`.** If it cannot (a destructive change), say so in a comment rather
  than leaving a `Down` that silently does the wrong thing.
- **One migration per PR.** Two migrations in one branch means you changed your mind — squash them
  by deleting both and regenerating, while they are still unmerged.

**A merged migration is immutable.** Once it is on `main`, it has run on someone's database. Fix a
mistake with a *new* migration; never edit or delete a merged one.

**Applying migrations:**

- Locally and in tests: `MoniPay:ApplyMigrationsOnStartup=true`, so a fresh clone and each
  Testcontainers run get a schema without a manual step.
- In production: **`false`**. Migrations are applied as a deliberate step before the new image is
  rolled out, because the host must not race itself when two instances start together.

```json
// server/src/MoniPay.Api/appsettings.Production.json
{ "MoniPay": { "ApplyMigrationsOnStartup": false } }
```

Reference: [EF Core migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/) ·
[Applying migrations in production](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
