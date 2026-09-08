# .NET module and vertical-slice design

Status: Proposed

## Architecture rule

Use two levels of ownership:

1. A project is a bounded-context module.
2. A feature folder is one vertical slice inside that module.

The module boundary prevents references between bounded contexts. The slice boundary keeps one use case in one directory.

A slice co-locates its endpoint, JSON:API records, handler, command, result, and focused tests. Shared domain and persistence code stays at the module root only when two slices use it.

Do not add global `Endpoints/`, `Contracts/`, or `Services/` folders to these new modules. Those folders split one use case across technical layers.

## Module ownership

### `MoniPay.Sessions`

This module owns phone verification, sign-up state, workflow tokens, access tokens, refresh tokens, and session revocation.

It also owns the SMS provider adapter and credential cleanup.

### `MoniPay.Users`

This module owns the user profile, normalized contact data, locale, legal consent, and the current-user route.

### `MoniPay.Api`

The host owns composition, middleware order, Problem Details, OpenAPI, and one cross-module adapter.

The host owns no sign-up rule and no route handler.

## `MoniPay.Sessions` layout

```text
server/src/MoniPay.Sessions/
  MoniPay.Sessions.csproj
  SessionsModule.cs
  SessionsOptions.cs
  SessionMessages.cs
  SessionMessageKeys.cs
  Features/
    SignUps/
      SignUpResource.cs
      SignUpResourceTypes.cs
      SignUpRoutes.cs
      SignUpEndpointNames.cs
      SignUpTags.cs
      SignUpSummaries.cs
      SignUpRateLimitPolicies.cs
      Start/
        StartSignUpEndpoint.cs
        StartSignUpHandler.cs
        StartSignUpRequest.cs
        StartSignUpCommand.cs
        StartSignUpResult.cs
      Get/
        GetSignUpEndpoint.cs
        GetSignUpHandler.cs
      ResendCode/
        CreateVerificationCodeDeliveryEndpoint.cs
        CreateVerificationCodeDeliveryHandler.cs
        CreateVerificationCodeDeliveryResult.cs
      VerifyPhone/
        CreatePhoneVerificationEndpoint.cs
        CreatePhoneVerificationHandler.cs
        CreatePhoneVerificationRequest.cs
        CreatePhoneVerificationResult.cs
      Complete/
        CreateSignUpCompletionEndpoint.cs
        CreateSignUpCompletionHandler.cs
        CreateSignUpCompletionRequest.cs
        CreateSignUpCompletionResult.cs
        IUserProvisioning.cs
        ProvisionUserRequest.cs
  Ports/
    IRegisteredPhoneLookup.cs
    ISecurityAlertSender.cs
    SecurityAlert.cs
    Sessions/
      SessionResource.cs
      SessionResourceTypes.cs
      SessionRoutes.cs
      SessionEndpointNames.cs
      SessionTags.cs
      SessionSummaries.cs
      SessionRateLimitPolicies.cs
      Refresh/
        CreateSessionRefreshEndpoint.cs
        CreateSessionRefreshHandler.cs
        CreateSessionRefreshRequest.cs
      GetCurrent/
        GetCurrentSessionEndpoint.cs
        GetCurrentSessionHandler.cs
      RevokeCurrent/
        DeleteCurrentSessionEndpoint.cs
        DeleteCurrentSessionHandler.cs
  Domain/
    SignUp.cs
    SignUpStatus.cs
    Session.cs
    RefreshToken.cs
    SessionTokenService.cs
  Persistence/
    SignUpConfiguration.cs
    SessionConfiguration.cs
    RefreshTokenConfiguration.cs
    SessionSets.cs
    ExpiredCredentialCleanupService.cs
  Security/
    VerificationCodeGenerator.cs
    VerificationCodeDigest.cs
    SignUpTokenFactory.cs
    SignUpTokenDigest.cs
    RegistrationTokenFactory.cs
    RegistrationTokenDigest.cs
    SignUpPersonalDataProtector.cs
    PhoneLookupDigest.cs
    AccessTokenIssuer.cs
    RefreshTokenFactory.cs
    SignUpAuthenticationHandler.cs
    RegistrationAuthenticationHandler.cs
  Providers/
    IVerificationCodeSender.cs
    VerificationCodeMessage.cs
    CodeDeliveryState.cs
    VerificationCodeRenderer.cs
  Resources/
    SessionMessages.resx
    SessionMessages.fr.resx
```

Sessions never talks to an SMS provider. `IVerificationCodeSender` is implemented by a host adapter over the notification outbox (see [Notification architecture](notifications.md)); the provider channel lives in `MoniPay.Notifications`.

The `Ports/` folder holds the other contracts the host implements for Sessions:

- `IRegisteredPhoneLookup.FindUserIdAsync(PhoneNumber, CancellationToken)` returns the `UserId` that owns a verified phone, or `null`. The verify-phone handler uses it for `phone-already-registered`; the sign-in verify handler uses it to find the user. It takes the normalized phone, not a hash: each module hashes with its own key.
- `ISecurityAlertSender.EnqueueAsync(SecurityAlert, CancellationToken)` queues `RefreshTokenReuseDetected`, `SessionRevoked` and `NewDeviceSignIn` by `UserId`; the host adapter resolves the recipient through Users.

## `MoniPay.Users` layout

```text
server/src/MoniPay.Users/
  MoniPay.Users.csproj
  UsersModule.cs
  UserMessages.cs
  UserMessageKeys.cs
  Features/
    Registration/
      RegisterUserHandler.cs
      RegisterUserCommand.cs
      RegisteredUser.cs
      PhoneRegistrationLookup.cs
    Contact/
      UserContactLookup.cs
      UserContact.cs
    CurrentUser/
      GetCurrentUserEndpoint.cs
      GetCurrentUserHandler.cs
      UserResource.cs
      UserResourceTypes.cs
      UserRoutes.cs
      UserEndpointNames.cs
      UserTags.cs
      UserSummaries.cs
  Domain/
    User.cs
    UserConsent.cs
    LegalDocumentKind.cs
  Persistence/
    UserConfiguration.cs
    UserConsentConfiguration.cs
    UserSets.cs
  Security/
    UserPersonalDataProtector.cs
    UserLookupDigest.cs
  Ports/
    IWelcomeMessageSender.cs
    WelcomeMessage.cs
  Resources/
    UserMessages.resx
    UserMessages.fr.resx
```

## Shared HTTP records

Add transport-only records under `MoniPay.Kernel/Http/`:

```text
JsonApiRequest.cs
JsonApiResponse.cs
JsonApiVersion.cs
JsonApiLinks.cs
JsonApiResourceIdentifier.cs
JsonApiRelationship.cs
MoniPayMediaTypes.cs
```

These types contain no domain rule and no ASP.NET Core dependency.

Each slice still owns its resource and attribute records. This keeps OpenAPI schemas explicit.

## Test layout

The test project mirrors each vertical slice:

```text
server/tests/MoniPay.Tests/
  Sessions/
    SignUps/
      Start/StartSignUpTests.cs
      Get/GetSignUpTests.cs
      ResendCode/CreateVerificationCodeDeliveryTests.cs
      VerifyPhone/CreatePhoneVerificationTests.cs
      Complete/CreateSignUpCompletionTests.cs
    Sessions/
      Refresh/CreateSessionRefreshTests.cs
      GetCurrent/GetCurrentSessionTests.cs
      RevokeCurrent/DeleteCurrentSessionTests.cs
  Users/
    Registration/RegisterUserTests.cs
    CurrentUser/GetCurrentUserTests.cs
```

A slice test calls its handler for behavior and its endpoint for HTTP semantics.

## Host composition layout

```text
server/src/MoniPay.Api/
  Composition/
    UserProvisioningAdapter.cs
    RegisteredPhoneLookupAdapter.cs
    VerificationCodeDeliveryAdapter.cs
    WelcomeMessageDeliveryAdapter.cs
    SecurityAlertDeliveryAdapter.cs
  Errors/
    MoniPayExceptionHandler.cs
    MoniPayProblemDetailsWriter.cs
    ValidationProblemItem.cs
  Http/
    JsonApiTransportMiddleware.cs
    JsonApiTransport.cs
  Hosting/
    PipelineExtensions.cs
    ForwardedHeadersOptionsSetup.cs
    RateLimitOptions.cs
  MoniPayModules.cs
```

Each adapter implements a port one module declares by calling a public slice of another module:

| Adapter | Port (declared by) | Calls |
|---|---|---|
| `UserProvisioningAdapter` | `IUserProvisioning` (Sessions) | `RegisterUserHandler` (Users) |
| `RegisteredPhoneLookupAdapter` | `IRegisteredPhoneLookup` (Sessions) | `PhoneRegistrationLookup` (Users) |
| `VerificationCodeDeliveryAdapter` | `IVerificationCodeSender` (Sessions) | `NotificationOutbox` (Notifications) |
| `WelcomeMessageDeliveryAdapter` | `IWelcomeMessageSender` (Users) | `NotificationOutbox` (Notifications) |
| `SecurityAlertDeliveryAdapter` | `ISecurityAlertSender` (Sessions) | `UserContactLookup` (Users), `NotificationOutbox` (Notifications) |

These adapters are the only code that names two modules. No module references another module project.

## Public interface

Only these Sessions types are public:

- `SessionsModule`
- `IUserProvisioning`, `ProvisionUserRequest`
- `IRegisteredPhoneLookup`
- `IVerificationCodeSender`, `VerificationCodeMessage`, `CodeDeliveryState`
- `ISecurityAlertSender`, `SecurityAlert`, `SecurityAlertKind`

Only these Users types are public:

- `UsersModule`
- `RegisterUserHandler`, `RegisterUserCommand`, `RegisteredUser`
- `PhoneRegistrationLookup`
- `UserContactLookup`, `UserContact`
- `IWelcomeMessageSender`, `WelcomeMessage`

A port is public because the host implements it; a slice is public because a host adapter calls it. Endpoint classes, JSON:API resource records, entities, EF configurations, token helpers, and provider DTOs stay internal. `Architecture/PublicSurfaceTests.cs` asserts these lists.

## Slice methods

Each endpoint calls one `HandleAsync` method. Each handler owns one use case.

### Start sign-up

```csharp
internal sealed class StartSignUpHandler(
    MoniPayDbContext database,
    IVerificationCodeSender sender,
    VerificationCodeGenerator codes,
    VerificationCodeDigest codeDigest,
    SignUpTokenFactory signUpTokens,
    SignUpTokenDigest signUpTokenDigest,
    SignUpPersonalDataProtector personalData,
    PhoneLookupDigest phoneLookup,
    TimeProvider timeProvider,
    IOptions<SessionsOptions> options)
{
    public Task<StartSignUpResult> HandleAsync(
        StartSignUpCommand command,
        CancellationToken cancellationToken);
}
```

The handler commits the sign-up and code digest before SMS delivery. It returns the raw Sign-up token once.

### Get sign-up

```csharp
internal sealed class GetSignUpHandler(MoniPayDbContext database)
{
    public Task<SignUpView> HandleAsync(
        Guid signUpId,
        CancellationToken cancellationToken);
}
```

The authentication handler already matched the workflow token to `signUpId`.

### Resend code

```csharp
internal sealed class CreateVerificationCodeDeliveryHandler(
    MoniPayDbContext database,
    IVerificationCodeSender sender,
    VerificationCodeGenerator codes,
    VerificationCodeDigest codeDigest,
    TimeProvider timeProvider,
    IOptions<SessionsOptions> options)
{
    public Task<CreateVerificationCodeDeliveryResult> HandleAsync(
        Guid signUpId,
        CancellationToken cancellationToken);
}
```

The handler locks the sign-up row, checks limits, rotates the digest, and preserves failed attempts.

### Verify phone

```csharp
internal sealed class CreatePhoneVerificationHandler(
    MoniPayDbContext database,
    VerificationCodeDigest codeDigest,
    RegistrationTokenFactory registrationTokens,
    RegistrationTokenDigest registrationTokenDigest,
    IRegisteredPhoneLookup registeredPhones,
    TimeProvider timeProvider,
    IOptions<SessionsOptions> options)
{
    public Task<CreatePhoneVerificationResult> HandleAsync(
        Guid signUpId,
        string verificationCode,
        CancellationToken cancellationToken);
}
```

The handler counts attempts atomically: on a mismatch it saves the attempt count and commits before it throws. On a match it asks `IRegisteredPhoneLookup` whether a user owns the phone and refuses with `phone-already-registered`; otherwise success invalidates the Sign-up token and returns the Registration token once.

### Get sign-up delivery state

`GetSignUpHandler` reports `codeDelivery` through `IVerificationCodeSender.GetLatestDeliveryAsync(signUpId)`, which the host adapter answers from `NotificationOutbox.FindLatestStatusAsync(correlationId: signUpId, kind: "VerificationCode")`. Sessions never opens the `notifications` table.

### Complete sign-up

```csharp
internal sealed class CreateSignUpCompletionHandler(
    MoniPayDbContext database,
    IUserProvisioning users,
    RegistrationTokenDigest registrationTokenDigest,
    SessionTokenService sessions,
    TimeProvider timeProvider)
{
    public Task<CreateSignUpCompletionResult> HandleAsync(
        Guid signUpId,
        string registrationToken,
        CreateSignUpCompletionCommand command,
        CancellationToken cancellationToken);
}
```

This handler owns the cross-module transaction. The endpoint contains no transaction or provisioning rule.

### User-provisioning port

```csharp
public interface IUserProvisioning
{
    Task<UserId> ProvisionAsync(
        ProvisionUserRequest request,
        CancellationToken cancellationToken);
}
```

`ProvisionUserRequest` contains `SignUpId`, `UserId`, verified `PhoneNumber`, profile data, locale, consent versions, and acceptance time.

The port returns only the user identity. Completion determines its `Created` result from the sign-up state, not the Users registration result.

### Registered-phone port

```csharp
public interface IRegisteredPhoneLookup
{
    Task<UserId?> FindUserIdAsync(
        PhoneNumber phone,
        CancellationToken cancellationToken);
}
```

The Users side is `PhoneRegistrationLookup`, a public slice that hashes the phone with the Users lookup key and reads `users.phone_lookup_hash`.

### Users registration slice

```csharp
public sealed class RegisterUserHandler(
    MoniPayDbContext database,
    UserPersonalDataProtector personalData,
    UserLookupDigest lookupDigest,
    TimeProvider timeProvider)
{
    public Task<RegisteredUser> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken);
}
```

The handler encrypts the already normalized contact data. `SignUpId` makes the operation idempotent. It calls `SaveChangesAsync` inside the caller's ambient transaction — that is how it observes a unique-constraint violation and maps it — but it opens no transaction of its own and never commits.

### Host adapter

```csharp
internal sealed class UserProvisioningAdapter(
    RegisterUserHandler users) : IUserProvisioning
{
    public Task<UserId> ProvisionAsync(
        ProvisionUserRequest request,
        CancellationToken cancellationToken);
}
```

The adapter maps records between two slices. It adds no rule and opens no table.

### Session-token service

```csharp
internal sealed class SessionTokenService(
    MoniPayDbContext database,
    AccessTokenIssuer accessTokens,
    RefreshTokenFactory refreshTokens,
    TimeProvider timeProvider,
    IOptions<SessionsOptions> options)
{
    public Task<SessionTokenResult> CreateAsync(
        UserId userId,
        Guid deviceId,
        CancellationToken cancellationToken);

    public Task<SessionTokenResult> ReplaceBootstrapAsync(
        UserId userId,
        Guid priorSessionId,
        Guid deviceId,
        CancellationToken cancellationToken);

    public Task<SessionTokenResult> RefreshAsync(
        string refreshToken,
        Guid deviceId,
        CancellationToken cancellationToken);

    public Task RevokeAsync(
        Guid sessionId,
        CancellationToken cancellationToken);
}
```

Refresh and revocation handlers call this shared service. Do not add interfaces for handlers, token helpers, or `SessionTokenService`.

### Credential cleanup

```csharp
internal sealed class ExpiredCredentialCleanupService(
    IServiceScopeFactory scopes,
    TimeProvider timeProvider,
    IOptions<SessionsOptions> options) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken);

    internal Task<int> DeleteBatchAsync(
        MoniPayDbContext database,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken);
}
```

The worker deletes expired sign-ups and old consumed refresh tokens in bounded batches. It never deletes active sessions.

A sign-up row is kept until both its `expires_at` and `created_at + MoniPay:Sessions:StartWindow` have passed: the per-phone start limit counts rows by `created_at`, so a row deleted earlier would let a phone escape the limit. The cutoff is `now - max(SignUpLifetime, StartWindow)`, never plain `now`.

## Aggregate behavior

Handlers load one aggregate and call behavior on it:

```csharp
internal sealed class SignUp
{
    public static SignUp Start(/* normalized inputs, digests, and timestamps */);

    public void RotateVerificationCode(/* digest and timestamps */);

    public PhoneVerificationOutcome VerifyPhone(
        ReadOnlySpan<byte> candidateDigest,
        DateTimeOffset now,
        int maximumAttempts);

    public void Complete(
        UserId userId,
        Guid sessionId,
        DateTimeOffset now);
}
```

The entity protects transitions. Handlers coordinate I/O and transactions. Endpoints translate HTTP only.

## Completion transaction

`CreateSignUpCompletionHandler.HandleAsync` performs one database transaction:

1. Lock the `sign_ups` row.
2. Read the current time and validate state, expiry, and Registration-token digest before any writes.
3. Generate a `UserId`.
4. Call `IUserProvisioning.ProvisionAsync`.
5. Create the bootstrap session and refresh-token digest.
6. Mark the sign-up `Completed` with `UserId` and `SessionId`.
7. Commit.
8. Return raw access and refresh tokens.

The adapter and Users handler use the same scoped `MoniPayDbContext`. All inserts join the same transaction.

`SignUp.CompletionRefusal` owns the state and lifetime rules shared by authentication and completion.
Both first completion and retry must start before sign-up expiry and within ten minutes of phone verification.
Eligibility uses the time after acquiring the row lock. Consent acceptance retains the server receipt time from handler entry.

A completion retry revokes the prior bootstrap session and creates one replacement session. Other sessions remain active.

### Completion decisions

- **Device on retry.** A retry may name a different device: it opens the session the client is asking
  from. The refresh device-match rule belongs to refresh and is not applied to completion.
- **Replacing a bootstrap that was already revoked.** A retry replaces it anyway, and opens a new
  token family. `Session.Revoke` keeps the first reason and time, so a session revoked by logout or
  by a refresh-token replay is never reactivated and never re-labelled; unrelated sessions and
  their families are untouched.
- **Committing after expiry.** Eligibility is decided once, under the row lock. A request that passed
  it may commit a moment after the expiry instant; there is no second expiry check after
  provisioning, so a retry cannot be half-applied.
- **Refusal precedence.** Credential first, then sign-up expiry, then state, then the registration
  token's lifetime: a caller without a valid credential learns nothing about the sign-up.

## Endpoint mapping

`SessionsModule.MapSessionsModule` creates groups and delegates route mapping to slices:

```csharp
var signUps = routes
    .MapGroup(SignUpRoutes.Group)
    .WithTags(SignUpTags.SignUps);

signUps.MapStartSignUp();
signUps.MapGetSignUp();
signUps.MapCreateVerificationCodeDelivery();
signUps.MapCreatePhoneVerification();
signUps.MapCreateSignUpCompletion();

routes.MapCreateSessionRefresh();

var sessions = routes
    .MapGroup(SessionRoutes.Group)
    .WithTags(SessionTags.Sessions)
    .RequireAuthorization(MoniPayPolicies.AuthenticatedUser);

sessions.MapGetCurrentSession();
sessions.MapDeleteCurrentSession();
```

Each `Map...` method sits beside its request and handler. It returns `TypedResults` with exact media-type metadata.

## Authentication and host pipeline

`SessionsModule.AddSessionsModule` registers JWT bearer, Sign-up token, Registration token, policies, rate limits, handlers, token helpers, cleanup, and SMS delivery.

JWT validation includes issuer, audience, signature, lifetime, disabled inbound claim mapping, and a small clock skew.

`PipelineExtensions.UseMoniPayPipeline` uses this order:

1. Forwarded headers from trusted proxies
2. Request localization
3. Observability
4. Exception handling and Problem Details
5. HSTS outside development
6. Rate limiting
7. Authentication
8. Authorization
9. Output no-store policy for credential routes
10. Mapped endpoints

## Database model

PostgreSQL is authoritative. Use `TimeProvider`, `DateTimeOffset`, string enum conversion, snake_case, and explicit indexes.

### `sign_ups`

| Column | Type | Rule |
|---|---|---|
| `id` | `uuid` | Primary key. |
| `phone_ciphertext` | `text` | Encrypted phone. |
| `phone_lookup_hash` | `bytea` | Keyed lookup hash. |
| `locale` | `varchar(16)` | Supported BCP-47 value. |
| `code_digest` | `bytea`, nullable | Keyed digest. |
| `code_expires_at` | `timestamptz`, nullable | Code expiry. |
| `signup_token_digest` | `bytea`, nullable | Initial workflow credential. |
| `registration_token_digest` | `bytea`, nullable | Completion credential. |
| `status` | `varchar(24)` | String enum. |
| `failed_attempts` | `integer` | Never reset by resend. |
| `resend_count` | `integer` | Limited by policy. |
| `can_resend_at` | `timestamptz` | Cooldown. |
| `locked_until` | `timestamptz`, nullable | Set when the attempt limit is reached; the source of `Retry-After`. |
| `expires_at` | `timestamptz` | Maximum sign-up lifetime. |
| `terms_version` | `varchar(64)` | Client-displayed version. |
| `privacy_version` | `varchar(64)` | Client-displayed version. |
| `user_id` | `uuid`, nullable | Set at completion. |
| `bootstrap_session_id` | `uuid`, nullable | Set at completion. |
| `version` | `bigint` | Optimistic concurrency token. |
| `created_at` | `timestamptz` | Audit time. |
| `verified_at` | `timestamptz`, nullable | Audit time. |
| `completed_at` | `timestamptz`, nullable | Audit time. |

Indexes:

- Unique active-workflow index on `phone_lookup_hash` for nonterminal states
- Index on `status` and `expires_at` for cleanup
- Unique index on `signup_token_digest` when not null
- Unique index on `registration_token_digest` when not null

### `users`

| Column | Type | Rule |
|---|---|---|
| `id` | `uuid` | Primary key. |
| `sign_up_id` | `uuid` | Unique idempotency key. |
| `first_name_ciphertext` | `text` | Encrypted. |
| `last_name_ciphertext` | `text` | Encrypted. |
| `phone_ciphertext` | `text` | Encrypted. |
| `phone_lookup_hash` | `bytea` | Unique keyed hash. |
| `email_ciphertext` | `text` | Encrypted. |
| `email_lookup_hash` | `bytea` | Unique keyed hash. |
| `locale` | `varchar(16)` | Supported BCP-47 value. |
| `created_at` | `timestamptz` | Audit time. |

Do not add `kyc_verified` to this table.

### `user_consents`

| Column | Type | Rule |
|---|---|---|
| `user_id` | `uuid` | Part of the composite key. |
| `document_kind` | `varchar(24)` | `Terms` or `Privacy`. |
| `document_version` | `varchar(64)` | Version shown by the client. |
| `accepted_at` | `timestamptz` | Server receipt time. |

### `sessions`

| Column | Type | Rule |
|---|---|---|
| `id` | `uuid` | Primary key and `sid` claim. |
| `user_id` | `uuid` | Scalar module reference. |
| `device_id` | `uuid` | Session label, not a secret. |
| `token_family_id` | `uuid` | Refresh-reuse scope. |
| `version` | `bigint` | Optimistic concurrency token. |
| `created_at` | `timestamptz` | Audit time. |
| `last_seen_at` | `timestamptz` | Updated on refresh. |
| `revoked_at` | `timestamptz`, nullable | Null while active. |
| `revoke_reason` | `varchar(32)`, nullable | Stable internal code. |

Indexes:

- Index on `user_id` and `revoked_at`
- Unique index on `token_family_id` and `device_id`

### `refresh_tokens`

| Column | Type | Rule |
|---|---|---|
| `id` | `uuid` | Primary key. |
| `session_id` | `uuid` | Sessions-owned relation. |
| `token_digest` | `bytea` | SHA-256 of a random token. |
| `created_at` | `timestamptz` | Audit time. |
| `expires_at` | `timestamptz` | Absolute expiry. |
| `used_at` | `timestamptz`, nullable | Set during rotation. |
| `replaced_by_id` | `uuid`, nullable | Rotation chain. |

Use unique indexes on `token_digest` and active token per session (partial on `session_id` where `used_at is null`), and an index on `expires_at` for cleanup.

`sessions.user_id` is a scalar, not a foreign key: Sessions never references the `users` table.

### Migrations

`MoniPay.Data` has no migration today. The first one arrives with the Users schema and also adds `dotnet-ef` to `dotnet-tools.json`; every later schema change adds one migration in the pull request that introduces it, with its generated SQL pasted in the pull request.

## Cache decision

Do not cache sign-up state, verification codes, workflow tokens, refresh tokens, uniqueness checks, or consent records.

Do not add Redis for the first implementation. PostgreSQL transactions and indexes are the authority.

JWT signature validation is local. The `AuthenticatedUser` policy checks active session state for sensitive routes.

If that database check becomes a measured bottleneck, add a distributed active-session cache in its own change. The cache must invalidate on revocation and use a lifetime shorter than access-token expiry.

Never HTTP-cache sign-up, user, or session responses. Credential routes always send `Cache-Control: no-store`.

## Composition changes

Implementation adds:

- Users and Sessions assemblies to `MoniPayModules.ModuleAssemblies`
- Module DI and route mapping calls
- The five host adapters, registered against their ports
- Project references from `MoniPay.Api`
- Project entries in `server/MoniPay.slnx`
- Project copy entries in `server/Dockerfile`, added by the pull request that creates each project — the image does not build without them
- Generated EF migrations in `MoniPay.Data`

## Package policy

Use framework and repository dependencies only:

- `Microsoft.AspNetCore.App`
- `Microsoft.AspNetCore.Authentication.JwtBearer`, already pinned centrally
- `Microsoft.EntityFrameworkCore`, already pinned centrally
- `System.Security.Cryptography`
- `HttpClient`

Do not add JSON:API, cache, SMS, or cryptography packages without approval.
