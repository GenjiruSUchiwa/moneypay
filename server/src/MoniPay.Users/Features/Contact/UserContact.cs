using MoniPay.Kernel;

namespace MoniPay.Users.Features.Contact;

public sealed record UserContact(PhoneNumber Phone, EmailAddress Email, Locale Locale)
{
    public override string ToString() => nameof(UserContact);
}
