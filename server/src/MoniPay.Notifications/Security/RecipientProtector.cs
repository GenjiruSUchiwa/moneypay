using Microsoft.Extensions.Options;
using MoniPay.Kernel.Security;

namespace MoniPay.Notifications.Security;

/// <summary>
/// The kernel protector under the notifications data key. It covers everything a message
/// carries — the recipient, the subject and the body, which for a verification code is the
/// code itself — with the same AES-GCM layout as every other module's protector.
/// </summary>
internal sealed class RecipientProtector(IOptions<NotificationsOptions> options)
    : PersonalDataProtector(options.Value.DataKey)
{
    private const int MaximumDomainHintLength = 8;

    private const int PhoneHintLength = 4;

    /// <summary>
    /// The support-visible sliver of a recipient: the last four digits of a phone, or the email
    /// domain of at most eight characters. Logs and support screens carry this, never the
    /// recipient itself.
    /// </summary>
    public static string Hint(string recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        int domainStart = recipient.LastIndexOf('@');
        if (domainStart >= 0)
        {
            string domain = recipient[(domainStart + 1)..];
            return domain.Length <= MaximumDomainHintLength ? domain : domain[..MaximumDomainHintLength];
        }

        string digits = new([.. recipient.Where(char.IsDigit)]);
        return digits.Length <= PhoneHintLength ? digits : digits[^PhoneHintLength..];
    }
}
