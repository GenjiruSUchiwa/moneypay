using MoniPay.Kernel;

namespace MoniPay.Sessions.Providers;

/// <summary>
/// One verification code to deliver, in its raw parts plus its rendered body: the adapter's
/// caller renders the localized body from <see cref="Code"/> and <see cref="CodeLifetime"/> through
/// the single composition point, so the host adapter never formats text. The message is
/// worthless after <see cref="ExpiresAt"/>, which equals the code's own expiry, and a replay of
/// one <see cref="IdempotencyKey"/> must not send twice.
/// </summary>
public sealed record VerificationCodeMessage(
    SignUpId SignUpId,
    PhoneNumber Recipient,
    string Code,
    TimeSpan CodeLifetime,
    Locale Locale,
    string Body,
    DateTimeOffset ExpiresAt,
    string IdempotencyKey)
{
    // The record's generated ToString would print the code, the phone, and the rendered body;
    // a logged message is a leak.
    public override string ToString() => nameof(VerificationCodeMessage);
}
