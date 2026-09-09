namespace MoniPay.Notifications.Channels;

internal interface INotificationChannel
{
    Task<ChannelResult> SendAsync(
        string recipient,
        string? subject,
        string body,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
