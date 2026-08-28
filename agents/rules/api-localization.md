---
title: Localize on the Server, Format on the Client
impact: HIGH
impactDescription: A server that formats money in its own culture ships "15 700,00 F" to one user and "15,700.00" to the next
tags: api, localization, i18n, culture, resx, problemdetails, notifications
---

## Localize on the Server, Format on the Client

**Impact: HIGH**

MoniPay's users read French (Cameroon); its API also answers tooling and, one day, an English
market. Two rules keep that from leaking everywhere:

1. **Data leaves the server unformatted.** A contract carries an integer amount plus a currency
   code and an ISO-8601 UTC instant. The iOS app formats them with the device locale.
2. **Prose is localized where it is produced** — a `ProblemDetails` title, an SMS, a push
   notification, an email — through `IStringLocalizer<T>` and `.resx` files owned by the module that
   sends the message.

### Request culture

Register the middleware once in the host, before anything that produces prose.

```csharp
// server/src/MoniPay.Api/Program.cs
var supportedCultures = new[] { "fr-CM", "fr", "en" };

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("fr")                 // the market MoniPay serves today
        .AddSupportedCultures(supportedCultures)    // for parsing and formatting
        .AddSupportedUICultures(supportedCultures); // for resource lookup

    // The user's stored preference wins over the header; the header is the fallback for a
    // caller with no account yet (sign-up, password reset).
    options.RequestCultureProviders.Insert(0, new UserPreferenceCultureProvider());
});

var app = builder.Build();
app.UseRequestLocalization();                       // before UseExceptionHandler and the endpoints
```

`Accept-Language: fr-CM,fr;q=0.9,en;q=0.5` resolves to `fr-CM`; an unsupported language falls back
to `fr`, never to the machine's culture.

**Store the preference.** `users.locale` is a BCP-47 tag (`fr-CM`), validated against the supported
set on write, and set from `Accept-Language` at sign-up. It is what a background worker uses when it
sends a top-up confirmation hours later, with no request in scope.

### Resources live with the module that sends the message

```
server/src/MoniPay.TopUps/
  Resources/TopUpMessages.resx        neutral fallback, English
  Resources/TopUpMessages.fr.resx     French
  Domain/TopUpNotifier.cs
```

Keys are meaning, not text: `TopUpSucceeded`, `TopUpBelowProviderCap`. Never key on the English
sentence — the day the wording changes, every translation silently falls back.

**Incorrect (prose hardcoded, money formatted with the server culture):**

```csharp
// The container runs in en-US, so a Cameroonian user is texted "15,700 FCFA" with a comma
// instead of a space, dated in the machine's time zone, and in one language for everyone.
var message = $"Top-up of {amountXaf:N0} FCFA completed on {DateTime.Now:d}";
await sms.SendAsync(user.Phone, message);
```

**Correct (localizer, explicit culture, values from the domain):**

```csharp
// server/src/MoniPay.TopUps/Domain/TopUpNotifier.cs
internal sealed class TopUpNotifier(
    IStringLocalizer<TopUpMessages> localizer,
    ISendSms sms,
    TimeProvider timeProvider)
{
    public async Task NotifySucceededAsync(User user, long amountXaf, CancellationToken cancellationToken)
    {
        // The worker has no request culture: it takes the recipient's stored locale. The
        // localizer reads CurrentUICulture, so the async flow is pointed at it explicitly.
        var culture = CultureInfo.GetCultureInfo(user.Locale);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var message = localizer["TopUpSucceeded", amountXaf.ToString("N0", culture)];

        await sms.SendAsync(user.Phone, message, cancellationToken).ConfigureAwait(false);
    }
}
```

```xml
<!-- Resources/TopUpMessages.resx — the neutral fallback, in English -->
<data name="TopUpSucceeded"><value>Top-up of {0} FCFA completed.</value></data>
<data name="TopUpBelowProviderCap"><value>This amount is above the provider limit of {0} FCFA.</value></data>
```

`Resources/TopUpMessages.fr.resx` carries the French translation of the same keys. That resource
file is the **only** place French belongs on the server.

### `ProblemDetails`: stable `type`, localized prose

The `type` URN is the machine-readable contract and is **never** translated — the iOS app branches
on it, and may localize entirely on its own. `title` and `detail` are localized for the request
culture, as a convenience for other clients.

```csharp
ProblemDetails =
{
    Status = refusal.Status,
    Type = "urn:monipay:error:" + refusal.Code,          // stable, ASCII, culture-free
    Title = localizer[refusal.Code],                     // localized
}
```

### Never format money or dates into a contract

```csharp
internal sealed record LedgerLineResponse(
    long AmountXaf,               // minor units, integer — not "15 700 F"
    string Currency,              // "XAF"
    DateTimeOffset CreatedAt);    // serialized as ISO-8601 UTC — not "28/08/2026"
```

Formatting on the server means the app cannot right-align a column, cannot switch locale without a
round trip, and cannot do arithmetic on what it received. See
[data-money-ledger](data-money-ledger.md).

**Any string conversion in code is explicit about culture.** `decimal.Parse(text,
CultureInfo.InvariantCulture)` for a provider payload, `ToString("N0", culture)` for a message.
A bare `ToString()` or `Parse()` on a number or a date is a review rejection — its behaviour depends
on the machine.

### Tests and configuration

- **Tests pin the culture.** `CultureInfo.InvariantCulture` for anything asserting a formatted
  value, and an explicit `Accept-Language` header when asserting a localized response — otherwise
  the suite passes on your Mac and fails on the runner.
- **`InvariantGlobalization` must stay `false`.** Turning it on trades a smaller image for a process
  where `fr-CM` silently resolves to the invariant culture and `.resx` lookup collapses. The Alpine
  runtime image therefore installs both `icu-libs` and `tzdata`; see
  [reference-dotnet-local-dev](reference-dotnet-local-dev.md).
- **French belongs in `.resx` and in the iOS app, never in C# source.** No French identifier, no
  French comment, no French literal outside a resource file.

Reference: [Globalization and localization in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization) ·
[.NET globalization-invariant mode](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/globalization)
