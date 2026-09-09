using MoniPay.Kernel;

namespace MoniPay.Sessions.Providers;

public sealed record VerificationCodeMessage(
    SignUpId SignUpId,
    PhoneNumber Recipient,
    string Body,
    DateTimeOffset ExpiresAt,
    string IdempotencyKey)
{
    public override string ToString() => nameof(VerificationCodeMessage);
}
