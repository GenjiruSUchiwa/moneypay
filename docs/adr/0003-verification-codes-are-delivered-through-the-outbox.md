# ADR 0003: Verification codes are delivered through the outbox

Status: Proposed
Date: 2026-08-29
Scope: `MoniPay.Sessions`, `MoniPay.Notifications`, `MoniPay.Api`

## Context

Starting a sign-up must send an SMS with the verification code. The first draft called the SMS provider inside the request after committing the sign-up row and returned `503` when the provider refused.

The repository rule for provider calls is record first, call the provider later. A provider call on the request thread blocks a Kestrel worker for the provider timeout, and a crash between the commit and the call loses the message with no retry.

The code is time-boxed: a message delivered after the code expired is worse than no message.

## Decision

`POST /signups` writes the sign-up row and a `notifications` row in one transaction and returns `202 Accepted`. A background worker in `MoniPay.Notifications` claims the row, calls the SMS provider, and records the result. The worker wakes on an in-process signal after the commit, so a healthy provider still receives the message within milliseconds.

The message carries `ExpiresAt`. The worker marks an expired message `Expired` instead of sending it. Retries for a verification code follow a short schedule: 5 seconds, 30 seconds, 2 minutes, then `Failed`.

`GET /signups/{signUpId}` exposes `codeDelivery` so the client can distinguish a slow network from a failed delivery before it offers a resend.

`MoniPay.Sessions` writes the localized message text and enqueues it through a port. `MoniPay.Notifications` owns delivery only. A host adapter connects the two, because modules never reference each other.

## Consequences

- The request thread never waits for a provider. Provider latency and outages do not change the API's response time.
- No message is lost with a process; every message is a row until it is sent, failed, or expired.
- The client cannot know from `POST /signups` alone whether the SMS was accepted. It polls `GET /signups/{signUpId}`.
- The SMS body contains the code, so the row is encrypted and the body is cleared the moment the provider accepts it.
- Several replicas can run the worker safely through `SKIP LOCKED` leases.
- One more module, one more table, one more worker. That cost is accepted because the same module carries every later message: welcome email, security alerts, receipts.

## Alternatives rejected

- Provider call in the request with `503` on failure: blocks workers, loses messages on a crash, and couples the API's availability to the provider's.
- Fire-and-forget `Task.Run` after the response: loses messages on a crash and has no retry or observability.
- A message broker: a second piece of infrastructure for a queue PostgreSQL already provides at this scale.

## References

- [Notification architecture](../architecture/authentication/notifications.md)
- `agents/rules/patterns-outbox-and-background-work.md`
