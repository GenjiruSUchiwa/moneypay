using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.Complete;

public sealed record ProvisionUserRequest(
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
