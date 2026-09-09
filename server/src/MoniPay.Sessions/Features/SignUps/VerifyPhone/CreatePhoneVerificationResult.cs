using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.VerifyPhone;

internal sealed record CreatePhoneVerificationResult(
    SignUpId SignUpId,
    string RegistrationToken,
    DateTimeOffset SignUpExpiresAt)
{
    public override string ToString() => nameof(CreatePhoneVerificationResult);
}
