# ADR 0002: The passcode and biometrics stay on the device

Status: Proposed
Date: 2026-08-29
Scope: `ios/Packages/Onboarding`, `MoniPay.Sessions`

## Context

The prototype and the iOS sign-up flow ask the user to create a four-digit passcode and to enable Face ID. The POC sends neither to the backend, and the question of whether production should is open.

A four-digit secret has 10,000 combinations. As a backend credential it needs lockouts, hashing, and reset flows, and it still cannot resist an offline attack on a leaked digest.

## Decision

The backend never receives the passcode, a passcode digest, a biometric template, a biometric result, or a biometric-enabled flag.

The passcode unlocks the app locally. The iOS app stores it in the Keychain with device-only protection, and Face ID controls local access to that item. Backend authentication uses the session tokens issued at sign-up: a short-lived JWT access token and a rotating opaque refresh token.

A sensitive action that needs server-side confirmation later uses a device-bound signing key or the refresh route, never the passcode.

## Consequences

- The `sign_ups`, `users`, and `sessions` tables hold no passcode column. There is no passcode reset flow to design or attack.
- Losing the device means losing the local passcode; the user signs in again on the new device through phone verification. See [Sign-in and device change](../architecture/authentication/sign-in.md).
- The iOS app owns the lock screen logic entirely. A backend outage does not lock the user out of viewing cached data.
- A future step-up flow must be designed with device keys, which is more work than a passcode check on the server. That cost is accepted.

## Alternatives rejected

- Hash the passcode server-side and use it as a second factor: low entropy, a new secret to protect, and a reset flow that reduces to phone verification anyway.
- Send a biometric-success flag: unverifiable by the server and trivially forged by a modified client.

## References

- [Security and operations](../architecture/authentication/security-and-operations.md)
- [Domain and state](../architecture/authentication/domain-and-state.md)
