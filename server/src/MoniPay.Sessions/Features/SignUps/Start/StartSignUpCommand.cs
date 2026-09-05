using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.Start;

/// <summary>A start request after the edge validated it: the phone and the locale are normalized values.</summary>
internal sealed record StartSignUpCommand(
    PhoneNumber Phone,
    Locale Locale,
    string TermsVersion,
    string PrivacyVersion);
