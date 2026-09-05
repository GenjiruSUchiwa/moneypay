# MoniPay.Kernel

Primitives shared by every module, and nothing else. No business rule lives here: if a type
knows what a top-up is, it belongs in `MoniPay.TopUps`, not in the kernel.

- `Money.cs` — an amount in minor units plus its currency. Addition and subtraction refuse to
  mix currencies. This is the type that keeps `double` out of the codebase.
- `Currency.cs` — `Xaf` (zero minor units) and `Usd` (two), with `MinorUnitScale()`.
- `KernelModule.cs` — registers `TimeProvider`, so no module reads the clock directly and a
  test can substitute `FakeTimeProvider`.

- `Security/` — the key-material primitives every module configures rather than re-implements.
  `Base64Key` validates and decodes a base64 32-byte secret, and `RequireKey` on an
  `OptionsBuilder` refuses a host whose secret is missing or short. `PersonalDataProtector` is
  AES-GCM with an authenticated one-byte key version; `LookupDigest` is HMAC-SHA256 under an
  HKDF derivation of the personal-data key with a module-specific info string. A module
  derives its own instance from its own key: the layout is shared, the key never is.
- `Http/` — JSON:API 1.1 envelope records (`JsonApiRequest`, `JsonApiResponse` and the
  resource, relationship, version and link types), plus `MoniPayMediaTypes`, `MoniPayHeaders`
  and `MoniPayConventions`. Transport names only: no domain rule and no ASP.NET Core
  dependency. Each slice still owns its resource and attribute records.
- `Validation/` — `ValidationFailure`, `ValidationFailures`, `ValidationCodes` and
  `ValidationException` for request-attribute failures. They carry stable codes and JSON Pointers,
  not user-facing text.
- `Errors/` — `ProblemType`, `MoniPayErrorTypes`, `RefusalException` and
  `ProviderUnavailableException` for expected failures shared with the host. A `ProblemType` binds a
  stable code to its HTTP status once; a refusal carries one and no user-facing message.
- `UserId.cs`, `SignUpId.cs` — typed identifiers, so a user id and a sign-up id cannot be
  swapped at a call site. `Locale.cs` — the closed list of BCP-47 tags the product speaks;
  the host's request localization reads it, so the API and the stored value agree.
- `MoniPayPolicies.cs` and `MoniPayClaimTypes.cs` — shared authorization names and JWT claim names.

References nothing.
