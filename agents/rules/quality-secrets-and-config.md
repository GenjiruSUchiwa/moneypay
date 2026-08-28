---
title: Secrets Live in User-Secrets and Environment Variables, Never in Git
impact: CRITICAL
impactDescription: A leaked Campay or Sudo key is money moving out of accounts that are not yours
tags: quality, secrets, configuration, user-secrets, encryption, campay, sudo
---

## Secrets Live in User-Secrets and Environment Variables, Never in Git

**Impact: CRITICAL**

MoniPay holds provider credentials that move real money — Campay collects from a customer's MoMo
wallet, Sudo issues USD cards — plus a signing key that mints session tokens and an encryption key
that protects stored provider tokens. None of them may ever appear in a tracked file. `poc/.env` is
git-ignored for exactly this reason, and the .NET server keeps that discipline with better tools.

**Local development: `dotnet user-secrets`.** The values are stored outside the repository, so there
is no file to accidentally `git add`.

```bash
dotnet user-secrets set "ConnectionStrings:MoniPay" \
  "Host=localhost;Port=5432;Database=monipay;Username=monipay;Password=<local password>" \
  --project server/src/MoniPay.Api
dotnet user-secrets set "MoniPay:Encryption:KeyBase64" "$(openssl rand -base64 32)" \
  --project server/src/MoniPay.Api
dotnet user-secrets set "MoniPay:Sessions:SigningKeyBase64" "$(openssl rand -base64 32)" \
  --project server/src/MoniPay.Api
dotnet user-secrets set "MoniPay:Campay:AppPassword" "<campay demo password>" \
  --project server/src/MoniPay.Api
```

**Everywhere else: environment variables**, with `__` between the configuration parts:

```
ConnectionStrings__MoniPay=Host=…;Database=monipay;Username=monipay;Password=…
MoniPay__Encryption__KeyBase64=…
MoniPay__Sessions__SigningKeyBase64=…
MoniPay__Campay__AppUsername=…
MoniPay__Campay__AppPassword=…
MoniPay__Sudo__ApiKey=…
```

**Incorrect (a real value committed, and a fallback that hides its absence):**

```json
// appsettings.json — tracked, therefore public the moment the repo is cloned
{
  "MoniPay": {
    "Sudo": { "ApiKey": "sk_live_9f3c…" },
    "Campay": { "AppPassword": "hunter2" }
  }
}
```

```csharp
var apiKey = configuration["MoniPay:Sudo:ApiKey"] ?? "test-key";   // boots, then fails per request
```

**Correct (empty defaults, validated at startup):**

```json
// server/src/MoniPay.Api/appsettings.json — the shape is public, the values are not
{
  "MoniPay": {
    "ApplyMigrationsOnStartup": false,
    "Campay": { "BaseUrl": "https://demo.campay.net/api/", "AppUsername": "", "AppPassword": "", "MaximumXaf": 25 },
    "Sudo": { "BaseUrl": "https://api.sandbox.sudo.africa/", "ApiKey": "" },
    "Encryption": { "KeyBase64": "" }
  }
}
```

```csharp
// The host refuses to start without them, so a missing secret is a deploy failure, never a
// customer-facing one.
services.AddOptions<SudoOptions>()
    .Bind(configuration.GetSection(SudoOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "MoniPay:Sudo:ApiKey is required.")
    .ValidateOnStart();

services.AddOptions<EncryptionOptions>()
    .Bind(configuration.GetSection(EncryptionOptions.SectionName))
    .Validate(options => Convert.TryFromBase64String(options.KeyBase64, new byte[32], out var written) && written == 32,
        "MoniPay:Encryption:KeyBase64 must decode to 32 bytes.")
    .ValidateOnStart();
```

**Rules:**

- **A key with an empty default in `appsettings.json` is a secret.** Never fill one in, in any
  environment file, including `appsettings.Development.json`.
- **Never a fallback default for a secret.** No `?? "dev-key"`, no `#if DEBUG` constant. Absence must
  stop the host.
- **Provider tokens stored in the database are encrypted at rest** with AES-GCM through the
  `ISecretCipher` in `MoniPay.Kernel`, keyed by `MoniPay:Encryption:KeyBase64` — a Sudo customer
  token or a MoMo authorisation is never a plaintext column.
- **Never log a secret, a PAN, a CVV, an OTP, or a full phone number.** Log the last four digits and
  the provider reference; that is enough to support a customer.
- **Test keys are obviously test keys.** `MoniPay.Tests` uses fixed 32-byte ASCII strings whose value
  is irrelevant and whose length is not — never a copy of a real one.
- **A leaked key is rotated, not deleted from history.** Revoke at the provider first, issue a new
  one, then clean the repository. `git rm` alone changes nothing about a key that was pushed.
- **CI reads secrets from the GitHub environment**, never from a file in the repo, and the workflows
  never `echo` one.

Reference: [Safe storage of app secrets in development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) ·
[Configuration providers in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers)
