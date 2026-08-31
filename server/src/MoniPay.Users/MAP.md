# MoniPay.Users

The user profile: the normalized contact data, the locale, and the legal consent a sign-up
recorded. The module holds no route yet — this is the schema and the composition entry point.

- `UsersModule.cs` — `AddUsersModule(services, configuration)`. The only public type of the
  module.
- `Domain/User.cs` — a registered user. Every contact value is stored twice: encrypted so it can
  be read back, and as a keyed hash so it can be looked up without decrypting the table. The
  entity protects nothing itself; `User.Register` takes values the caller already normalized and
  already encrypted or hashed.
- `Domain/UserConsent.cs`, `Domain/LegalDocumentKind.cs` — one accepted legal document per user
  and kind, with the version the client displayed and the server receipt time.
- `Persistence/UserConfiguration.cs`, `UserConsentConfiguration.cs` — the `users` and
  `user_consents` tables: snake_case, `timestamptz`, the document kind stored by name.
- `Persistence/UsersConstraints.cs` — the names PostgreSQL knows the tables, indexes and the
  foreign key by. A unique violation is mapped back to a refusal by name, so the name is a
  constant and a rename breaks the build.
- `Persistence/UserSets.cs` — the module's `DbSet` accessors on the shared context.

The tables are created by `AddUsers` in `MoniPay.Data/Migrations/`: one database, one ordered
history, whichever module prompted the change.

References `Kernel`, `Data`.
