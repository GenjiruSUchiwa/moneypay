# MoniPay.Users

The user profile: the normalized contact data, the locale, and the legal consent a sign-up
recorded. The module holds no route yet.

- `UsersModule.cs` — `AddUsersModule(services, configuration)`. The only public type of the
  module.
- `UsersOptions.cs` — the module configuration: `MoniPay:Users:PersonalDataKeyBase64`, refused
  at startup (`ValidateOnStart`) when missing or not a 32-byte key. A host that cannot encrypt
  must not come up.
- `Security/UserPersonalDataProtector.cs` — AES-GCM over the personal-data key for first name,
  last name, phone and email. The payload starts with an authenticated one-byte key version so
  a later re-encryption migration can find rows by key.
- `Security/UserLookupDigest.cs` — HMAC-SHA256 over the normalized contact value, keyed by an
  HKDF derivation of the personal-data key: lookup and encryption never share key material
  directly.
- `Features/Registration/RegisterUserHandler.cs`, `RegisterUserCommand.cs`, `RegisteredUser.cs` —
  the slice sign-up completion calls: protect the contact data, insert the user and its two
  consents, idempotent on `sign_up_id`. It saves inside the caller's ambient transaction and
  maps a unique violation by constraint name — `sign_up_id` back to the existing user, the
  lookup hashes to `phone-already-registered` / `email-already-registered`.
- `Domain/User.cs` — a registered user. `User.Register` takes values the caller already
  normalized and already encrypted or hashed, and records the two legal consents itself: a user
  cannot exist without them, and a consent cannot exist without its user.
- `Domain/Ciphertext.cs`, `LookupHash.cs`, `ProtectedContact.cs` — the types a personal value
  has once protected. A plaintext `string` does not fit the entity: only the protector (#84)
  produces a `Ciphertext`. A contact is one value in the domain and two columns in the table —
  the ciphertext to read back, the keyed hash to look up and keep unique — mapped as an owned
  type so the hash can carry the unique index.
- `Domain/UserConsent.cs`, `Domain/LegalDocumentKind.cs` — one accepted legal document per user
  and kind, with the version the client displayed and the server receipt time.
- `Persistence/UserConfiguration.cs`, `UserConsentConfiguration.cs` — the `users` and
  `user_consents` tables: snake_case, `timestamptz`, the document kind stored by name. The
  kernel types (`UserId`, `SignUpId`, `Locale`) need no conversion here — `MoniPay.Data`
  declares it once for every module. Deleting
  a user is refused while its consents exist: the consent row is legal evidence, and erasing it
  is a decision, not a side effect.
- `Persistence/UsersSchema.cs` — the names PostgreSQL knows the tables, keys, indexes and the
  foreign key by. A unique violation is mapped back to a refusal by name, so the name is a
  constant and a rename breaks the build.
- `Persistence/UserSets.cs` — the module's `DbSet` accessors on the shared context.

The tables are created by `AddUsers` in `MoniPay.Data/Migrations/`: one database, one ordered
history, whichever module prompted the change.

References `Kernel`, `Data`.
