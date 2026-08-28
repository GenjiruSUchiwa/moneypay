---
title: EF Core Conventions — One Context, Module-Owned Persistence
impact: HIGH
impactDescription: Prevents a shared mapping file that every module edits, and float columns that lose francs
tags: data, efcore, dbcontext, postgresql, decimal, timeprovider, projections
---

## EF Core Conventions — One Context, Module-Owned Persistence

**Impact: HIGH**

One PostgreSQL database, one `MoniPayDbContext`, and **zero entity knowledge inside it**. Every
module keeps its mappings in its own `Persistence/` folder next to the entities they map; the
context only composes the assemblies it was handed. Adding a module never means editing shared
mapping code, and a test can compose one module alone.

```
server/src/MoniPay.Wallet/
  Domain/       WalletEntry.cs            the entity and its behaviour
  Persistence/  WalletEntryConfiguration.cs   internal IEntityTypeConfiguration
                WalletSets.cs                 the module's DbSet accessors
server/src/MoniPay.Data/
  MoniPayDbContext.cs  ModuleAssemblies.cs  PersistenceModule.cs  Migrations/
```

**The context, and it stays this small:**

```csharp
// server/src/MoniPay.Data/MoniPayDbContext.cs
public sealed class MoniPayDbContext(
    DbContextOptions<MoniPayDbContext> options,
    ModuleAssemblies modules) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Each module's Persistence/ folder supplies its own IEntityTypeConfiguration<T>.
        // MoniPay.Data names no entity, so a new module adds no line here.
        foreach (var assembly in modules.Value)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }
}

/// <summary>The assemblies whose entity configurations compose the model.</summary>
public sealed record ModuleAssemblies(IReadOnlyList<Assembly> Value);
```

**Incorrect (every module editing one file, floats for money, ambient clock):**

```csharp
public sealed class MoniPayDbContext : DbContext
{
    public DbSet<WalletEntry> WalletEntries => Set<WalletEntry>();   // the context knows every module
    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<WalletEntry>().Property(e => e.Amount).HasColumnType("double precision"); // loses francs
        builder.Entity<WalletEntry>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");    // untestable clock
    }
}
```

**Correct (module-owned, `internal`, explicit column types):**

```csharp
// server/src/MoniPay.Wallet/Persistence/WalletEntryConfiguration.cs
internal sealed class WalletEntryConfiguration : IEntityTypeConfiguration<WalletEntry>
{
    public void Configure(EntityTypeBuilder<WalletEntry> builder)
    {
        builder.ToTable("wallet_entries");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id).HasColumnName("id");
        builder.Property(entry => entry.UserId).HasColumnName("user_id").IsRequired();

        // FCFA has no minor unit: a franc is the smallest thing that exists. bigint, signed,
        // because the ledger is append-only and a debit is a negative entry.
        builder.Property(entry => entry.AmountXaf).HasColumnName("amount_xaf").IsRequired();

        builder.Property(entry => entry.Kind)
            .HasColumnName("kind").HasConversion<string>().HasMaxLength(24).IsRequired();

        // The rate that produced this entry, kept for audit. numeric, never float.
        builder.Property(entry => entry.UsdToXafRate)
            .HasColumnName("usd_to_xaf_rate").HasPrecision(18, 6);

        builder.Property(entry => entry.IdempotencyKey)
            .HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        builder.Property(entry => entry.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(entry => new { entry.UserId, entry.CreatedAt });
        builder.HasIndex(entry => entry.IdempotencyKey).IsUnique();

        builder.HasOne<User>().WithMany().HasForeignKey(entry => entry.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
```

```csharp
// server/src/MoniPay.Wallet/Persistence/WalletSets.cs — C# 14 extension members, internal to the module
internal static class WalletSets
{
    extension(MoniPayDbContext database)
    {
        public DbSet<WalletEntry> WalletEntries => database.Set<WalletEntry>();
    }
}
```

Make the accessor `public` only when another module legitimately reads the table — and prefer
exposing a method on the owning service instead; see
[architecture-dotnet-module-boundaries](architecture-dotnet-module-boundaries.md).

**Non-negotiables:**

- **Money columns.** Whole-franc and cent amounts are `bigint` minor units. A column that genuinely
  holds a fraction — an FX rate, a margin — is `numeric` with an explicit `HasPrecision(18, 6)`.
  `float`, `real`, and `double precision` are banned in this schema; see
  [data-money-ledger](data-money-ledger.md).
- **Time comes from `TimeProvider`,** injected into the service that creates the row. No
  `DateTime.UtcNow`, no `now()` default: a frozen clock in tests is what makes an expiry assertable.
  Store `DateTimeOffset` (`timestamptz`), never a naive `DateTime`.
- **Lazy loading stays off** (do not install the proxies package). An `Include` is deliberate.
- **Read paths project.** `Select` into the shape you need instead of materialising an entity and
  mapping it; add `AsNoTracking()` on anything you will not save.
- **Enums are stored as strings** (`HasConversion<string>()` with a `HasMaxLength`), so a reordered
  enum cannot silently reinterpret existing rows.
- **snake_case tables and columns**, matching PostgreSQL habit and the migration SQL you will read.
- **The context is scoped**; a background worker creates its own scope per cycle and never captures
  one across iterations.
- **Migrations live in `MoniPay.Data/Migrations/`** even though the configurations do not — one
  database means one ordered history; see [data-efcore-migrations](data-efcore-migrations.md).

```csharp
// Projection, not entity graph: this is a list screen, it needs four columns.
var recent = await database.WalletEntries
    .AsNoTracking()
    .Where(entry => entry.UserId == userId)
    .OrderByDescending(entry => entry.CreatedAt)
    .Take(limit)
    .Select(entry => new LedgerLine(entry.Id, entry.AmountXaf, entry.Kind, entry.CreatedAt))
    .ToListAsync(cancellationToken)
    .ConfigureAwait(false);
```

Reference: [EF Core — entity type configuration](https://learn.microsoft.com/en-us/ef/core/modeling/) ·
[Npgsql value mapping](https://www.npgsql.org/efcore/mapping/general.html)
