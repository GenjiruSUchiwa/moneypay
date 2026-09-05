using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.VerifyPhone;

/// <summary>The one moment the raw registration token exists.</summary>
internal sealed record CreatePhoneVerificationResult(
    SignUpId SignUpId,
    string RegistrationToken,
    DateTimeOffset SignUpExpiresAt)
{
    // The record's generated ToString would print the token; a logged token is a leaked credential.
    public override string ToString() => nameof(CreatePhoneVerificationResult);
}
