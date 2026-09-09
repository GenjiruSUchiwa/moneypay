using Microsoft.Extensions.Options;
using MoniPay.Kernel.Security;

namespace MoniPay.Notifications.Security;

internal sealed class RecipientProtector(IOptions<NotificationsOptions> options)
    : PersonalDataProtector(options.Value.DataKey)
{
    private const int MaximumDomainHintLength = 8;

    private const int PhoneHintLength = 4;

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
