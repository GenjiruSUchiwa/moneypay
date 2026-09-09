using MoniPay.Kernel;

namespace MoniPay.Users.Providers;

public sealed record WelcomeMessage(
    UserId UserId,
    EmailAddress Recipient,
    string Subject,
    string Body,
    string IdempotencyKey)
{
    public override string ToString() => nameof(WelcomeMessage);
}
