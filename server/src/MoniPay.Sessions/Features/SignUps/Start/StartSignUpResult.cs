using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.Start;

/// <summary>
/// The one moment the raw sign-up token exists. The shape is the same for a new, an in-flight
/// and a registered phone, so a start reveals nothing about the phone.
/// </summary>
internal sealed record StartSignUpResult(
    SignUpId SignUpId,
    string SignUpToken,
    DateTimeOffset CodeExpiresAt,
    DateTimeOffset CanResendAt,
    DateTimeOffset SignUpExpiresAt)
{
    // The record's generated ToString would print the token; a logged token is a leaked credential.
    public override string ToString() => nameof(StartSignUpResult);
}
