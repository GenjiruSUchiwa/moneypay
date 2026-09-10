# Testing strategy

Status: Proposed

## Purpose

This document defines how the sign-up backend is tested: the test types, the harness, the layout, the methodology, and the rules every test follows.

The repository rules in `agents/rules/testing-xunit-testcontainers.md` and `agents/rules/testing-dotnet-coverage.md` apply. This document applies them to sign-up and sessions.

## Current state

`server/tests/MoniPay.Tests/` exists with `HealthEndpointsTests`, `MoneyTests`, and two Wallet tests. `HealthEndpointsTests` and `WalletLocalizationTests` boot the host through `WebApplicationFactory<Program>` without a database; `WalletEndpointsTests` maps the module on a bare builder and `MoneyTests` is a pure domain test. Only the first two move onto the fixture.

The project does not yet reference `Testcontainers.PostgreSql` or `Npgsql`, and it has no `Support/` or `Fakes/` folder. Both package versions are already pinned in `server/Directory.Packages.props`. `MoniPay.Data` has no migration yet, so the fixture's `ApplyMigrationsOnStartup = true` only creates the migration history table until the first schema change lands.

The harness below is the first delivery change. Every sign-up test depends on it.

## Test types

| Type | What it proves | Runs against | Speed |
|---|---|---|---|
| Domain tests | Aggregate transitions, value-type normalization, digests, token formats | Plain objects, no host | Milliseconds |
| Slice tests | One handler's behavior, including its transaction and constraints | The host's DI scope and a real PostgreSQL | Tens of milliseconds |
| HTTP contract tests | Status, media types, JSON:API shape, Problem Details, headers, localization | The real host over HTTP and a real PostgreSQL | Tens of milliseconds |
| Security tests | Token validation, scheme isolation, replay detection, enumeration resistance | The real host over HTTP | Tens of milliseconds |
| Concurrency tests | Exactly-once outcomes under parallel requests | The real host over HTTP | Hundreds of milliseconds |
| Provider adapter tests | Provider payload mapping to `ChannelResult` | A stub `HttpMessageHandler` | Milliseconds |
| Migration tests | The schema the migrations produce | An empty PostgreSQL | Seconds, once per run |
| Architecture tests | Module boundaries and public surface | Reflection on the assemblies | Milliseconds |
| Contract sync | The committed OpenAPI document matches the host | `scripts/check-openapi-sync.sh` | Seconds |

There is no mocking framework. There are no tests against a live provider sandbox.

## The test host

`server/tests/MoniPay.Tests/Support/MoniPayApi.cs` is an xunit v3 assembly fixture. It starts one PostgreSQL container for the whole run and one `WebApplicationFactory<Program>`.

```csharp
[assembly: AssemblyFixture(typeof(MoniPayApi))]

public sealed class MoniPayApi : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("monipay").WithUsername("monipay").WithPassword("monipay").Build();

    private WebApplicationFactory<Program>? factory;

    public TestTimeProvider Time { get; } = new();

    public RecordingChannel Sms { get; } = new("sms-ref");

    public RecordingChannel Email { get; } = new("email-ref");

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(MoniPayEnvironments.Testing);
            builder.UseSetting("ConnectionStrings:MoniPay", container.GetConnectionString());
            builder.UseSetting(MoniPayConfiguration.ApplyMigrationsOnStartup, "true");
            builder.UseSetting(SessionsOptions.Keys.SigningKeyBase64, TestKeys.Signing);
            builder.UseSetting(SessionsOptions.Keys.VerificationCodeKeyBase64, TestKeys.VerificationCode);
            builder.UseSetting(SessionsOptions.Keys.PersonalDataKeyBase64, TestKeys.SessionsPersonalData);
            builder.UseSetting(UsersOptions.Keys.PersonalDataKeyBase64, TestKeys.UsersPersonalData);
            // Values differ from the defaults on purpose: a test asserting a default passes even
            // when nothing read the option.
            builder.UseSetting(SessionsOptions.Keys.MaximumVerificationAttempts, "3");
            builder.UseSetting(SessionsOptions.Keys.VerificationCodeLifetime, "00:02:00");
            builder.UseSetting(NotificationsOptions.Keys.WorkerEnabled, "false");
            builder.UseSetting(SessionsOptions.Keys.CleanupEnabled, "false");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Time);
                services.RemoveAllKeyed<INotificationChannel>(NotificationChannel.Sms);
                services.AddKeyedSingleton<INotificationChannel>(NotificationChannel.Sms, Sms);
                services.AddKeyedSingleton<INotificationChannel>(NotificationChannel.Email, Email);
            });
        });
    }

    public HttpClient CreateClient() { /* Accept and Content-Type headers preset */ }

    public Task RunNotificationCycleAsync() { /* one NotificationProcessor cycle in a fresh scope */ }

    public Task RunCleanupCycleAsync() { /* one ExpiredCredentialCleanupService batch */ }
}
```

Configuration keys are constants on the options classes. A test never repeats a configuration literal.

`TestKeys` holds fixed 32-byte ASCII strings. Their values are irrelevant; their length is not.

Isolation comes from data, not from containers. Each test starts its own sign-up with a unique phone from `TestPhones.Next()`. Nothing truncates tables between tests.

## Fakes

`server/tests/MoniPay.Tests/Fakes/` holds hand-written fakes. Each implements one port and records what it was asked.

| Fake | Port | Records | Configurable |
|---|---|---|---|
| `RecordingChannel` | `INotificationChannel` | Recipient, subject, body, idempotency key | Next `ChannelResult` |
| `TestTimeProvider` | `TimeProvider` | Nothing | `Advance`, `Set`, `Reset` |
| `StubHandler` | `HttpMessageHandler` | Request snapshots | Canned status and body, injectable behavior |

`Support/DatabaseAssertions.cs` reads every table through Npgsql and returns the columns holding a given plaintext, for the "no personal data in a row" assertions.

A fake contains no branch. If a test needs the fake to decide something, the decision belongs in the code under test.

`RecordingChannel.CodeFor(phone)` extracts the six digits from the last recorded body for that recipient. Tests run a delivery cycle first, then read the verification code from there, never from the database and never from a predictable generator.

## Flow helpers

`server/tests/MoniPay.Tests/Support/SignUpFlow.cs` drives the HTTP routes and returns typed results:

```csharp
var started = await Api.StartSignUpAsync(phone);                 // signUpId, signUpToken
await Api.RunNotificationCycleAsync();
var code = Api.Sms.CodeFor(phone);
var verified = await Api.VerifyPhoneAsync(started, code);        // registrationToken
var session = await Api.CompleteSignUpAsync(verified, Profile.Sample());  // accessToken, refreshToken, userId
```

Every helper uses the route constants and the JSON:API records from the modules. A renamed route breaks the helper at compile time.

`Profile.Sample()` returns a distinct email per call. Two tests never share a user.

## Layout

The test project mirrors each vertical slice, as [.NET module design](dotnet-module-design.md#test-layout) shows:

```text
server/tests/MoniPay.Tests/
  Support/
    MoniPayApi.cs
    MoniPayApiTest.cs
    SignUpFlow.cs
    JsonApiAssertions.cs
    ProblemDetailsAssertions.cs
    DatabaseAssertions.cs
    TestKeys.cs
    TestPhones.cs
    TestTimeProvider.cs
  Fakes/
    RecordingChannel.cs
    RecordingVerificationCodeSender.cs
    StubRegisteredPhoneLookup.cs
    StubHandler.cs
  Architecture/
    ModuleBoundaryTests.cs
    PublicSurfaceTests.cs
  Kernel/
    PhoneNumberTests.cs
    EmailAddressTests.cs
    PersonNameTests.cs
    JsonApiDocumentTests.cs
  Sessions/
    Domain/
      SignUpTransitionTests.cs
      VerificationCodeGeneratorTests.cs
      RefreshTokenFactoryTests.cs
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
    Security/
      AccessTokenValidationTests.cs
      SchemeIsolationTests.cs
      EnumerationResistanceTests.cs
    Persistence/
      ExpiredCredentialCleanupTests.cs
  Users/
    Registration/RegisterUserTests.cs
    CurrentUser/GetCurrentUserTests.cs
  Notifications/
    Deliver/NotificationProcessorTests.cs
    Channels/BirdSmsChannelTests.cs
    Channels/BirdSmsWiringTests.cs
  Migrations/
    SchemaTests.cs
```

A slice test class covers one slice: its happy path, each documented refusal, and its concurrency case when it mutates state.

## Methodology

### One behavior per test

A test name is a sentence about behavior:

```text
A_resend_does_not_reset_the_failed_attempt_count
The_third_wrong_code_locks_the_sign_up
A_consumed_refresh_token_revokes_its_whole_family_when_replayed
Starting_a_sign_up_for_a_registered_phone_looks_the_same_as_a_new_one
```

Each test follows arrange, act, assert, with one act. A test that verifies then completes then refreshes is three tests.

### Assert outcomes, not calls

A test asserts a status code, a document member, a row, or a recorded message. It never asserts that a method was called.

```csharp
[Fact]
public async Task A_resend_does_not_reset_the_failed_attempt_count()
{
    var phone = TestPhones.Next();
    var started = await Api.StartSignUpAsync(phone);
    await Api.VerifyPhoneAsync(started, "000000", expect: HttpStatusCode.UnprocessableContent);
    await Api.VerifyPhoneAsync(started, "000000", expect: HttpStatusCode.UnprocessableContent);

    Api.Time.Advance(TimeSpan.FromSeconds(61));
    await Api.ResendCodeAsync(started);
    var response = await Api.VerifyPhoneAsync(started, "000000", expect: HttpStatusCode.TooManyRequests);

    var problem = await response.ReadProblemAsync();
    Assert.Equal(MoniPayErrorTypes.SignUpAttemptLimit, problem.Type);
}
```

The test proves the rule through the third response. It does not read `failed_attempts` from the table.

### Frozen time

`TestTimeProvider` wraps a `FakeTimeProvider`. A test moves time with `Api.Time.Advance`. No test sleeps, and no test compares against `DateTimeOffset.UtcNow`.

Expiry tests advance past the boundary by one second, then assert the refusal. They also assert the state one second before the boundary still works.

### Pinned culture

A test that asserts localized text sends an explicit `Accept-Language`. A test that asserts a formatted value uses `CultureInfo.InvariantCulture`. The suite must pass on a French macOS and an English Linux runner.

### Cancellation

Every `HttpClient` call passes `TestContext.Current.CancellationToken`. A handler test passes it to `HandleAsync`. A slice that drops the token fails the provider cancellation test.

### Test values differ from defaults

The test host sets `MaximumVerificationAttempts` to 3, not the production 5. A test asserting the lock on the third attempt fails when the option is not read.

### No live providers

Provider channels are tested against `StubHandler` with recorded provider payloads. The real sandbox is exercised only by a manual smoke script, once, after the provider is selected.

## Domain tests

Domain tests need no host. They construct the aggregate and call transitions.

| Area | Cases |
|---|---|
| `SignUp.Start` | Initial status, code expiry, resend cooldown, sign-up expiry, zero attempts |
| `SignUp.RotateVerificationCode` | Cooldown not elapsed, resend limit, attempts preserved, old digest invalid |
| `SignUp.VerifyPhone` | Match, mismatch, lock on the last attempt, expired code, expired sign-up, wrong state |
| `SignUp.Complete` | From `PhoneVerified`, retry from `Completed`, refusal from other states |
| `PhoneNumber` | Each supported country, wrong length, unsupported prefix, plus sign, spaces |
| `EmailAddress` | Trim, domain lowercased, local part preserved, missing `@`, missing dot, too long |
| `PersonName` | Trim, internal whitespace collapsed, apostrophe and hyphen, digits refused, empty |
| `VerificationCodeGenerator` | Six digits, leading zeros kept, uniform distribution over a large sample |
| `VerificationCodeDigest` | Same input same digest, different sign-up different digest, constant length |
| `RefreshTokenFactory` | 43-character base64url, unique across a large sample |
| `AccessTokenIssuer` | Claims present, forbidden claims absent, lifetime from options |

The distribution test samples 100,000 codes and asserts each digit position is within 2% of uniform. It guards against a modulo bias.

## Slice tests

A slice test resolves the handler from a scope of the test host and calls `HandleAsync`. It sees the real database, the real options, and the fakes.

Use a slice test when the behavior is about the transaction or a constraint:

- `RegisterUserHandler` returns the same user for a repeated `SignUpId`.
- `CreateSignUpCompletionHandler` creates no user when the session insert fails.
- `CreateSignUpCompletionHandler` revokes the prior bootstrap session on retry.
- `ExpiredCredentialCleanupService.DeleteBatchAsync` never deletes an active session.

Use an HTTP test for everything the client can observe.

## HTTP contract tests

Every route has one test class. Each class asserts:

- The success status and `Content-Type: application/vnd.api+json` without `charset`.
- `jsonapi.version`, `data.type`, `data.id` as strings, `links.self`.
- Each documented refusal with its exact `type`, `status`, and `Content-Type: application/problem+json`.
- `traceId` and `instance` present on every error.
- `Cache-Control: no-store` on every sign-up and session response.
- `Retry-After` on every `429`.
- A validation pointer per invalid attribute.
- `415` for a JSON request body, `406` for `Accept: text/html`, `400` for an unknown attribute.
- French `title` by default and English `title` with `Accept-Language: en`.

`JsonApiAssertions` and `ProblemDetailsAssertions` in `Support/` hold the shared assertions, so each test states only what differs.

## Security tests

| Test | Assertion |
|---|---|
| Expired access token | `401 session-invalid` |
| Wrong issuer, wrong audience, wrong key | `401 session-invalid` |
| Token signed with `none` | `401 session-invalid` |
| Registration token on `/users/me` | `401 session-invalid` |
| Bearer token on `/signups/{id}/completions` | `401 registration-token-invalid` |
| Sign-up token after phone verification | `401 signup-token-invalid` |
| Registration token on another sign-up's route | `401 registration-token-invalid` |
| Consumed refresh token replayed | `401 refresh-token-reused`, the new token also refused |
| Revoked session refresh | `401 session-invalid` |
| Start sign-up for a registered phone | Same status, same attributes, and an SMS recorded |
| Complete with a registered email | `409 email-already-registered` only after phone verification |
| Captured logs | No phone, email, name, code, or token in any event |
| Database rows | No plaintext phone, email, name, code, or token in any column |

The log assertion uses the Serilog test sink the `Testing` environment already writes to. The row assertion reads the tables through Npgsql and searches for the known plaintext values.

## Concurrency tests

Each state-changing slice has one parallel test:

```csharp
[Fact]
public async Task Parallel_completions_create_one_user_and_one_active_session()
{
    var verified = await Api.StartAndVerifyAsync(TestPhones.Next());
    var profile = Profile.Sample();

    var responses = await Task.WhenAll(
        Enumerable.Range(0, 8).Select(_ => Api.CompleteSignUpRawAsync(verified, profile)));

    Assert.All(responses, response => Assert.Contains(response.StatusCode, [HttpStatusCode.Created, HttpStatusCode.OK]));
    Assert.Equal(1, await Api.CountUsersAsync(profile.Email));
    Assert.Equal(1, await Api.CountActiveSessionsAsync(verified.SignUpId));
}
```

Other parallel cases:

- Eight parallel verifications with the right code: one `200`, seven `409` or `401`.
- Eight parallel wrong codes: the attempt count equals the configured maximum, never more.
- Two parallel refreshes with one token: one `200`, one `401`, the family revoked.
- Two notification worker cycles in parallel on one pending row: one send.

## Provider adapter tests

The SMS provider is Bird. `Channels/BirdSmsChannelTests.cs` covers one test per documented provider response: accepted, throttled, invalid recipient, sender rejection, authentication failure, insufficient balance, duplicate key, timeout, transport failure, and malformed bodies. Each asserts the `ChannelResult` and that the request carried the documented fields, the `+`-prefixed recipient, the unchanged idempotency key, and no subject. `Channels/BirdSmsWiringTests.cs` resolves the production keyed SMS registration against a stub handler and proves a delivery cycle persists the provider reference, and that a channel-internal failure is recorded as a retry so the batch still saves. The log test pins that phone, code, body, and API key never reach the logs. The real sandbox is exercised only by the manual `scripts/bird-sms-smoke.sh`, once; see `docs/adr/0004-sms-provider.md`.

## Migration tests

`Migrations/SchemaTests.cs` runs once against a fresh database and asserts the tables, columns, and indexes named in [.NET module design](dotnet-module-design.md#database-model) exist with the documented types. It also asserts that no column uses `float`, `real`, or `double precision`.

The generated migration SQL is reviewed in the pull request. The test proves the migration applies; the review proves it is right.

## Architecture tests

`Architecture/ModuleBoundaryTests.cs` loads the module assemblies and asserts:

- `MoniPay.Sessions` does not reference `MoniPay.Users` or `MoniPay.Notifications`.
- `MoniPay.Users` does not reference `MoniPay.Sessions` or `MoniPay.Notifications`.
- `MoniPay.Notifications` references no other module.
- `MoniPay.Kernel` references no module.

`Architecture/PublicSurfaceTests.cs` asserts the public types of each module equal the list in [.NET module design](dotnet-module-design.md#public-interface). A new public type fails the test until the list is updated.

These use reflection only. No architecture-test package is added.

## Coverage expectations

| Area | Expectation |
|---|---|
| `SignUp` aggregate and value types | Every branch |
| Token generation and digests | Every branch |
| Handlers | Happy path, every refusal, one concurrency case |
| Endpoints | Every documented status |
| Notification worker | Claim, send, retry, fail, expire, lease release |
| Cleanup worker | Deletes expired, keeps active |
| Provider channels | Every documented provider response |

Not tested: EF Core, `Microsoft.Extensions.*`, the JSON:API record constructors, OpenAPI generation internals.

Coverage is measured with the built-in `dotnet test --coverage` of Microsoft.Testing.Platform. The number is a signal for review, not a gate.

## Commands

```bash
dotnet test server/MoniPay.slnx                                            # whole suite, needs Docker
dotnet test server/MoniPay.slnx --filter-class '*CreatePhoneVerificationTests'
dotnet test server/MoniPay.slnx --filter-namespace 'MoniPay.Tests.Sessions'
dotnet test server/MoniPay.slnx --coverage
```

CI runs `dotnet test server/MoniPay.slnx --no-build --configuration Release` on an Ubuntu runner with a Docker daemon. Nothing runs in CI that does not run locally.

## iOS side

The iOS contract cutover has its own tests in `ios/Packages/ApiClient/Tests/`:

- Decoding each JSON:API document in [HTTP contract](http-contract.md) from a fixture file.
- Decoding each Problem Details example into the typed error.
- Mapping every problem type to a `SignUpError` case.
- Refusing to build a local user after a failed completion.

Those tests use Swift Testing and the fixtures are copied from the server contract tests, so both sides read the same JSON.

## Rules

- A new slice arrives with its tests in the same pull request.
- No `Moq`, no `NSubstitute`, no mocking package.
- No `Thread.Sleep`, no `Task.Delay` without the test time provider.
- No shared mutable state between tests except the fixture.
- No assertion on a log message text; assert on the structured fields.
- No test reads a verification code from the database.
- A flaky test is fixed or deleted, never retried by configuration.

## References

- `agents/rules/testing-xunit-testcontainers.md`
- `agents/rules/testing-dotnet-coverage.md`
- `agents/rules/testing-timezone-locale.md`
- `agents/rules/ci-dotnet-pipeline.md`
- [xUnit.net v3](https://xunit.net/docs/getting-started/v3/getting-started)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests)
- [Microsoft.Testing.Platform code coverage](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-extensions-code-coverage)
