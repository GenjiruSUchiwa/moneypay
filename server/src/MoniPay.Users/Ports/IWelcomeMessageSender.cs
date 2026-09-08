namespace MoniPay.Users.Ports;

/// <summary>
/// The delivery port the host implements over the notification outbox. The contract is
/// transactional: <see cref="EnqueueAsync"/> only adds a row to the caller's scoped
/// <c>MoniPayDbContext</c>, so the registration's <c>SaveChangesAsync</c> commits the user and
/// its welcome together, and a rollback leaves neither. An enqueue that throws leaves no
/// tracked row, so a failed optional welcome never leaks into a later save.
/// </summary>
public interface IWelcomeMessageSender
{
    Task EnqueueAsync(WelcomeMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Detaches a message <see cref="EnqueueAsync"/> staged but the registration will not commit
    /// — the caller lost a race after staging. A no-op when nothing was staged.
    /// </summary>
    void Discard(WelcomeMessage message);
}
