using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Persistence;

/// <summary>
/// The names PostgreSQL knows this module's table, keys and indexes by. A unique violation is
/// mapped back to a refusal by name, so each name is a constant here rather than a literal at
/// the mapping site: a renamed index breaks the build instead of a running client.
/// </summary>
internal static class SessionsSchema
{
    public const string SignUpsTable = "sign_ups";

    public const string SignUpsPrimaryKey = "pk_sign_ups";

    /// <summary>One active workflow per phone, in any nonterminal state.</summary>
    public const string SignUpPhoneActiveWorkflowUnique = "ix_sign_ups_phone_lookup_hash";

    /// <summary>The cleanup worker's scan: expired workflows by status.</summary>
    public const string SignUpStatusExpiryIndex = "ix_sign_ups_status_expires_at";

    public const string SignUpTokenDigestUnique = "ix_sign_ups_signup_token_digest";

    public const string SignUpRegistrationTokenDigestUnique = "ix_sign_ups_registration_token_digest";

    public const string SessionsTable = "sessions";

    public const string SessionsPrimaryKey = "pk_sessions";

    /// <summary>The active-sessions listing and the family revocation scan.</summary>
    public const string SessionUserRevokedIndex = "ix_sessions_user_id_revoked_at";

    public const string SessionFamilyDeviceUnique = "ix_sessions_token_family_id_device_id";

    public const string RefreshTokensTable = "refresh_tokens";

    public const string RefreshTokensPrimaryKey = "pk_refresh_tokens";

    public const string RefreshTokenDigestUnique = "ix_refresh_tokens_token_digest";

    /// <summary>
    /// One unconsumed token per session. An expired token that was never used still holds the
    /// slot: rotation consumes the old link before it issues the next one.
    /// </summary>
    public const string RefreshTokenActivePerSessionUnique = "ix_refresh_tokens_session_id_active";

    /// <summary>The cleanup worker's scan.</summary>
    public const string RefreshTokenExpiryIndex = "ix_refresh_tokens_expires_at";

    /// <summary>
    /// The statuses a phone may still own a workflow for; completion or expiry frees the phone.
    /// Locked stays in the list so locking cannot be escaped by starting a fresh sign-up
    /// with a fresh attempt budget on the same phone. The start slice looks an active sign-up up
    /// by the same list, so what the index refuses is exactly what the handler reuses.
    /// </summary>
    public static readonly IReadOnlyList<SignUpStatus> ActiveStatuses =
        [SignUpStatus.CodePending, SignUpStatus.PhoneVerified, SignUpStatus.Locked];

    public static readonly string ActiveStatusFilter =
        $"status IN ({string.Join(", ", ActiveStatuses.Select(status => $"'{status}'"))})";
}
