using System.Globalization;
using Microsoft.Extensions.Localization;
using MoniPay.Kernel;
using MoniPay.Users.Ports;

namespace MoniPay.Users.Features.Registration;

/// <summary>
/// Renders the welcome email from the validated registration, once, before enqueue. Users owns
/// the text, so the host adapter never formats it: the adapter only carries the already
/// localized <see cref="WelcomeMessage.Subject"/> and <see cref="WelcomeMessage.Body"/>.
/// </summary>
internal sealed class WelcomeMessageRenderer(IStringLocalizer<UserMessages> messages)
{
    private const string IdempotencyKeyPrefix = "welcome";

    /// <summary>
    /// The welcome for a user, in the locale the registration was validated with — the same
    /// value stored on the user, never the request or worker culture. The key is stable and
    /// culture-independent: one delivery per user, and it names neither the recipient nor the name.
    /// </summary>
    public WelcomeMessage Render(UserId userId, EmailAddress recipient, PersonName firstName, Locale locale)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, default);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale.Value);

        CultureInfo culture = CultureInfo.GetCultureInfo(locale.Value);
        CultureInfo currentCulture = CultureInfo.CurrentCulture;
        CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            return new WelcomeMessage(
                userId,
                recipient,
                messages[UserMessageKeys.WelcomeEmailSubject].Value,
                messages[UserMessageKeys.WelcomeEmailBody, firstName.Value].Value,
                FormattableString.Invariant($"{IdempotencyKeyPrefix}:{userId}"));
        }
        finally
        {
            CultureInfo.CurrentCulture = currentCulture;
            CultureInfo.CurrentUICulture = currentUICulture;
        }
    }
}
