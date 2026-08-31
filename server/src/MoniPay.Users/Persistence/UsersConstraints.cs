namespace MoniPay.Users.Persistence;

/// <summary>
/// The names PostgreSQL knows this module's tables and constraints by. A unique-violation is
/// mapped back to a domain refusal by name, so the name is a constant here rather than a
/// literal at the mapping site: renaming an index then breaks the build instead of a client.
/// </summary>
internal static class UsersConstraints
{
    public const string UsersTable = "users";
    public const string UserConsentsTable = "user_consents";

    public const string SignUpIdUnique = "ix_users_sign_up_id";
    public const string PhoneLookupHashUnique = "ix_users_phone_lookup_hash";
    public const string EmailLookupHashUnique = "ix_users_email_lookup_hash";

    public const string UserConsentsUserForeignKey = "fk_user_consents_users_user_id";
}
