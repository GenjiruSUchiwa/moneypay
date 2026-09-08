using System.Globalization;
using Microsoft.Extensions.Localization;
using MoniPay.Kernel;

namespace MoniPay.Sessions.Providers;

/// <summary>
/// Renders the verification SMS from its raw parts. Sessions owns the text, so the host adapter
/// never formats it: the adapter only carries the already localized <see cref="VerificationCodeMessage.Body"/>.
/// </summary>
internal sealed class VerificationCodeRenderer(IStringLocalizer<SessionMessages> messages)
{
    /// <summary>
    /// The localized body for a code and its lifetime, in the recipient's stored locale. The code
    /// stays a string so leading zeroes survive; the lifetime is the same configured lifetime the
    /// sign-up row uses, displayed in whole minutes rounded up so a partial minute is never
    /// silently truncated to a shorter display.
    /// </summary>
    public string Render(Locale locale, string code, TimeSpan codeLifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale.Value);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(codeLifetime, TimeSpan.Zero);

        CultureInfo culture = CultureInfo.GetCultureInfo(locale.Value);
        string lifetimeMinutes = ((int)Math.Ceiling(codeLifetime.TotalMinutes)).ToString(CultureInfo.InvariantCulture);

        CultureInfo currentCulture = CultureInfo.CurrentCulture;
        CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            return messages[SessionMessageKeys.VerificationCodeSms, code, lifetimeMinutes].Value;
        }
        finally
        {
            CultureInfo.CurrentCulture = currentCulture;
            CultureInfo.CurrentUICulture = currentUICulture;
        }
    }
}
