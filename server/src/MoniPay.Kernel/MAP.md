# MoniPay.Kernel

Primitives shared by every module, and nothing else. No business rule lives here: if a type
knows what a top-up is, it belongs in `MoniPay.TopUps`, not in the kernel.

- `Money.cs` — an amount in minor units plus its currency. Addition and subtraction refuse to
  mix currencies. This is the type that keeps `double` out of the codebase.
- `Currency.cs` — `Xaf` (zero minor units) and `Usd` (two), with `MinorUnitScale()`.
- `KernelModule.cs` — registers `TimeProvider`, so no module reads the clock directly and a
  test can substitute `FakeTimeProvider`.

- `Http/` — JSON:API 1.1 envelope records (`JsonApiRequest`, `JsonApiResponse` and the
  resource, relationship, version and link types), plus `MoniPayMediaTypes`, `MoniPayHeaders`
  and `MoniPayConventions`. Transport names only: no domain rule and no ASP.NET Core
  dependency. Each slice still owns its resource and attribute records.

References nothing.
