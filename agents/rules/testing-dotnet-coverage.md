---
title: Test the Money, the Contract, and the Provider Edges
impact: HIGH
impactDescription: Coverage spent on the ledger and the API surface is what stops a francs-losing regression
tags: testing, dotnet, coverage, naming, fakes, http-handler
---

## Test the Money, the Contract, and the Provider Edges

**Impact: HIGH**

Coverage percentage is a weak goal; *what* is covered is the real one. In a wallet, three areas earn
a test before anything else, and a PR that touches one without a test is incomplete.

**1. Ledger and FX arithmetic — exhaustively.** This is pure, fast, and the most expensive thing to
get wrong. Cover the boundaries: zero, one franc, the Campay 25 XAF cap, insufficient balance by
exactly one franc, rounding direction, a reversal restoring the balance, a replayed idempotency key.

```csharp
// server/tests/MoniPay.Tests/Fx/UsdToXafTests.cs
public sealed class UsdToXafTests
{
    [Theory]
    [InlineData(2_500, 15_700)]   // $25.00 at 628 XAF/USD
    [InlineData(1, 7)]            // one cent still rounds up to a whole franc, in MoniPay's favour
    [InlineData(0, 0)]
    public void Converts_cents_to_whole_francs_rounding_up(long cents, long expectedXaf)
    {
        var rate = new FxRate(usdToXaf: 610m, margin: 0.03m);

        Assert.Equal(expectedXaf, rate.ToXaf(cents));
    }

    [Fact]
    public void Refuses_a_negative_amount()
    {
        var rate = new FxRate(usdToXaf: 610m, margin: 0.03m);

        Assert.Throws<ArgumentOutOfRangeException>(() => rate.ToXaf(-1));
    }
}
```

**2. Endpoint contracts — every route, both outcomes.** Status code, body shape, and the
authorisation rule. These run against the real host, so they also prove the migration, the
serialiser, and the `ProblemDetails` mapping.

```csharp
[Fact]
public async Task Issuing_a_card_without_funds_answers_402_with_a_stable_type()
{
    var user = await Api.SignUpAsync();                     // no top-up

    // The route constant, not a literal: a rename must break this test at compile time.
    var response = await Api.CreateClient()
        .PostAsJsonAsync(CardRoutes.Group, new { amountUsdCents = 2_500 });

    Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    Assert.Equal(MoniPayErrorTypes.InsufficientFunds, problem!.Type);
}
```

**3. Provider clients — against a fake `HttpMessageHandler`, not the network.** Campay and Sudo both
answer in ways that need mapping: Sudo returns HTTP 200 with `{ "statusCode": 400 }` in the body, and
Campay refuses above 25 XAF in the demo environment. Those quirks are the reason the client exists,
so they are what the test asserts.

```csharp
// server/tests/MoniPay.Tests/Cards/SudoCardIssuerTests.cs
[Fact]
public async Task Maps_a_200_with_an_error_body_to_a_declined_outcome()
{
    var handler = new StubHandler(HttpStatusCode.OK, """{"statusCode":400,"message":"insufficient funding"}""");
    var issuer = new SudoCardIssuer(new HttpClient(handler) { BaseAddress = new Uri("https://sudo.test/") },
        Options.Create(new SudoOptions { ApiKey = "test" }));

    var failure = await Assert.ThrowsAsync<ProviderDeclinedException>(
        () => issuer.IssueAsync(new CardOrder(userId, 2_500, "key-1"), TestContext.Current.CancellationToken));

    Assert.Equal("insufficient funding", failure.Reason);
}
```

**What not to test:** EF Core itself, `Microsoft.Extensions.*`, generated OpenAPI plumbing, and
trivial `record` factories. A test that restates the implementation line for line locks the code in
place without proving anything.

**Naming.** A test name is a sentence about behaviour, in `Snake_case_after_the_first_word`:

```
A_replayed_campay_callback_credits_the_wallet_once
Issuing_a_card_debits_the_wallet_before_calling_sudo
A_top_up_above_the_provider_cap_is_refused
```

Not `TestTopUp1`, not `WalletService_AppendAsync_Works`. The class is named after what it covers —
`TopUpIdempotencyTests` — and lives in the module folder.

**No mocking framework.** There is no Moq, no NSubstitute, and none is to be added. Behaviour is
verified through observable outcomes: a balance, a status code, a row. Where a collaborator must be
replaced, write a hand-rolled fake in `Fakes/` that implements the port and records what it was
asked — it reads better, survives refactors, and cannot assert an interaction that does not matter.
See [testing-xunit-testcontainers](testing-xunit-testcontainers.md) for the host and the fakes.

**Coverage expectations:** near-total on `Fx`, `Wallet`, and the outbox handlers — this is money.
Every endpoint has at least a happy path and its principal refusal. New code arrives with its test in
the same PR; "tests in a follow-up" means untested. Run the suite before pushing:

```bash
dotnet test server/MoniPay.slnx
```

Reference: [Unit testing best practices in .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
