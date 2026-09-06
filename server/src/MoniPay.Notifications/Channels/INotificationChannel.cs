namespace MoniPay.Notifications.Channels;

/// <summary>
/// A provider behind the worker, registered keyed by <see cref="NotificationChannel"/>. A channel
/// returns, it never throws: a transport exception, a timeout or a malformed body becomes
/// <see cref="ChannelResult.Retry"/> or <see cref="ChannelResult.Rejected"/> inside the channel.
/// Only the caller's cancellation propagates, as <see cref="OperationCanceledException"/>.
/// </summary>
internal interface INotificationChannel
{
    /// <param name="subject">Null for a channel without one, such as SMS.</param>
    Task<ChannelResult> SendAsync(
        string recipient,
        string? subject,
        string body,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
