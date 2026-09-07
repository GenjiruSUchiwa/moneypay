namespace MoniPay.Sessions.Features.Sessions;

/// <summary>
/// The names of the IP rate-limit policies the session routes attach to their groups. The names
/// are public because the host composes the limiters they name; the limits behind each name are
/// the host's, from <c>MoniPay:RateLimits</c>. Rotation and replay rules — not an IP budget —
/// remain the authority on refresh abuse.
/// </summary>
public static class SessionRateLimitPolicies
{
    /// <summary>Refreshing a session: 120 per hour per client IP.</summary>
    public const string Refresh = "session-refresh";
}
