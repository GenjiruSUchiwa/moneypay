using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.Start;

internal sealed record StartSignUpCommand(
    PhoneNumber Phone,
    Locale Locale,
    string TermsVersion,
    string PrivacyVersion);
