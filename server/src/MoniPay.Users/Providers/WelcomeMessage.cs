using MoniPay.Kernel;

namespace MoniPay.Users.Providers;

/// <summary>
/// One welcome email to deliver, in its raw parts: Users renders the localized subject and body
/// from the user's stored locale, so the host adapter never formats text. The stable
/// <see cref="IdempotencyKey"/> admits one delivery per user, and the message carries no
/// notification-module type.
/// </summary>
public sealed record WelcomeMessage(
    UserId UserId,
    EmailAddress Recipient,
    string Subject,
    string Body,
    string IdempotencyKey)
{
    public override string ToString() => nameof(WelcomeMessage);
}
