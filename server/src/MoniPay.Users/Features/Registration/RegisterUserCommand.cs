using MoniPay.Kernel;

namespace MoniPay.Users.Features.Registration;

public sealed record RegisterUserCommand(
    SignUpId SignUpId,
    UserId UserId,
    PhoneNumber Phone,
    PersonName FirstName,
    PersonName LastName,
    EmailAddress Email,
    Locale Locale,
    string TermsVersion,
    string PrivacyVersion,
    DateTimeOffset AcceptedAt);
