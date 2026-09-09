using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MoniPay.Api.Errors;
using MoniPay.Kernel;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Sessions.Features.SignUps;

namespace MoniPay.Api.Hosting;

/// <summary>
/// The per-route IP limits, bound from <c>MoniPay:RateLimits</c>. Each policy's default is the
/// documented one; the key a deployment overrides is declared, because a mistyped key would
/// silently fall back to the default and a mistyped *intent* is exactly what must fail.
/// </summary>
internal sealed class RateLimitOptions
{
    public const string SectionName = "MoniPay:RateLimits";

    /// <summary>Starting a sign-up or a sign-in, per client IP per hour.</summary>
    public int StartPerHour { get; set; } = 20;

    /// <summary>Resending a verification code, per client IP per hour.</summary>
    public int ResendPerHour { get; set; } = 30;

    /// <summary>Checking a verification code, per client IP per hour.</summary>
    public int VerifyPerHour { get; set; } = 60;

    /// <summary>Completing a sign-up, per client IP per hour.</summary>
    public int CompletePerHour { get; set; } = 20;

    /// <summary>Refreshing a session, per client IP per hour.</summary>
    public int RefreshPerHour { get; set; } = 120;

    /// <summary>Reading a resource the bearer credential names, per session per hour.</summary>
    public int AuthenticatedReadPerHour { get; set; } = 600;

    /// <summary>The configuration keys, declared so a mistyped key is a compile error.</summary>
    internal static class Keys
    {
        public const string StartPerHour = $"{SectionName}:{nameof(StartPerHour)}";
        public const string ResendPerHour = $"{SectionName}:{nameof(ResendPerHour)}";
        public const string VerifyPerHour = $"{SectionName}:{nameof(VerifyPerHour)}";
        public const string CompletePerHour = $"{SectionName}:{nameof(CompletePerHour)}";
        public const string RefreshPerHour = $"{SectionName}:{nameof(RefreshPerHour)}";
        public const string AuthenticatedReadPerHour = $"{SectionName}:{nameof(AuthenticatedReadPerHour)}";
    }
}

/// <summary>
/// Composes the fixed-window limiters the modules' route groups attach by name, one window of
/// one hour, partitioned by the client IP the forwarded headers established. The rejection is a
/// bare 429 with <c>Retry-After</c>; the body comes with the shared Problem Details writer.
/// </summary>
internal sealed class RateLimiterSetup(IOptions<RateLimitOptions> limits) : IConfigureOptions<RateLimiterOptions>
{
    public void Configure(RateLimiterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = static (rejected, _) =>
        {
            HttpResponse response = rejected.HttpContext.Response;
            response.StatusCode = StatusCodes.Status429TooManyRequests;
            if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
            {
                response.Headers.RetryAfter = RetryAfterHeader.Format(retryAfter);
            }

            return ValueTask.CompletedTask;
        };

        AddPolicy(options, SignUpRateLimitPolicies.Start, limits.Value.StartPerHour);
        AddPolicy(options, SignUpRateLimitPolicies.Resend, limits.Value.ResendPerHour);
        AddPolicy(options, SignUpRateLimitPolicies.Verify, limits.Value.VerifyPerHour);
        AddPolicy(options, SignUpRateLimitPolicies.Complete, limits.Value.CompletePerHour);
        AddPolicy(options, SessionRateLimitPolicies.Refresh, limits.Value.RefreshPerHour);
        AddPolicy(
            options,
            MoniPayRateLimitPolicies.AuthenticatedRead,
            limits.Value.AuthenticatedReadPerHour,
            PerSession);
    }

    /// <summary>
    /// The session the presented ticket names, so one looping client spends its own budget and
    /// not that of every caller sharing its address. A request with no readable session has not
    /// authenticated yet and falls back to the client IP.
    /// </summary>
    private static string PerSession(HttpContext httpContext) =>
        httpContext.User.FindFirst(MoniPayClaimTypes.SessionId)?.Value ?? PerClientIp(httpContext);

    private static string PerClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static void AddPolicy(
        RateLimiterOptions options,
        string name,
        int limitPerHour,
        Func<HttpContext, string>? partition = null) =>
        options.AddPolicy(name, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                (partition ?? PerClientIp)(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limitPerHour,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                }));
}
