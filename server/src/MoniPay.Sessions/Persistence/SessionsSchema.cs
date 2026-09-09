using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Persistence;

internal static class SessionsSchema
{
    public const string SignUpsTable = "sign_ups";

    public const string SignUpsPrimaryKey = "pk_sign_ups";

    public const string SignUpPhoneActiveWorkflowUnique = "ix_sign_ups_phone_lookup_hash";

    public const string SignUpStatusExpiryIndex = "ix_sign_ups_status_expires_at";

    public const string SignUpTokenDigestUnique = "ix_sign_ups_signup_token_digest";

    public const string SignUpRegistrationTokenDigestUnique = "ix_sign_ups_registration_token_digest";

    public const string SessionsTable = "sessions";

    public const string SessionsPrimaryKey = "pk_sessions";

    public const string SessionUserRevokedIndex = "ix_sessions_user_id_revoked_at";

    public const string SessionFamilyDeviceUnique = "ix_sessions_token_family_id_device_id";

    public const string RefreshTokensTable = "refresh_tokens";

    public const string RefreshTokensPrimaryKey = "pk_refresh_tokens";

    public const string RefreshTokenDigestUnique = "ix_refresh_tokens_token_digest";

    public const string RefreshTokenActivePerSessionUnique = "ix_refresh_tokens_session_id_active";

    public const string RefreshTokenExpiryIndex = "ix_refresh_tokens_expires_at";

    public static readonly IReadOnlyList<SignUpStatus> ActiveStatuses =
        [SignUpStatus.CodePending, SignUpStatus.PhoneVerified, SignUpStatus.Locked];

    public static readonly string ActiveStatusFilter =
        $"status IN ({string.Join(", ", ActiveStatuses.Select(status => $"'{status}'"))})";
}
