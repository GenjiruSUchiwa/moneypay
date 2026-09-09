using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Notifications;
using MoniPay.Sessions.Providers;

namespace MoniPay.Api.Composition;

internal sealed class VerificationCodeDeliveryAdapter(NotificationOutbox outbox) : IVerificationCodeSender
{
    private const string VerificationCodeKind = "VerificationCode";

    private const string ProviderName = "sms";

    public Task EnqueueAsync(VerificationCodeMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            outbox.Enqueue(new OutboundMessage(
                NotificationChannel.Sms,
                message.Recipient.Value,
                null,
                message.Body,
                VerificationCodeKind,
                true,
                message.IdempotencyKey,
                message.ExpiresAt,
                message.SignUpId.Value));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception failure) when (failure is not ProviderUnavailableException)
        {
            throw new ProviderUnavailableException(ProviderName, null, failure);
        }

        return Task.CompletedTask;
    }

    public async Task<CodeDeliveryState?> GetLatestDeliveryAsync(SignUpId signUpId, CancellationToken cancellationToken)
    {
        NotificationStatus? status = await outbox
            .FindLatestStatusAsync(signUpId.Value, VerificationCodeKind, cancellationToken)
            .ConfigureAwait(false);

        return status switch
        {
            null => null,
            NotificationStatus.Pending => CodeDeliveryState.Queued,
            NotificationStatus.Sent => CodeDeliveryState.Sent,
            NotificationStatus.Failed => CodeDeliveryState.Failed,
            NotificationStatus.Expired => CodeDeliveryState.Expired,
            _ => throw new InvalidOperationException($"Unknown notification status {status}."),
        };
    }
}
