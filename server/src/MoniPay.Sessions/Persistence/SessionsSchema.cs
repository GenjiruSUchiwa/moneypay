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
