using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MoniPay.Api.Errors;
using MoniPay.Kernel;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Sessions.Features.SignUps;

namespace MoniPay.Api.Hosting;

internal sealed class RateLimitOptions
{
    public const string SectionName = "MoniPay:RateLimits";

    public int StartPerHour { get; set; } = 20;

    public int ResendPerHour { get; set; } = 30;

    public int VerifyPerHour { get; set; } = 60;

    public int CompletePerHour { get; set; } = 20;

    public int RefreshPerHour { get; set; } = 120;

    public int AuthenticatedReadPerHour { get; set; } = 600;

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
