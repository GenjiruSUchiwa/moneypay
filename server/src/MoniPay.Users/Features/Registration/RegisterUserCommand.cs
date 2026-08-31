using MoniPay.Kernel;

namespace MoniPay.Users.Features.Registration;

/// <summary>
/// Everything registration needs, already normalized by the caller. The document versions are
/// the ones the client displayed; <see cref="AcceptedAt"/> is the server receipt time, stamped
/// on the user and on both consents.
/// </summary>
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
