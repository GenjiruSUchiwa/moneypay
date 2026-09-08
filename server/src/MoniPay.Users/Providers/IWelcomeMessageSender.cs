namespace MoniPay.Users.Providers;

/// <summary>
/// The delivery port the host implements over the notification outbox. The contract is
/// transactional: <see cref="EnqueueAsync"/> only adds a row to the caller's scoped
/// <c>MoniPayDbContext</c> and never saves, so the handler's own save commits the welcome in the
/// caller's ambient transaction, and a rollback leaves neither. A failure is the caller's to
/// absorb: registration continues without the optional welcome.
/// </summary>
public interface IWelcomeMessageSender
{
    Task EnqueueAsync(WelcomeMessage message, CancellationToken cancellationToken);
}
