using System.Globalization;

namespace MoniPay.Api.Errors;

/// <summary>
/// Formats a retry delay as invariant whole seconds, rounded up, which is what
/// <c>Retry-After</c>'s delta-seconds form requires.
/// </summary>
internal static class RetryAfterHeader
{
    public static string Format(TimeSpan delay) =>
        ((int)Math.Ceiling(delay.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
}
