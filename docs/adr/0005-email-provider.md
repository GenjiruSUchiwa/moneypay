# ADR 0005: Bird sends the email channel

Status: Approved
Date: 2026-09-10
Scope: `MoniPay.Notifications`, `MoniPay.Api`
Docs checked: 2026-09-10

## Context

Gate 6 needs an email provider for the welcome message and the security alerts. The outbox, retry schedule, and keyed `INotificationChannel` contract already exist. The adapter must send one recipient of already-localized plain text with the outbox idempotency key, and map one HTTP attempt to one `ChannelResult`.

## Decision

Send through the Bird Email API v1 with a typed `HttpClient` in `MoniPay.Notifications`:

- `POST https://eu1.platform.bird.com/v1/email/messages` ([sending email](https://bird.com/docs/guides/email/sending-email), [API reference](https://bird.com/docs/api/reference/create-email-message))
- `Authorization: Bearer bk_eu1_...` with the narrowest send-only scope `emails:write` ([authentication](https://bird.com/docs/api/authentication), [scopes](https://bird.com/docs/guides/authentication))
- Payload allowlist: `from`, single-entry `to`, `subject`, `text`, `category: "transactional"`. No HTML, attachments, tags, metadata, lists, or scheduling.
- `category` is set explicitly because an inline send defaults to `marketing`, which applies marketing suppressions and unsubscribe handling to operational mail.
- `Idempotency-Key` carries the outbox idempotency key unchanged on every attempt ([idempotency](https://bird.com/docs/guides/idempotency)).

The eu1 region keeps the workspace, messages, and event logs in the EU; the organization region is immutable and a key only works on its own host ([regions](https://bird.com/docs/api/regions)).

## Request and success

Minimal payload:

```json
{
  "from": "hello@mail.example.com",
  "to": ["marie.ngo@example.cm"],
  "subject": "Bienvenue sur MoniPay",
  "text": "Votre compte est pret.",
  "category": "transactional"
}
```

`202 Accepted` means Bird durably accepted the send. The provider reference is the `id` field (`em_...`) of the JSON body with `status: "accepted"`. Accepted means accepted, not delivered: per-recipient outcomes arrive later through events. A suppressed recipient still returns `202`; its `rejected` state is visible only on the read endpoints or webhooks ([sending email](https://bird.com/docs/guides/email/sending-email), [suppressions](https://bird.com/docs/guides/email/suppressions)).

Field limits enforced client-side before sending: `subject` up to 998 characters, `text` up to 524288 characters, exactly one `to` recipient.

## Response mapping

Branch on the status code, then on the error `type` and `code`, never on `message` ([errors](https://bird.com/docs/guides/errors), [error codes](https://bird.com/docs/api/errors)).

| Bird response | `ChannelResult` |
|---|---|
| `202` with usable `id` | `Accepted(id)` |
| `202` without usable `id`, or unparsable body | `Retry("provider-protocol")` |
| `409 E01004` (same key still in flight) | `Retry("provider-key-in-flight")` with the same key |
| `429` (`E01003`, `E04010`, `E04015`, `E04017`, `E04067`, `E04070`) | `Retry("provider-rate-limited")`, one attempt, no sleep |
| `5xx` (`E04008`, `E01033`, others) | `Retry("provider-5xx")` |
| Transport or DNS or TLS failure | `Retry("provider-transport")` |
| Client timeout, supplied token still active | `Retry("provider-timeout")` |
| Caller cancellation of the supplied token | Propagates `OperationCanceledException` |
| `401`, `403`, `421` (wrong region) | `Rejected("provider-unauthorized")` |
| `402` (billing/quota unfunded) | `Rejected("provider-rejected")` |
| `422`, `400`, `404`, `410`, `413` (`E04006`, `E04009`, `E04011`, others) | `Rejected("provider-rejected")` |
| `409 E01005` (same key, different payload) | `Rejected("provider-key-conflict")`; never mint a new key to bypass it |
| Any `3xx` or anything else | `Rejected("provider-unexpected-status")`; redirects stay disabled |

`E01005` signals our own bug (same key, changed body). Retrying it can never succeed, and a fresh key would resend a sibling attempt already in flight.

## Idempotency and the retry schedule

Keys are any non-empty string up to 255 characters; the outbox key is capped at 128, so it always fits. A replay inside the window returns the cached `202` with `Idempotency-Replay: true` instead of a second email. Completed responses are kept for 3 hours; `5xx` and `429` never consume the key.

The default schedule retries at 1 min, 5 min, 15 min, and 1 h, all inside the window. A retry after a restart delay past 3 hours, or a crash after acceptance with a lost response, can deliver twice. Exactly-once delivery is not claimed.

## Throttling

Sends are limited organization-wide at 10 `POST /v1/email/messages` calls per minute. Every response carries `RateLimit` headers; a `429` carries `Retry-After`. The current `ChannelResult` cannot carry `Retry-After`, so the worker schedule governs the wait. At our volume (one email per registration) the limit only binds during an incident retry storm.

## Sandbox and smoke test

No toggle, test mode, or special key: sends to `messagebird.dev` run the normal accept path with magic local parts ([testing sandbox](https://bird.com/docs/guides/email/testing-sandbox)):

- `delivered@messagebird.dev` accepts and reports delivered
- `bounce@messagebird.dev` hard-bounces through the event pipeline
- `suppressed@messagebird.dev` rejects with `recipient_suppressed`
- Nothing is really delivered and nothing is written to the suppression list

Before the sending domain is verified, `onboarding@messagebird.dev` reaches workspace members and sandbox addresses only, capped at 50 recipients per organization per UTC day (`E04010` past the cap). The smoke test sends to `delivered@messagebird.dev` from the onboarding sender and proves acceptance only.

## Sending domain

Register a dedicated subdomain such as `mail` under the product domain ([sending domains](https://bird.com/docs/guides/email/sending-domains), [DKIM, SPF, and DMARC](https://bird.com/docs/guides/email/dkim-spf-dmarc)):

- DKIM TXT record; Bird generates the organization key and signs every message
- Return-path CNAME toward the regional bounce host, which also covers SPF, so no apex SPF record is needed
- A DMARC TXT policy; `p=none` is enough to start
- Registration is per region: the same domain on eu1 is a separate record set

`from.email` must sit on the verified domain or the onboarding domain; anything else is `422 E04006`.

## Bounce, complaint, and suppression ownership

Bird fills the workspace suppression list automatically from hard bounces and spam complaints; soft bounces retry without suppressing ([suppressions](https://bird.com/docs/guides/email/suppressions)). The list is managed in the dashboard `Email > Suppressions`, through the suppressions API, or the CLI. Complaint suppressions cannot be removed with an API key. Delivery, bounce, and complaint events are available through queryable events and optional Standard Webhooks endpoints ([events](https://bird.com/docs/guides/email/events), [webhooks](https://bird.com/docs/guides/webhooks)).

No webhook or inbound integration ships here. A separate delivery-event feature is the linked prerequisite before a later bounce can change a row.

## Operations

- Pricing meters per recipient; the free plan covers 1000 emails a month, Startup starts at $15 for 50000 ([pricing](https://bird.com/pricing/email)).
- Message content is stored up to 30 days when content storage is enabled; attachments the same ([sending email](https://bird.com/docs/guides/email/sending-email)).
- Key rotation issues a replacement with a 24 h grace period by default, `grace_period: 0` on leak; revocation propagates within seconds ([authentication](https://bird.com/docs/guides/authentication)).
- Open and click tracking default to `true` on the send; no tracking fields are set by the adapter.

## Approved, not yet done

- The API key and the verified `FromAddress` do not exist yet. Both remain blockers: startup validation rejects empty values, and no smoke run has happened.
- Event-log retention past 30 days is unverified in the fetched docs.
- No server-side request timeout is documented; the client `ProviderTimeout` (default 10 s) is the only bound.

## Consequences

- One recipient, one HTTP attempt, one mapped result per invocation. No retries, delays, or redirect following inside the adapter.
- A `202` marks the row `Sent` even when the recipient later bounces or turns out suppressed; that correction waits for the delivery-event feature.
- `Retry-After` is honored only as fast as the worker schedule allows.
- Logs and exceptions carry fixed codes, never the recipient, subject, body, key material, or provider payloads.

## Alternatives rejected

- MailerSend: no documented idempotency on the send endpoint, so a retry after a lost response always risks a duplicate with no replay mechanism.
- Generic SMTP relay: standard but provider-agnostic delivery reports are weaker, and sandbox behavior is less explicit than magic addresses on the same API path.
- Bulk endpoint for single sends: one message per outbox row keeps attempts, references, and retries attributable; batching would blur them.

## References

- [Email overview](https://bird.com/docs/guides/email/overview)
- [Sending email](https://bird.com/docs/guides/email/sending-email)
- [Create email message reference](https://bird.com/docs/api/reference/create-email-message)
- [Idempotency](https://bird.com/docs/guides/idempotency)
- [Rate limits](https://bird.com/docs/guides/rate-limits)
- [Errors](https://bird.com/docs/guides/errors) and [error codes](https://bird.com/docs/api/errors)
- [Testing sandbox](https://bird.com/docs/guides/email/testing-sandbox)
- [Sending domains](https://bird.com/docs/guides/email/sending-domains)
- [Suppressions](https://bird.com/docs/guides/email/suppressions)
- [Events](https://bird.com/docs/guides/email/events)
- [Regions](https://bird.com/docs/api/regions)
- [Authentication](https://bird.com/docs/guides/authentication)
- [Pricing](https://bird.com/pricing/email)
- Issue #102
