namespace MoniPay.Sessions.Features.SignUps;

/// <summary>
/// The names of the IP rate-limit policies the sign-up routes attach to their groups. The names
/// are public because the host composes the limiters they name; the limits behind each name are
/// the host's, from <c>MoniPay:RateLimits</c>. These policies guard anonymous traffic by client
/// IP; the persistent per-phone limits stay the authoritative layer.
/// </summary>
public static class SignUpRateLimitPolicies
{
    /// <summary>Starting a sign-up: 20 per hour per client IP.</summary>
    public const string Start = "sign-up-start";

    /// <summary>Resending a verification code: 30 per hour per client IP.</summary>
    public const string Resend = "sign-up-resend";

    /// <summary>Checking a verification code: 60 per hour per client IP.</summary>
    public const string Verify = "sign-up-verify";

    /// <summary>Completing a sign-up: 20 per hour per client IP.</summary>
    public const string Complete = "sign-up-complete";
}
