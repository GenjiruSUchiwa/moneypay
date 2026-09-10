namespace MoniPay.Notifications.Channels;

internal interface INotificationChannel
{
    Task<ChannelResult> SendAsync(
        Guid notificationId,
        string recipient,
        string? subject,
        string body,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
