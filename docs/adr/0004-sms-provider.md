# 0004. SMS provider: Bird

Date: 2026-09-10
Status: Accepted
Owner approval: repository owner selected Bird for the SMS channel (issue #101).

## Decision

Send SMS through Bird (bird.com), `POST {region-host}/v1/sms/messages` on the
unified Bird API, as the `NotificationChannel.Sms` typed `HttpClient` adapter
`BirdSmsChannel`. No provider SDK.

Official references checked 2026-09-10:

- Send endpoint, request fields, `202` semantics, `422`/`402` rules:
  <https://bird.com/docs/api/reference/create-sms-message>
- API conventions (regional base URLs, bearer auth, snake_case JSON, typed IDs,
  error envelope, `Idempotency-Key`): <https://docs.bird.com/api/sms>
- Machine-readable contract (Postman collection generated from the same OpenAPI
  spec as the reference): <https://bird.com/postman/bird-api.postman_collection.json>
- Destination coverage and per-segment pricing:
  <https://bird.com/fr-ch/tarifs/sms>

## Coverage

Cameroon (CM) is covered with an alphanumeric sender at $0.23 per segment, as is
the rest of CEMAC (CF, TD, CG, GQ, GA). A rejected alternative, MailerSend SMS,
accepts only US/CA recipients
(<https://developers.mailersend.com/api/v1/sms>, `to.*` limited to `US, CA`)
and cannot serve +237 numbers.

## Operations

- Submission: `POST https://eu1.platform.bird.com/v1/sms/messages`, API version
  `v1`, JSON body, `Authorization: Bearer <workspace-key>`. Region `eu1` keeps
  submission data in the EU region.
- Sender: a workspace-owned number, short code, or claimed alphanumeric sender
  ID. The ID must also be permitted, and where required registered, for the
  destination country. Sender setup is account-specific and lives outside code.
- Body: free text with `category: authentication` for verification codes. That
  category additionally redacts the retained body (`**REDACTED**`) on later API
  reads. Bird does not truncate: bodies over 12 segments are rejected with 422.
  `smart_encoding` stays off so the localized body, including French accents and
  leading-zero codes, is sent unchanged; accented bodies travel as UCS-2.
- Success: `202` with a message `id` (`sms_` + ULID, about 30 characters). It
  proves Bird durably accepted the submission, not handset delivery.
- Delivery receipts: `sms.*` webhook events and the get-message endpoint exist;
  receipt processing is out of scope for #101.
- Rate limits surface as `429`; a `503` may carry `Retry-After`. The worker
  schedule cannot honor an arbitrary delay (limitation below).
- Pricing is per segment; Unicode bodies segment faster. Wallet balance funds
  sends; a `402` means operations must top up.
- Sandbox: Bird issues a test API key instantly; production unlocks once a
  sender is verified. The smoke check uses sandbox credentials and is recorded
  below.
- Secret rotation: workspace API keys are managed in the Bird dashboard and
  supplied to the API only through user-secrets or environment variables.

## Compatibility answers

1. Deduplication is real: the `Idempotency-Key` header replays the retained
   response for the same key within the idempotency window (3 hours by default).
   A concurrent duplicate returns `409 request_in_progress` (E01004); a
   completed key against a different body returns `409 idempotency_key_reuse`
   (E01005). Every worker retry schedule (verification: 5s/30s/2min; default:
   max 1h) fits inside the window, so a retry after a lost response replays the
   original acceptance instead of sending twice. Crash recovery past the window
   could duplicate; a duplicated verification SMS only resends the same code,
   which the owner accepts as a known ceiling.
2. `Retry-After` is approximated, not honored exactly: `ChannelResult.Retry`
   carries only a code, and changing the worker schedule is out of scope. The
   worker delays (5s to 1h) approximate backoff; the same idempotency key is
   reused, which the endpoint explicitly recommends. Accepted as a known
   ceiling by the owner.
3. The provider reference fits: `sms_`-prefixed IDs are about 30 characters
   against the 128-character column. Overlong references are rejected as
   protocol errors, never truncated or persisted.

## Outcome mapping

| Provider outcome | Internal result |
|---|---|
| `202` with a usable `sms_` reference | `Accepted(reference)` |
| `202` with missing, foreign, overlong, or unreadable reference | `Retry(sms-protocol-error)` |
| `400`, generic or unknown `422` | `Rejected(sms-rejected)` |
| `422 SMSInvalidRecipient` | `Rejected(sms-invalid-recipient)` |
| `422 SMSSenderNotConfigured`, `SMSNoEligibleSender`, `SenderCategoryNotPermitted` | `Rejected(sms-sender-rejected)` |
| `401`, `403` | `Retry(sms-unauthorized)`; a rotated or expired key must not destroy the pending rows, which resume once the key is fixed |
| `402` insufficient balance | `Retry(sms-insufficient-balance)`; funding is operations-side, the code may expire meanwhile |
| `409 request_in_progress` | `Retry(sms-duplicate-inflight)`; the attempt lock expires within 30 seconds |
| Other `409` | `Retry(sms-protocol-error)`; unreachable by construction since the key is the row identity |
| `429` | `Retry(sms-rate-limited)` |
| `5xx`, including `503 IdempotencyUnavailable` | `Retry(sms-unavailable)` |
| Transport or body-read failure with a live token | `Retry(sms-transport-error)`; the response may be lost after acceptance, the same key replays it |
| Processor deadline or caller shutdown | `OperationCanceledException` propagates; the processor records `provider-timeout` |
| `3xx` or any undocumented status | `Retry(sms-protocol-error)`; redirects are never followed |

Error codes stay within the 64-character column and never carry provider text.
The adapter logs only allowlisted codes, joined to the notification id, with the elapsed time and, on a transport retry, the failure.

## Sandbox smoke

Unchecked: no Bird workspace credentials are available in this change. Run
`scripts/bird-sms-smoke.sh` with sandbox credentials and a bounded timeout,
then record the date, sandbox outcome, and whether Bird simulated acceptance
or confirmed handset delivery. Never publish credentials, the recipient, or
the message body.
