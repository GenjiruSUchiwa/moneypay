using System.Globalization;
using Microsoft.Extensions.Localization;
using MoniPay.Kernel;

namespace MoniPay.Sessions.Providers;

internal sealed class VerificationCodeRenderer(IStringLocalizer<SessionMessages> messages)
{
    public string Render(Locale locale, string code, TimeSpan codeLifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale.Value);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(codeLifetime, TimeSpan.Zero);

        string lifetimeMinutes = ((int)Math.Ceiling(codeLifetime.TotalMinutes)).ToString(CultureInfo.InvariantCulture);

        return locale.Invoke(() => messages[SessionMessageKeys.VerificationCodeSms, code, lifetimeMinutes].Value);
    }
}
