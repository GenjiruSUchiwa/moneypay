namespace MoniPay.Sessions.Domain;

/// <summary>Why a session ended. Stored as a string so a reordered enum cannot reinterpret rows.</summary>
internal enum SessionRevokeReason
{
    /// <summary>The user signed out of this device.</summary>
    UserRequest,

    /// <summary>A consumed refresh token was presented again; the whole family is revoked.</summary>
    RefreshTokenReuse,

    /// <summary>A repeated sign-up completion replaced this bootstrap session.</summary>
    BootstrapReplaced,
}
