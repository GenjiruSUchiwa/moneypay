# MoniPay.Sessions

Phone verification and sign-up state: one workflow from the delivered code to the provisioned
user and its bootstrap session. The module holds no route yet — the aggregate, the schema and the
four sign-up handlers. Endpoints, completion and sessions arrive with the next slices.

- `SessionsModule.cs` — `AddSessionsModule(services, configuration)`: the options and the
  credential singletons. The four scoped handlers are registered with their endpoints, once the
  host also provides the two ports they depend on; until then the test host registers them next
  to its fakes (`TestPorts`), so a Development host keeps starting.
- `Providers/` — the delivery port the host implements over the notification outbox:
  `IVerificationCodeSender` (`EnqueueAsync` only adds rows to the caller's scoped context, so
  the sign-up and its message commit together; `GetLatestDeliveryAsync` answers the view),
  `VerificationCodeMessage` (the code in raw parts, `ExpiresAt` equal to the code expiry,
  idempotency key `verification-code:{signUpId}:{resendCount}`) and `CodeDeliveryState`.
- `Ports/IRegisteredPhoneLookup.cs` — the "phone already registered" guard on verify, answered
  by Users through a host adapter on the normalized `PhoneNumber`, never a hash.
- `Features/SignUps/` — one folder per slice, each with a handler exposing one `HandleAsync`
  and its command or result records; every mutating handler runs one transaction, takes one
  lock (`SignUpQueries`: the row with `FOR UPDATE`, or the phone through an advisory lock and
  then its active row `FOR UPDATE`, so a start serializes with a resend or a verify), calls one
  aggregate transition and commits. `Start/` locks the phone's active sign-up first and reopens
  it by rotating its code under the resend rules and re-issuing the sign-up token; only a new
  row is counted against the configured window (`rate-limited` past the limit, `Retry-After` to
  the start whose leaving frees a slot); one whose lifetime has passed is closed. `Get/` reads
  the view and the latest delivery state. `ResendCode/` rotates the code.
  `VerifyPhone/` commits the attempt count before it refuses, asks the phone lookup, closes the
  sign-up when the phone is taken so a fresh start is free at once, and issues the registration
  token once. `VerificationCodeMessages` composes the message for start and
  resend.
- `SessionsOptions.cs` — the module configuration, bound from `MoniPay:Sessions` and validated
  at startup: the six supported country phone rules, the code length and lifetime, the resend
  cooldown and limit, the verification-attempt limit, the sign-up lifetime, the per-phone
  start window and limit, and the two
  32-byte keys (`VerificationCodeKeyBase64`, `PersonalDataKeyBase64`) the host refuses to
  start without. The lock has no setting of its own: a sign-up stays locked for the remainder
  of its lifetime.
- `CountryPhoneRules.cs` — the six supported markets by name (CEMAC and UEMOA), the default of `SupportedCountries`.
- `Security/` — the sign-up credentials, all singletons holding key material only.
  `VerificationCodeGenerator` draws digits from `RandomNumberGenerator`; `VerificationCodeDigest`
  is `HMAC-SHA256(verificationCodeKey, signUpId || phoneLookupHash || code)`; `SignUpTokens`
  issues a 32-byte base64url `WorkflowToken` and digests it with a purpose prefix (`signup` /
  `registration`) so one raw value cannot serve both schemes. `SignUpPersonalDataProtector` and
  `PhoneLookupDigest` are the kernel primitives under the Sessions personal-data key: a phone
  is never matched across modules by hash.
- `Domain/SignUp.cs` — the aggregate root, built on the kernel's typed values (`SignUpId`,
  `Ciphertext`, `LookupHash`, `Locale`). `Start` stores the code digest before delivery;
  `RotateVerificationCode` refuses before the cooldown, past the resend limit (with the delay
  to the sign-up's expiry, when a fresh start works) and on any status but `CodePending`, and
  never resets `FailedAttempts`; `Restart` rotates the code and replaces the sign-up token for
  a client that started over, answering a locked sign-up with its retry delay and refusing a
  verified one; `Close` ends a sign-up that cannot complete and `TryExpire` calls it once the
  lifetime has passed, so the phone is free; `VerifyPhone` compares in constant time, returns a mismatch as an
  outcome so the handler can persist the attempt count, and locks on the last allowed attempt
  with `LockedUntil = ExpiresAt`; `IssueRegistrationToken` voids the sign-up token;
  `Complete` moves from `PhoneVerified`, retries safely from `Completed`, refuses everything
  else. Every refusal is a `RefusalException` with its problem type; the handler never
  inspects `Status`.
- `Domain/SignUpStatus.cs`, `PhoneVerificationOutcome.cs` — the state machine and the result of
  one code check.
- `Persistence/SignUpConfiguration.cs`, `SessionSets.cs` — the `sign_ups` table: snake_case,
  string status, `timestamptz` times, `locked_until`, and a `bigint` concurrency token the
  aggregate increments on every persisted mutation. Four indexes: one active workflow per phone
  (partial on the nonterminal statuses), the cleanup scan on `(status, expires_at)`, and a
  unique index per workflow token digest while it exists.
- `Persistence/SessionsSchema.cs` — the names PostgreSQL knows the table, keys and indexes
  by, and the active-status filter of the partial index. A rename breaks the build instead of a
  running client.

The table is created by `AddSignUps` in `MoniPay.Data/Migrations/`: one database, one ordered
history, whichever module prompted the change.

References `Kernel`, `Data`.
