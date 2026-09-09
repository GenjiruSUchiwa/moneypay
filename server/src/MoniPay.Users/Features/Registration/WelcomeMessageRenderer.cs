using Microsoft.Extensions.Localization;
using MoniPay.Kernel;
using MoniPay.Users.Providers;

namespace MoniPay.Users.Features.Registration;

internal sealed class WelcomeMessageRenderer(IStringLocalizer<UserMessages> messages)
{
    private const string IdempotencyKeyPrefix = "welcome";

    public WelcomeMessage Render(UserId userId, EmailAddress recipient, PersonName firstName, Locale locale)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, default);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale.Value);

        return locale.Invoke(() => new WelcomeMessage(
            userId,
            recipient,
            messages[UserMessageKeys.WelcomeEmailSubject].Value,
            messages[UserMessageKeys.WelcomeEmailBody, firstName.Value].Value,
            FormattableString.Invariant($"{IdempotencyKeyPrefix}:{userId}")));
    }
}
