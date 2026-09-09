namespace MoniPay.Users.Persistence;

internal static class UsersSchema
{
    public const string UsersTable = "users";
    public const string UserConsentsTable = "user_consents";

    public const string UsersPrimaryKey = "pk_users";
    public const string UserConsentsPrimaryKey = "pk_user_consents";

    public const string SignUpIdUnique = "ix_users_sign_up_id";
    public const string PhoneLookupHashUnique = "ix_users_phone_lookup_hash";
    public const string EmailLookupHashUnique = "ix_users_email_lookup_hash";

    public const string UserConsentsUserForeignKey = "fk_user_consents_users_user_id";
}
