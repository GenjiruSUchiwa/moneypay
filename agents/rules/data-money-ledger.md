---
title: The Wallet Is an Append-Only Ledger in Minor Units
impact: CRITICAL
impactDescription: A mutable balance column plus a retried MoMo callback is how a wallet double-credits real money
tags: data, money, ledger, idempotency, decimal, xaf, usd, campay
---

## The Wallet Is an Append-Only Ledger in Minor Units

**Impact: CRITICAL**

A balance is not a column you overwrite. It is the sum of immutable entries, each one explaining
itself: who, how much, why, from which provider event. Providers retry, users tap twice, and a
webhook arrives after the poll already succeeded — an append-only ledger with a unique idempotency
key is what makes all three harmless.

**Two currencies, two exponents, zero floats:**

| Currency | Minor unit | CLR type | Column |
|---|---|---|---|
| XAF (FCFA) | 1 franc — **0 decimals** | `long AmountXaf` | `bigint` |
| USD | 1 cent — **2 decimals** | `long AmountUsdCents` | `bigint` |
| FX rate, margin | not money | `decimal` | `numeric(18,6)` |

`double`, `float`, and `real` are banned anywhere near an amount. `decimal` is correct for a rate or
a margin — a value that gets *rounded into* money — and never for the stored amount itself.

**Incorrect (mutable balance, floating amounts, no idempotency):**

```csharp
public sealed class Wallet
{
    public double BalanceFcfa { get; set; }   // 0.1 + 0.2 != 0.3, and FCFA has no decimals at all
}

public async Task OnCampayCallbackAsync(CampayCallback callback)
{
    var wallet = await database.Wallets.SingleAsync(w => w.UserId == callback.UserId);
    wallet.BalanceFcfa += callback.Amount;    // a retried callback credits twice
    await database.SaveChangesAsync();        // and the "why" is nowhere in the database
}
```

**Correct (one entry per event, keyed by the provider reference):**

```csharp
// server/src/MoniPay.Wallet/Domain/WalletEntry.cs
public sealed class WalletEntry
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    /// <summary>Whole francs. Positive credits the wallet, negative debits it.</summary>
    public long AmountXaf { get; init; }

    public WalletEntryKind Kind { get; init; }

    /// <summary>The rate applied when this entry converted USD, for audit. Null for a pure FCFA move.</summary>
    public decimal? UsdToXafRate { get; init; }

    /// <summary>
    /// One entry per real-world event. Campay's external reference for a collection, Sudo's
    /// authorization id for a card debit: the same event replayed writes nothing.
    /// </summary>
    public required string IdempotencyKey { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public enum WalletEntryKind { TopUp, CardFunding, CardAuthorization, CardRefund, Fee, Reversal }
```

```csharp
// server/src/MoniPay.Wallet/Domain/WalletService.cs
public sealed class WalletService(MoniPayDbContext database, TimeProvider timeProvider)
{
    /// <summary>
    /// Appends one entry, or returns the entry an earlier attempt already wrote. The unique index
    /// on idempotency_key is the real guard: two concurrent callbacks race to insert, and the
    /// loser reads the winner's row rather than crediting again.
    /// </summary>
    public async Task<WalletEntry> AppendAsync(
        Guid userId,
        long amountXaf,
        WalletEntryKind kind,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var entry = new WalletEntry
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            AmountXaf = amountXaf,
            Kind = kind,
            IdempotencyKey = idempotencyKey,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        database.WalletEntries.Add(entry);

        try
        {
            await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return entry;
        }
        catch (DbUpdateException failure) when (failure.InnerException is PostgresException { SqlState: "23505" })
        {
            database.Entry(entry).State = EntityState.Detached;

            return await database.WalletEntries
                .SingleAsync(existing => existing.IdempotencyKey == idempotencyKey, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public Task<long> BalanceXafAsync(Guid userId, CancellationToken cancellationToken) =>
        database.WalletEntries
            .Where(entry => entry.UserId == userId)
            .SumAsync(entry => entry.AmountXaf, cancellationToken);
}
```

**Rules:**

- **Entries are never updated or deleted.** A mistake is corrected by a `Reversal` entry that points
  at the one it reverses. The history is the audit trail a fintech is asked for.
- **Every write carries an idempotency key** from the provider: Campay's `external_reference`,
  Sudo's authorization id. Where there is no natural one — a user-initiated transfer — the client
  sends one and the endpoint passes it through.
- **Debit before you call the provider.** Issuing a Sudo card debits `ceil(amountUsdCents / 100 ×
  rate)` francs *first*; if the provider then refuses, append a `Reversal`. The opposite order gives
  away cards for free.
- **Conversions round in MoniPay's favour and are computed once**, in `Fx`, from `decimal` rate and
  margin, then rounded up to the whole franc with `decimal.Ceiling`. Never recompute a stored
  amount from a live rate — the entry records the rate it used.
- **A cached balance column is allowed only as a projection** rebuilt from the entries, never as the
  authority. If the two disagree, the entries are right.
- **Money crosses the HTTP boundary as an integer of minor units**, so `Money` never reaches JSON as
  a floating number; see [api-openapi-contract](api-openapi-contract.md).
- **The server never formats an amount into a contract.** No thousands separator, no currency
  symbol, no `ToString()` without an explicit `CultureInfo` — a response carries the integer and the
  currency code, and the client formats for its own locale; see
  [api-localization](api-localization.md).

Reference: [Ledger design — Martin Fowler, Accounting Patterns](https://martinfowler.com/eaaDev/AccountingNarrative.html) ·
[decimal (C# reference)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types)
