# Sign-up backend delivery plan

Status: Proposed

## Approval gates

Implementation cannot start before these approvals:

1. Add the `MoniPay.Users`, `MoniPay.Sessions`, and `MoniPay.Notifications` projects.
2. Change `server/MoniPay.slnx`, `server/Dockerfile`, and host composition.
3. Add the user, sign-up, session, consent, refresh-token, and notification schema.
4. Replace the POC `POST /signup` contract with the proposed route set.
5. Add `Testcontainers.PostgreSql` to the test project and `server/compose.yaml` to the server root.
6. Select the SMS provider and the email provider.
7. Accept the legal terms and privacy version identifiers.

KYC has its own later approval gate. The current repository does not define its production states or provider.

## Delivery order

Each pull request stays below 500 changed code lines and 10 code files. Documentation and generated OpenAPI files do not count toward that limit.

Every change lands with its tests. A change without tests is not complete.

```mermaid
flowchart LR
    C0[0 Test harness] --> C1[1 Kernel primitives]
    C1 --> C2[2 Users domain]
    C1 --> C3[3 Sessions sign-up state]
    C1 --> C4[4 Notifications module]
    C3 --> C5[5 Session tokens]
    C2 --> C6[6 Completion composition]
    C5 --> C6
    C4 --> C7[7 Delivery composition]
    C3 --> C7
    C6 --> C8[8 Routes and OpenAPI]
    C7 --> C8
    C8 --> C9[9 Provider channels]
    C8 --> C10[10 Runtime changes]
    C8 --> C11[11 iOS cutover]
```

### Change 0: test harness

Scope: `server/tests/MoniPay.Tests/` and its package references only.

Add:

- `Testcontainers.PostgreSql` package reference; the version is already pinned centrally
- `Support/MoniPayApi.cs` assembly fixture with one PostgreSQL container
- `Support/TestTimeProvider.cs`, `TestKeys.cs`, `TestPhones.cs`
- `Support/JsonApiAssertions.cs` and `ProblemDetailsAssertions.cs`
- `Fakes/StubHandler.cs`
- Migration of the existing Wallet tests onto the fixture

Acceptance:

- `dotnet test server/MoniPay.slnx` starts one container and passes.
- The existing Wallet and health tests keep their behavior.
- A test can advance time through the fixture.

See [Testing strategy](testing-strategy.md#the-test-host).

### Change 1: shared primitives

Scope: `MoniPay.Kernel` and Kernel tests.

Add:

- `UserId`
- `PhoneNumber`, `EmailAddress`, `PersonName` value types with normalization
- `Http/` JSON:API records and `MoniPayMediaTypes`
- `Validation/` records and `ValidationException`
- `RefusalException`
- `MoniPayHeaders`, `MoniPayPolicies`, `MoniPayClaimTypes`, `MoniPayConventions`
- Sign-up, session, and notification entries in `MoniPayErrorTypes`

Acceptance:

- Phone normalization accepts the supported sign-up countries.
- Invalid lengths and country codes fail deterministically.
- Email and name normalization match [Validation, guards, and error handling](validation-and-errors.md#normalization).
- No module-specific business rule enters Kernel.

### Change 2: Users domain and persistence

Scope: `MoniPay.Users`, module tests, and one EF migration.

Add:

- `User` and `UserConsent` entities
- `RegisterUserHandler` and `RegisterUserCommand`
- `UserPersonalDataProtector` and `UserLookupDigest`
- EF configurations with named constraints
- Unique indexes for `sign_up_id`, phone hash, and email hash

Do not add HTTP routes in this change.

Acceptance:

- The same `SignUpId` returns the same user.
- A duplicate verified phone raises `phone-already-registered`.
- A duplicate normalized email raises `email-already-registered`.
- Stored rows contain no plaintext personal data.
- The generated migration SQL has the documented tables and indexes.

### Change 3: Sessions sign-up state

Scope: `MoniPay.Sessions`, module tests, and one EF migration.

Add:

- `SignUp` aggregate and `SignUpStatus`
- `VerificationCodeGenerator` and `VerificationCodeDigest`
- `SignUpTokenFactory`, `SignUpTokenDigest`, `RegistrationTokenFactory`, `RegistrationTokenDigest`
- `IVerificationCodeSender` port
- `StartSignUpHandler`, `CreateVerificationCodeDeliveryHandler`, `CreatePhoneVerificationHandler`
- Persistent attempt and resend limits
- `SessionsOptions` with startup validation

Tests use a recording sender fake. Do not add a production SMS fallback.

Acceptance:

- Resend invalidates the prior code.
- Resend does not reset failed attempts.
- Expired codes do not verify.
- The sign-up can still resend after a code expires.
- Attempt exhaustion locks verification.
- The stored code and tokens are digests.
- Parallel verifications produce one success.

### Change 4: Notifications module

Scope: `MoniPay.Notifications`, module tests, and one EF migration.

Add:

- `Notification` entity and `NotificationStatus`
- `NotificationOutbox` and `OutboundMessage`
- `NotificationWorker`, `NotificationProcessor`, `DeliverySignal`, `RetrySchedule`
- `DeliveredNotificationPurgeService`
- `ISmsChannel`, `IEmailChannel`, `ChannelResult`
- `RecipientProtector`
- `NotificationsOptions` with startup validation

Acceptance:

- A row enqueued in a transaction is visible to the worker only after commit.
- Two parallel cycles send one message once.
- A retryable failure follows the schedule and then fails.
- An expired message is never sent.
- The body ciphertext is cleared after delivery.
- The worker is off in the test host and driven per cycle.

See [Notification architecture](notifications.md).

### Change 5: session tokens and authentication

Scope: `MoniPay.Sessions`, host pipeline, and integration tests.

Add:

- `AccessTokenIssuer` and `RefreshTokenFactory`
- `Session` and `RefreshToken` entities with rotation and replay detection
- `SessionTokenService`
- `SignUpAuthenticationHandler` and `RegistrationAuthenticationHandler`
- JWT bearer authentication with previous-key support
- Named policies and rate-limit policies
- `ExpiredCredentialCleanupService`

The JWT bearer package already has a central version. Add no package version to a project file.

Acceptance:

- The bearer middleware validates issuer, audience, signature, and lifetime.
- A token signed with the previous key validates until it expires.
- Refresh rotates the token.
- Refresh replay revokes the token family.
- Revocation prevents future refresh.
- Each scheme rejects the other schemes' tokens.
- Cleanup deletes expired sign-ups and keeps active sessions.

### Change 6: sign-up completion composition

Scope: `MoniPay.Sessions`, `MoniPay.Users`, and the host adapter. This is one cross-module contract change.

Add:

- `IUserProvisioning`, `ProvisionUserRequest`, `ProvisionedUser`
- `UserProvisioningAdapter` in the host
- `CreateSignUpCompletionHandler`
- Atomic user and bootstrap-session creation
- Safe completion retry

Acceptance:

- Completion creates one user and one active bootstrap session.
- A failed transaction creates neither resource.
- A retry returns a replacement session without a second user.
- Parallel completions create one user.
- Sessions and Users have no project reference to each other.

### Change 7: delivery composition

Scope: `MoniPay.Sessions`, `MoniPay.Users`, `MoniPay.Notifications`, and the host adapters.

Add:

- `VerificationCodeDeliveryAdapter` in the host
- `IWelcomeMessageSender` port in Users and `WelcomeMessageDeliveryAdapter` in the host
- Localized message rendering in `SessionMessages.resx` and `UserMessages.resx`
- `codeDelivery` projection on the sign-up read model

Acceptance:

- Starting a sign-up enqueues one SMS in the same transaction.
- The rendered SMS is French by default and English on request.
- The SMS contains the code and its lifetime, and nothing personal.
- A welcome email is enqueued after completion, and its failure does not affect sign-up.
- No module references another module.

### Change 8: routes, contracts, and OpenAPI

Scope: slice endpoints in both modules, the host error handling, and endpoint tests.

Add:

- Every route in [HTTP contract](http-contract.md)
- Slice resource and attribute records
- `JsonApiContentNegotiationFilter`
- `MoniPayExceptionHandler`, `MoniPayProblemDetailsWriter`, `ValidationProblemItem`
- Route, operation, tag, summary, policy, and rate-limit constants
- OpenAPI metadata with both media types
- Regenerated `docs/api/openapi.json`

Acceptance:

- Every endpoint uses `TypedResults`.
- Every endpoint calls one `HandleAsync`.
- Every handler accepts and forwards `CancellationToken`.
- Resource records remain internal.
- Every documented status has a test.
- The generated OpenAPI document matches [HTTP contract](http-contract.md).

This change exceeds the size limit as one pull request. Split it by route group: host error handling first, then sign-ups, sessions, and users.

### Change 9: provider channels

Scope: `MoniPay.Notifications` only, one provider per pull request.

Add the selected SMS provider and the selected email provider as typed `HttpClient` channels.

Acceptance:

- Provider DTOs stay internal.
- Every documented provider response maps to one `ChannelResult`.
- Logs contain no recipient, body, or provider payload.
- Timeouts and cancellation propagate.
- The idempotency key reaches the provider when it supports one.
- Sandbox smoke runs manually with provider-owned test numbers only.

### Change 10: runtime changes

Scope: `server/Dockerfile`, `server/compose.yaml`, `appsettings.json`, and the release workflow.

Add:

- Project `COPY` lines for the three modules
- `icu-libs` in the runtime image
- Every new configuration key with its default or an empty secret value
- Forwarded-headers configuration
- The migration bundle step in the release workflow
- `server/compose.yaml`

Acceptance:

- The image boots with only environment variables and refuses to start when a secret is missing.
- A French `Accept-Language` request to the container returns a French title.
- `docker compose -f server/compose.yaml up -d` gives a working local database.

See [Runtime and deployment](runtime-and-deployment.md).

### Change 11: iOS contract cutover

Scope: `ios/` and the generated OpenAPI contract. This is the client half of the contract change.

The iOS work must:

- Start sign-up from the phone step.
- Poll `GET /signups/{signUpId}` for `codeDelivery` before offering a resend.
- Call the real verify and resend routes.
- Keep the passcode and Face ID on the device.
- Complete sign-up from the profile step.
- Store the refresh token in device-only Keychain storage.
- Keep the access token in memory.
- Restore the session with the refresh route.
- Stop creating a local authenticated user after a backend error.
- Route to KYC only after sign-up completion succeeds.

The client must not send passcode or biometric data.

## Test plan

[Testing strategy](testing-strategy.md) defines the test types, the harness, and the rules. The lists below are the minimum per area.

### Domain tests

- Phone, email, and name normalization
- Sign-up state transitions
- Code expiry and sign-up expiry
- Resend cooldown and resend count
- Wrong-code attempt count and lock
- Digest determinism and constant length
- Token format and uniqueness
- Access-token claims

### Slice and database tests

- Unique phone hash, email hash, and `SignUpId`
- Concurrent code verification
- Concurrent sign-up completion
- Transaction rollback across the host adapter
- Refresh-token rotation under concurrency
- Notification claim, retry, expiry, and purge
- Migration from an empty database

### HTTP tests

- Every documented status code
- Every route constant and operation name
- Media-type negotiation
- Validation pointers
- English and French Problem Details
- `Retry-After` on rate limits
- `Cache-Control: no-store` on credential routes
- No account enumeration before phone verification
- OpenAPI request and response schemas
- Cancellation propagation

### Security tests

- Expired, wrong-issuer, wrong-audience, and wrong-key JWT rejection
- Scheme isolation between Sign-up, Registration, and Bearer tokens
- Registration token bound to its route `signUpId`
- Refresh-token family revocation after replay
- No credential or personal data in captured logs or table rows

## Verification commands

Run these commands after each backend change:

```bash
dotnet build server/MoniPay.slnx --configuration Release
dotnet test server/MoniPay.slnx
dotnet format server/MoniPay.slnx --verify-no-changes
```

Run these commands after an endpoint or contract change:

```bash
./scripts/update-openapi.sh
./scripts/check-openapi-sync.sh
```

Review generated migration SQL before merge.

## Prototype traceability

| Prototype behavior | Production backend change |
|---|---|
| Phone length controls the first CTA. | The server repeats country and length validation. |
| The first CTA advances without a server call. | `POST /signups` stores the sign-up and queues the real code. |
| Any six-digit code succeeds. | `POST /signups/{id}/phone-verifications` checks a keyed digest, expiry, and attempt limit. |
| The resend timer is local only. | `POST /signups/{id}/verification-code-deliveries` enforces the cooldown and rotates the code. |
| The passcode exists in JavaScript memory. | iOS stores it locally. The backend never receives it. |
| Face ID is simulated. | iOS enrolls local authentication. The backend never receives biometric data. |
| The profile step calls `POST /signup`. | `POST /signups/{id}/completions` creates the user and first session atomically. |
| The profile is stored in `localStorage`. | iOS stores only session credentials in secure storage. |
| A server error falls back to demo mode. | Production sign-up remains incomplete and shows an error. |
| KYC follows profile completion. | KYC remains a separate module after authenticated sign-up. |
| The POC creates a card-provider customer during sign-up. | Card-provider creation moves to `MoniPay.Cards`. |

## Deliberate omissions

This design does not add:

- Password authentication
- Existing-user sign-in, designed in [Sign-in and device change](sign-in.md) and delivered after this plan
- Email verification
- Social sign-in
- Customer roles
- Admin or support access
- Multi-device session management UI
- Device proof-of-possession
- Push or in-app notifications
- KYC states or provider integration
- Card-provider customer creation
- A compatibility `/signup` endpoint
- A runtime mock or demo fallback
- A cache, a broker, or OpenTelemetry exporters

Add one of these only after its own requirement exists.
