namespace MoniPay.Kernel;

/// <summary>
/// The rate-limit policy names more than one module attaches. They live here for the reason
/// <see cref="MoniPayRoutes"/> does: a policy the Users routes and the Sessions routes share
/// cannot be named by either module without a sibling reference. The limits behind each name
/// are the host's, from <c>MoniPay:RateLimits</c>.
/// </summary>
public static class MoniPayRateLimitPolicies
{
    /// <summary>
    /// Reading a resource the bearer credential names, partitioned by the session the ticket
    /// carries rather than by the client IP: one stolen or looping client cannot spend the budget
    /// of every caller behind the same NAT.
    /// </summary>
    public const string AuthenticatedRead = "authenticated-read";
}
