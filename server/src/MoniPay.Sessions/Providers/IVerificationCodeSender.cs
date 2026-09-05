using MoniPay.Kernel;

namespace MoniPay.Sessions.Providers;

/// <summary>
/// The delivery port the host implements over the notification outbox. The contract is
/// transactional: <see cref="EnqueueAsync"/> only adds rows to the caller's scoped
/// <c>MoniPayDbContext</c>, so the handler's <c>SaveChangesAsync</c> commits the sign-up and its
/// message together, and a rollback leaves neither.
/// </summary>
public interface IVerificationCodeSender
{
    Task EnqueueAsync(VerificationCodeMessage message, CancellationToken cancellationToken);

    /// <summary>The state of the latest message queued for a sign-up, or <c>null</c> when there is none.</summary>
    Task<CodeDeliveryState?> GetLatestDeliveryAsync(SignUpId signUpId, CancellationToken cancellationToken);
}
