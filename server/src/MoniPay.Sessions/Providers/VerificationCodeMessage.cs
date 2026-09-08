using MoniPay.Kernel;

namespace MoniPay.Sessions.Providers;

/// <summary>
/// One rendered verification code to deliver. The caller localizes the body through the
/// single composition point, so the host adapter never formats text. The message is
/// worthless after <see cref="ExpiresAt"/>, which equals the code's own expiry, and a replay of
/// one <see cref="IdempotencyKey"/> must not send twice.
/// </summary>
public sealed record VerificationCodeMessage(
    SignUpId SignUpId,
    PhoneNumber Recipient,
    string Body,
    DateTimeOffset ExpiresAt,
    string IdempotencyKey)
{
    // The record's generated ToString would print the phone and the rendered body containing the code;
    // a logged message is a leak.
    public override string ToString() => nameof(VerificationCodeMessage);
}
