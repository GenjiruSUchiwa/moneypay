using System.Globalization;

namespace MoniPay.Api.Errors;

internal static class RetryAfterHeader
{
    public static string Format(TimeSpan delay) =>
        ((int)Math.Ceiling(delay.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
}
