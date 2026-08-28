# Knowledge Base — Domain & Business Rules

Domain knowledge for MoniPay. For coding guidelines see [`rules/`](rules/); for commands see
[`commands.md`](commands.md).

Everything here is sourced from the handoffs in `docs/handoff/`, `docs/design/`,
`poc/README.md`, `poc/server.js` and the Swift sources under `ios/`. Anything not
established by those files is marked **TODO** — do not invent a business rule to fill a gap.

These rules are shared by both components. The Node POC in `poc/` implements them today and
defines the current API contract; the .NET server in `server/` will take them over, one
`MoniPay.<Module>` project per area:

| Domain area | Backend module | iOS package |
|---|---|---|
| Money, currencies, FX | `MoniPay.Fx` | `Money` |
| FCFA balance, holds, authorization | `MoniPay.Wallet` | `WalletStore` |
| MoMo top-ups (Campay) | `MoniPay.TopUps` | `TopUp` |
| USD card issuing (Sudo Africa) | `MoniPay.Cards` | `Cards` |
| Transaction history | `MoniPay.Transactions` | `Transactions` |
| Identity verification | `MoniPay.Kyc` | `KYC` |
| Accounts, auth / PIN | `MoniPay.Users`, `MoniPay.Sessions` | `Onboarding`, `Settings` |

## Product in one paragraph

MoniPay is a fintech wallet for Cameroon / the CEMAC zone. A user tops up an **FCFA (XAF)
balance** from MTN Mobile Money or Orange Money, then spends it abroad through **USD virtual
Visa / Mastercard cards**. The wallet is the single source of truth for the balance; the cards
have no balance of their own. All user-facing text is French.

**Naming**: the product is **MoniPay**, and so are the Xcode project, the bundle identifier
(`com.monipay.app`) and the log subsystem. Only the repository folder still says *MoneyPay*,
along with some older user-facing copy in the feature catalogs — rename that copy in its own PR,
never as a side effect.

## Currencies

| Currency | ISO | Decimals | In-app representation | Display |
|---|---|---|---|---|
| Franc CFA (Afrique centrale) | XAF | **0** | `Int`, whole francs | `428 500 FCFA` |
| US dollar | USD | **2** | `Int`, **cents** | `$25.00` |

Rules:

- XAF has **no minor unit**. One `Int` unit is one franc. There is no such thing as half a franc;
  never introduce a scaling factor of 100 for XAF.
- USD amounts are always stored in cents (`amountUSDCents`, `monthlyLimitUSDCents`,
  `spentUSDCents`).
- Helpers live in the `Money` package (`ios/Packages/Money/`): `Int.xafLabel` → `"428500 FCFA"`,
  `Int.usdLabel` → `"$25.00"`. **TODO**: neither helper groups thousands; the prototype displays
  grouped digits with narrow no-break spaces. Formatting is a presentation concern and belongs in
  the design system, not in `Money`.
- Never use `Double` or `Float` for an amount, a fee, a limit or a balance. On the backend the
  same amounts are `decimal` plus an ISO currency code, stored in minor units.

## FX conversion

Two hardcoded rates exist today and they **do not match** — this is a known POC inconsistency:

| Source | Rate | Margin | Rounding |
|---|---|---|---|
| App (`Money` package, `FXRate`) | `usdToXAF = 610.0` | `marginPct = 0.03` (3 %) | `.rounded(.up)` — up to the franc |
| POC backend (`poc/server.js`) | `RATE_FCFA_PER_USD = 628` | none, baked into the rate | `Math.ceil` |

- The app formula is `xaf = ceil(cents / 100 × rate × (1 + margin))`. The FCFA amount is always
  rounded **up**: the business must not lose money on rounding.
- The FX margin (2–4 %) is where the business actually makes its money, **not** card fees. It is a
  deliberate calibration knob.
- `FXRate` currently stores `usdToXAF` and `marginPct` as `Double`. This predates the
  no-`Double`-for-money rule; new code must use `Decimal` and this type is expected to migrate.
- The EUR/XAF leg is a **fixed peg** managed by the BEAC (see the comment in `Money.swift`); the
  USD/XAF leg floats and must come from an FX provider. **TODO**: no rate feed, no refresh
  policy, no rate-staleness rule is defined yet — both rates are hardcoded on purpose for the POC.

## Top-up (« Recharger ») — MTN MoMo / Orange Money via Campay

Provider: **Campay** demo environment (`https://demo.campay.net/api`), chosen for Cameroon
(MTN + Orange) with a three-endpoint API. Notch Pay is the accepted plan B; pawaPay is the
option if the product goes multi-country.

Flow, as implemented in `poc/server.js` (`POST /topup`):

1. Authenticate: `POST /token/` with app username + password → a token used as
   `Authorization: Token <token>`.
2. Collect: `POST /collect/` with `amount` (string), `currency: "XAF"`, `from` (the payer's
   phone), `description`, `external_reference`. This triggers a **USSD push on the user's
   phone**, which they must approve.
3. Poll: `GET /transaction/{reference}/` every 2 s, up to 30 times (**60 s maximum**), until the
   status leaves `PENDING`. Only `SUCCESSFUL` credits the balance; anything else is a failure and
   the provider message is surfaced to the user.
4. Credit: the balance increases by the full requested amount.

Limits and constraints:

- **Campay demo caps a collection at 25 XAF.** Live-mode tests must use `amount_fcfa: 25`.
- Minimum top-up in the prototype: **25 F in live mode** (the sandbox cap), **1 000 F otherwise**.
- Polling is used instead of webhooks. This is an assumed POC limitation; webhooks are on the
  shortlist of next steps but nothing is committed.
- The prototype also exposes `POST /simtopup`, a sandbox-only credit with no MoMo call. It exists
  because the 25 F cap makes it impossible to fund a card through a real collection. Never use it
  outside sandbox demos.
- In mock mode the operator is guessed from the phone prefix (`23769…` → Orange, otherwise MTN).
  That heuristic is **mock-only** and must not be treated as a real operator lookup.

Top-up methods shown in the app (`SampleData` in the `WalletStore` package) — **sample data for
the prototype, not confirmed pricing** (**TODO**: real fee schedule):

| Method | Fee | Settlement |
|---|---|---|
| MTN Mobile Money | 1.5 % | instant |
| Orange Money | 1.5 % | instant |
| Virement bancaire (Afriland First Bank) | 0 % | 1–2 days |
| Agent MoniPay (cash deposit) | 2 % | instant |

## Card issuance — USD virtual cards via Sudo Africa

Provider: **Sudo Africa** (`https://api.sandbox.sudo.africa`, dashboard `app.sudo.africa`),
chosen because the sandbox is instant and needs no KYB file. Maplerad and Bridgecard were
rejected: both demand a KYB dossier at sign-up.

Issuance sequence (`POST /card` in `poc/server.js`):

1. **Customer** — `POST /customers` with `type: "individual"`, name, phone, email, a
   `billingAddress`, and an `individual` block carrying `dob` **and** `identity`. The sandbox uses
   the test BVN `22222222222`; production requires real KYC identity data.
2. **USD wallet account** — `POST /accounts` with `type: "wallet"`, `accountType: "Current"`,
   `currency: "USD"`, `customerId`. One USD account per customer; the card debits it.
3. **Funding source** — `GET /fundingsources`, take the account default. If the account has none,
   one must be created in the dashboard (Card Programs / Funding Sources); there is no API
   fallback.
4. **Fund the account (sandbox only)** — `POST /accounts/simulator/fund` with `amount + 2` USD:
   issuance charges a fee **on top of** the card amount, so the wallet must hold more than the
   card amount.
5. **Create the card** — `POST /cards` with `customerId`, `fundingSourceId`, `debitAccountId`,
   `brand: "Visa"`, `type: "virtual"`, `currency: "USD"`, `status: "active"`,
   `issuerCountry: "USA"`, `amount` (whole dollars).

Rules and traps (learned by iterating against the real sandbox — do not rediscover them):

- **Minimum card amount: 3 USD.** The prototype defaults to 5 USD.
- The FCFA cost is debited **before** the card call: `ceil(amountUSD × 628)`. Insufficient balance
  fails with the required amount and the current balance.
- Re-running `/card` for a user who already has one **funds the existing card**; it never issues a
  second one.
- The `account._id` returned by `/cards` differs from the `debitAccountId` that was sent. Keep the
  returned value for later funding calls.
- Sudo sometimes answers HTTP 200 with a body containing `{ statusCode: 400 }`, and sometimes
  wraps the payload in `data`. Always read `r.data || r` and check the expected field is present.
- At authorization time the card draws from the account's **default funding source** — there is no
  per-card balance. Purchases are simulated from the dashboard (Simulator).
- Card art, labels and collections (« Originales », « Héritage » — ndop / bogolan / wax) are a
  design concern; see `CardArt` in the `DesignSystem` package and `docs/handoff/`.

## Wallet ledger and card authorization

The `Wallet` type in the `WalletStore` package is the authoritative model, implemented as an
`actor`.

- The wallet holds the balance. A virtual card has **no account of its own**: on every purchase the
  processor asks us, by webhook, whether to approve — **just-in-time funding**, with roughly
  **2 seconds** to answer.
- State: `balanceXAF` plus `holds` (`authId` → frozen FCFA). **Available = balance − holds.**
- `authorize(authId:amountUSDCents:spendLimitUSDCents:)` freezes funds and is **idempotent**: the
  network replays messages, so the same `authId` returns the same decision without re-freezing.
- `capture(authId:finalUSDCents:)` debits the final amount, which may differ from the authorized
  one (tip, fuel adjustment). `void(authId:)` releases the hold.
- Decision values (`AuthDecision`): `APPROVE`, `DECLINE_INSUFFICIENT_FUNDS`,
  `DECLINE_CARD_BLOCKED`, `DECLINE_SPEND_LIMIT`.
- Each decline is billed by the processor (~0.3–0.5 USD with African providers). After
  **3 consecutive declines** the card is blocked (`maxConsecutiveDeclines`, configurable). A
  successful authorization resets the counter; `unblock()` clears both.

## Card controls

From `VirtualCard` (`Money` package): freeze (`isFrozen`), monthly limit
(`monthlyLimitUSDCents`, `nil` = none), spend to date (`spentUSDCents`, drives `usage`), single
use (`singleUse`), online payments (`onlineAllowed`), subscriptions (`subscriptionsAllowed`),
consecutive declines (`declineCount`). The PAN is only ever shown masked
(`•••• •••• •••• 1234`) outside the explicit reveal action.

## KYC

- The app has a four-step flow (`KYC` package, `KYCFlow`): intro → document type →
  document capture → selfie → review. It can be skipped (`onLater` / `onSkip`) and the user then
  lands in the main app.
- The domain models KYC as a single boolean: `User.kycVerified`.
- **The POC has no KYC at all** — this is an explicitly assumed limitation (`poc/README.md`). The
  Sudo sandbox is satisfied with a test identity (BVN `22222222222`) and a hardcoded date of
  birth.
- **TODO**: real KYC states (not started / pending / under review / verified / rejected /
  expired), accepted document types, per-state limits, re-verification triggers, and which
  operations require a verified user are **not defined anywhere in the repository**. Do not guess
  them.

## Transactions

Case names are English, as all identifiers are; the French text in parentheses is the **displayed
translation**, which lives in the localization catalogs and never in code.

Kinds (`TxKind`): `payment` (Paiement carte), `topUp` (Rechargement), `conversion` (Conversion),
`refund` (Remboursement), `fee` (Frais).

Statuses (`TxStatus`): `approved` (Réussi), `pending` (En attente), `declined` (Refusé),
`refunded` (Remboursé).

Categories (`TxCategory`, 8 values, fixed colour slot per category so the colour-blind separation
never drifts): `streaming` (Divertissement), `software` (Logiciels), `shopping` (Achats),
`transport` (Transport), `food` (Restauration), `ads` (Publicité), `travel` (Voyage), `other`
(Autre — takes the reserved neutral colour).

A `Transaction` carries **both** legs of the money: `amountUSDCents` (what the merchant
presented) and `amountXAF` (what actually moved on the wallet), plus the `fxRate` and
`fxMarginPct` that were applied. Never recompute a historical conversion from today's rate —
store and read the rate that was used. Sign convention: negative means debit; `isCredit` is
`amountXAF > 0`.

Analytics conventions (`Store` in `WalletStore`, and the prototype's Analyse screen): declined transactions
are excluded from spend; the default window is 30 days; transactions are grouped by day, newest
first; periods are Semaine / Mois / Année; grouping is by category, merchant or card.

## Glossary FR / EN

The right-hand column is what you write in code; the left-hand column is what the user reads.
Never let a French term reach an identifier, a comment or a log line.

| French (UI) | English (code) | Meaning |
|---|---|---|
| Solde | balance | FCFA wallet balance |
| Disponible | available | balance minus holds |
| Recharger / Rechargement | top up / topUp | credit the wallet from MoMo, bank or agent |
| Carte virtuelle | virtual card | USD card issued by Sudo Africa |
| Nouvelle carte | new card | card issuance flow |
| Geler / Dégeler | freeze / unfreeze | temporarily block a card |
| Plafond | limit | monthly spend limit, in USD cents |
| Convertir / Conversion | convert / conversion | FCFA ↔ USD exchange |
| Taux de change | FX rate | XAF per USD |
| Marge | margin | percentage added on top of the FX rate |
| Frais | fee | explicit fee charged to the user |
| Autorisation | authorization | just-in-time approve/decline of a purchase |
| Gel (de fonds) | hold | funds frozen by a pending authorization |
| Débit / Crédit | debit / credit | outgoing / incoming movement |
| Refusé | declined | rejected authorization |
| Remboursement | refund | money returned by a merchant |
| Paiement carte | card payment | purchase made with a virtual card |
| Activité | activity | transaction list |
| Analyse | insights | spend analytics screen |
| Profil | settings / profile | account settings |
| Bénéficiaire | recipient | **TODO** — transfer feature not specified |
| Justificatif | proof of identity | KYC document |
| Selfie | selfie | KYC liveness capture |
| Numéro | phone number | MSISDN, Cameroon format `2376XXXXXXXX` |
| Opérateur | operator | MTN or Orange |
| Push USSD | USSD push | approval prompt sent to the payer's handset |

## Known POC limitations (assumed, documented, not bugs)

Hardcoded FX rate; in-memory state lost on restart; polling instead of webhooks; no KYC; no
persistence in the Swift app either (`Store`, in `WalletStore`, holds sample data in memory). The POC validates the
end-to-end journey, not production behaviour. Do not "fix" these silently — they are product
decisions recorded in `poc/README.md` and `docs/handoff/`.

## Secrets

`poc/.env` holds `SUDO_API_KEY`, `CAMPAY_APP_USERNAME` and `CAMPAY_APP_PASSWORD`. It is
git-ignored. Never commit it, never print its values, never echo a PAN, CVV, OTP or full phone
number into logs.
