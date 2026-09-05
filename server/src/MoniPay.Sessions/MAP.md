# MoniPay.Sessions

Phone verification and sign-up state: one workflow from the delivered code to the provisioned
user and its bootstrap session. The module holds no route yet — this is the aggregate and the
schema. Handlers, tokens and sessions arrive with the next slices.

- `SessionsModule.cs` — `AddSessionsModule(services, configuration)`. The only public type of
  the module.
- `SessionsOptions.cs` — the module configuration, bound from `MoniPay:Sessions` and validated
  at startup: the six supported country phone rules, the code length and lifetime, the resend
  cooldown and limit, the verification-attempt limit and the sign-up lifetime. The lock has no
  setting of its own: a sign-up stays locked for the remainder of its lifetime.
- `Domain/SignUp.cs` — the aggregate root, built on the kernel's typed values (`SignUpId`,
  `Ciphertext`, `LookupHash`, `Locale`). `Start` stores the code digest before delivery;
  `RotateVerificationCode` refuses before the cooldown and past the resend limit and never
  resets `FailedAttempts`; `VerifyPhone` compares in constant time, returns a mismatch as an
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
