using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.Complete;

/// <summary>
/// Everything user provisioning needs, already normalized: the phone is the verified one from
/// the sign-up, the consent versions are the ones the client displayed at start, and
/// <see cref="AcceptedAt"/> is the server receipt time.
/// </summary>
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
