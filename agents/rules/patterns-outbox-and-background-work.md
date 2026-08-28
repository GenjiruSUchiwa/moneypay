---
title: Outbox and Background Workers — Record First, Call the Provider Later
impact: HIGH
impactDescription: A crash between the database commit and the Campay call must not lose or duplicate money
tags: patterns, outbox, background-service, idempotency, worksignal, campay, sudo
---

## Outbox and Background Workers — Record First, Call the Provider Later

**Impact: HIGH**

Two things can fail independently: the database and the provider. A card issued at Sudo but never
recorded is a card MoniPay cannot see; a top-up recorded but never collected is a balance the user
did not fund. The way out is not a distributed transaction — it is to **commit the intent locally,
then let a worker drive it to the provider until it succeeds**.

`MoniPay.Outbox` owns that: a message table written in the *same transaction* as the domain change,
and a worker that claims, executes, and retries with backoff.

**Incorrect (provider call inside the request, no record of the intent):**

```csharp
topUps.MapPost("/", async (StartTopUpRequest body, MoniPayDbContext db, HttpClient http) =>
{
    var topUp = new TopUp { Status = TopUpStatus.Pending };
    db.TopUps.Add(topUp);
    await db.SaveChangesAsync();

    await http.PostAsJsonAsync("collect/", new { amount = body.AmountXaf });  // 20 s, on the request thread
    // If the process dies here, nothing will ever retry. If it times out, the user taps again
    // and Campay collects twice.
});
```

**Correct (one transaction, then a worker):**

```csharp
// server/src/MoniPay.TopUps/Domain/TopUpService.cs
public async Task<TopUp> StartAsync(Guid userId, TopUpOrder order, CancellationToken cancellationToken)
{
    var topUp = new TopUp
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        AmountXaf = order.AmountXaf,
        Status = TopUpStatus.Pending,
        IdempotencyKey = order.IdempotencyKey,
        CreatedAt = timeProvider.GetUtcNow(),
    };

    database.TopUps.Add(topUp);

    // Same DbContext, therefore the same transaction: the intent and its outbox message either
    // both exist or neither does.
    outbox.Enqueue(new OutboxMessage
    {
        Id = Guid.CreateVersion7(),
        Kind = OutboxKind.CollectFunds,
        SubjectId = topUp.Id,
        IdempotencyKey = order.IdempotencyKey,
        CreatedAt = timeProvider.GetUtcNow(),
    });

    await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    collectionSignal.Raise();     // a nudge, not a queue: the work is already durable
    return topUp;
}
```

```csharp
// server/src/MoniPay.Outbox/Domain/OutboxWorker.cs
internal sealed partial class OutboxWorker(
    IServiceScopeFactory scopes,
    CollectionSignal signal,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // The worker is a singleton; the processor and its DbContext are scoped.
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<OutboxProcessor>()
                    .RunCycleAsync(stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception failure)
            {
                LogCycleFailed(failure);   // one bad message never kills the loop
            }

            // Wakes on the signal, or on the interval — the cycle that catches whatever no
            // signal reached, including work left behind by a crashed instance.
            await signal.WaitAsync(options.Value.PollInterval, timeProvider, stoppingToken)
                .ConfigureAwait(false);
        }
    }

    [LoggerMessage(LogLevel.Warning, "An outbox cycle failed. The next cycle will try again.")]
    private partial void LogCycleFailed(Exception failure);
}
```

**The `WorkSignal` contract.** A signal is a *hint*, never a queue: what is worth doing is already a
row. A signal lost with the process that raised it costs one interval, not the work. This is why the
signal may be an in-memory `SemaphoreSlim` and why the periodic interval must never be removed.

**Rules for a handler:**

- **Idempotent by construction.** The handler passes the message's `IdempotencyKey` to the provider
  (Campay `external_reference`, Sudo request id) and to `WalletService.AppendAsync`, so replaying it
  after a crash is a no-op — see [data-money-ledger](data-money-ledger.md).
- **Claim with a lease.** A message is taken with `SELECT ... FOR UPDATE SKIP LOCKED` and a
  `lease_until`, so two instances never work the same message and a crashed instance releases it.
- **Bounded retries with backoff** (`1 min`, `5 min`, `15 min`), then `Failed` with `last_error`
  recorded. Infinite retry against a provider that answers 400 is a busy loop, not resilience.
- **Poll, then trust the webhook.** Campay is polled today; a webhook, when it lands, feeds the same
  idempotent handler, so a callback arriving after the poll already settled changes nothing.
- **Every worker is gated by its options** (`Enabled`, `PollInterval`, `ValidateOnStart`). The test
  host turns the worker off and drives `RunCycleAsync` directly, which is deterministic; a test that
  races a live loop is a flake generator.
- **Nothing long-running on the request thread.** An endpoint returns `202 Accepted` with the
  pending resource, and the app polls `GET /topups/{id}`.

Reference: [Transactional outbox pattern](https://learn.microsoft.com/en-us/azure/architecture/best-practices/transactional-outbox-cosmos) ·
[BackgroundService](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers)
