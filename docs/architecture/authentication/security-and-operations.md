# Sign-up security and operations

Status: Proposed

## Security decisions

### Phone verification

Generate each verification code with `RandomNumberGenerator.GetInt32`.

Format the value as six invariant digits. Never use `Random`, a timestamp, or a sequence.

Store this digest:

```text
HMAC-SHA256(verificationCodeKey, signUpId || phoneLookupHash || code)
```

Use a constant-time comparison. Remove the digest after successful verification.

The SMS message is localized from `SessionMessages.resx`. It contains the code and its lifetime. It never contains a full profile.

### Registration token

After phone verification, generate 32 random bytes. Return them as a base64url registration token.

Store only a keyed digest. Bind the digest to the sign-up identifier and purpose.

The registration authentication scheme accepts this token only on sign-up completion. A registration token cannot call user, wallet, card, or KYC routes.

### Access token

Issue a signed JWT with these claims:

- `sub`: `UserId`
- `sid`: `SessionId`
- `jti`: Unique token identifier
- `iss`: Configured issuer
- `aud`: Configured mobile API audience
- `iat`, `nbf`, and `exp`: Token times

Do not add names, phone numbers, email addresses, locale, KYC state, or customer roles.

Use a 256-bit signing key for the single monolith. Keep the key outside source control. Validate issuer, audience, signature, and lifetime.

### Refresh token

Generate 32 random bytes and encode them as base64url. Store only its SHA-256 digest.

Rotate the refresh token on every successful use. Keep the consumed digest to detect replay.

If a consumed token appears again, revoke every session in that token family. Return `refresh-token-reused`.

### Passcode and biometrics

The four-digit passcode never crosses the network. A four-digit secret has too little entropy for standalone backend authentication.

The iOS app stores the passcode with device-only Keychain protection. Face ID controls local access to that credential.

The backend never receives biometric templates, biometric results, or a biometric-enabled flag.

A later payment-confirmation design can use a device signing key. It must not send the passcode to the backend.

## Proposed defaults

These values are security defaults, not product pricing rules. Keep them in `SessionsOptions` and validate them at startup.

| Setting | Default |
|---|---:|
| Verification-code length | 6 digits |
| Verification-code lifetime | 5 minutes |
| Resend cooldown | 60 seconds |
| Maximum resends per sign-up | 3 |
| Maximum wrong-code attempts | 5 |
| Sign-up lifetime | 15 minutes |
| Registration-token lifetime | Remaining sign-up lifetime, maximum 10 minutes |
| Access-token lifetime | 10 minutes |
| Refresh-token lifetime | 30 days |
| Clock skew | 30 seconds |

Reaching the attempt limit sets `locked_until` to the sign-up expiry; the `429` carries `Retry-After` from it.

Use these configuration names:

- `MoniPay:Sessions:Issuer`
- `MoniPay:Sessions:Audience`
- `MoniPay:Sessions:SigningKeyBase64`
- `MoniPay:Sessions:VerificationCodeKeyBase64`
- `MoniPay:Sessions:PersonalDataKeyBase64`
- `MoniPay:Users:PersonalDataKeyBase64`
- `MoniPay:Sessions:VerificationCodeLifetime`
- `MoniPay:Sessions:ResendCooldown`
- `MoniPay:Sessions:MaximumResends`
- `MoniPay:Sessions:MaximumVerificationAttempts`
- `MoniPay:Sessions:SignUpLifetime`
- `MoniPay:Sessions:AccessTokenLifetime`
- `MoniPay:Sessions:RefreshTokenLifetime`

Environment variables use double underscores. Example: `MoniPay__Sessions__SigningKeyBase64`.

Do not place default secret values in `appsettings.json`.

## Rate limits

Use two layers:

1. ASP.NET Core named rate-limit policies partition anonymous traffic by remote IP.
2. The Start, Resend, and Verify handlers enforce persistent limits by keyed phone hash.

The in-process rate limiter does not protect all instances. The persistent phone limits remain authoritative after horizontal scaling.

Initial limits:

| Action | IP limit | Phone limit |
|---|---:|---:|
| Start sign-up or sign-in | 20 per hour | 5 per hour, shared |
| Resend code | 30 per hour | 3 per sign-up |
| Verify code | 60 per hour | 5 per sign-up |
| Complete sign-up | 20 per hour | One user per verified phone |
| Refresh session | 120 per hour | Rotation and replay rules apply |

Return `429 Too Many Requests` with `Retry-After`. Do not say which limit fired.

## Account-enumeration rules

Before phone verification, return the same start response for registered and unregistered phones.

One transient exception is accepted: inside the resend cooldown, a start for a phone with an in-flight sign-up answers `429` (`signup-resend-too-soon`) where a fresh or registered phone answers `202`. The window is 60 seconds and only reveals that someone started a sign-up for that phone moments ago. The enumeration-resistance test pins this behavior.

If rate limits permit, send the same verification-code message for registered and unregistered phones.

After successful phone verification, the API can return `phone-already-registered`. The caller proved control of that phone.

Do not reveal email uniqueness until phone verification succeeded.

## Personal data

Normalize phone numbers into digits-only E.164 form. Validate supported countries before storage.

Normalize email with trimmed whitespace and an invariant domain part. Preserve the address for display.

Use keyed lookup hashes because phone numbers and emails have low entropy. A plain hash does not protect them from offline enumeration.

Encrypt the original phone, email, first name, and last name. Keep encryption keys outside the database.

Do not put personal data in route values, metrics, logs, traces, exception messages, or token claims.

## Logging

Use source-generated `[LoggerMessage]` methods.

Permitted structured fields:

- `SignUpId`
- `UserId`
- `SessionId`
- Provider result code
- Stable internal refusal code
- Attempt count
- Elapsed milliseconds

Forbidden fields:

- Full phone number
- Email address
- First or last name
- Verification code
- Registration token
- Access token
- Refresh token
- Passcode
- KYC document data

Do not log request bodies for sign-up and session routes.

## Metrics

Record counters for:

- Sign-ups started
- Verification messages accepted by the provider
- Verification delivery errors
- Verification attempts
- Locked sign-ups
- Completed sign-ups
- Completion conflicts
- Session refreshes
- Refresh-token reuse
- Session revocations

Record duration histograms for SMS delivery and sign-up completion.

Use result codes as labels. Never use phone, email, user, session, or sign-up identifiers as metric labels.

## Audit records

Keep audit events for:

- Phone verified
- User created
- Legal documents accepted
- Session created
- Session refreshed
- Session revoked
- Refresh-token replay detected

Audit records identify the user or sign-up by identifier. They contain no credential value.

## Failure behavior

| Failure | Required behavior |
|---|---|
| Database write fails | No sign-up and no queued message exist. Return `503`. |
| SMS provider rejects delivery | The notification worker retries on a short schedule. `codeDelivery` reports `failed` after exhaustion. Resend stays available. |
| SMS accepted but the provider response is lost | The idempotency key prevents a duplicate on retry. A resend rotates the code after the cooldown. |
| Wrong code | Count the attempt atomically. Return a generic error. |
| Sign-up completion transaction fails | Create no user and no session. A retry remains possible. |
| Completion response is lost | A retry revokes the prior bootstrap session and returns a replacement. |
| Refresh response is lost | The old token can look reused. The client must sign in again after family revocation. |
| Access token expires | Use the refresh route. |
| Session is revoked | Reject refresh and require sign-in. |

## Localization

Neutral `.resx` resources contain English. `.fr.resx` resources contain French.

Use `Accept-Language` before a user exists. Store the selected supported locale when the user is created.

`ProblemDetails.type` never changes with culture. Localize only `title` and `detail`.

SMS delivery outside an HTTP request uses the stored sign-up locale.

## Provider decision gate

The repository has no SMS provider decision. Do not invent a production adapter.

Before implementation reaches SMS delivery, select a provider and record:

- Supported CEMAC countries and operators
- Sender-ID requirements
- Delivery receipts
- Retry and timeout behavior
- Data residency
- Pricing
- Sandbox behavior
- Secret rotation
- Provider error mapping

Use `HttpClient` and a module-owned adapter. Do not add a provider SDK unless the user approves the dependency.

## KYC decision gate

KYC starts after sign-up. It uses the bearer session but a separate module and data model.

Do not add KYC claims, routes, tables, or policies in the sign-up change. First resolve the KYC decisions listed in [Domain and state](domain-and-state.md#kyc-decision-gate).

## Roles and policies

The sign-up flow has one human actor: the customer.

Use these named policies:

- `Registration`: Valid registration token and matching `signUpId`.
- `AuthenticatedUser`: Valid bearer token with `sub` and `sid`.

Do not introduce `Customer`, `User`, or `KycVerified` roles. Role-based access does not model sign-up state.
