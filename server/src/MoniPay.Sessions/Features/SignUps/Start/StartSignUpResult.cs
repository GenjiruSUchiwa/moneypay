using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.Start;

internal sealed record StartSignUpResult(
    SignUpId SignUpId,
    string SignUpToken,
    DateTimeOffset CodeExpiresAt,
    DateTimeOffset CanResendAt,
    DateTimeOffset SignUpExpiresAt)
{
    public override string ToString() => nameof(StartSignUpResult);
}
