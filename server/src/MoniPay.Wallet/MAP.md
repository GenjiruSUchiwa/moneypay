# MoniPay.Wallet

The FCFA ledger: the balance, the holds placed by accepted authorizations, and the just-in-time
decision to accept or refuse one. The wallet is the single source of truth for what the user can
spend — a card has no balance of its own.

- `WalletModule.cs` — `AddWalletModule(services, configuration)`.
- `Endpoints/WalletEndpoints.cs` — `MapWalletEndpoints`. `GET /wallet` today, a stub until the
  ledger lands. Every route, name, tag and summary comes from a constant beside it:
  `WalletRoutes`, `WalletEndpointNames`, `WalletTags`, `WalletSummaries`.
- `Contracts/WalletResponse.cs` — the wire shape: currency plus balance, held and available in
  minor units.
- `Domain/WalletBalance.cs` — balance minus held is available. An authorization compares against
  available, never against the balance.
- `Resources/WalletMessages[.fr].resx` + `WalletMessageKeys.cs` — the text a user reads. Neutral
  is English, `.fr` is what a Cameroonian client gets. Only refusals are localized; an
  OpenAPI summary is developer-facing and stays a plain constant.
- `Persistence/` — entity configurations for the wallet's tables. Empty until the first one.

References `Kernel`, `Data`.
