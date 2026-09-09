using Microsoft.Extensions.Logging;

namespace MoniPay.Notifications;

internal static partial class NotificationsLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Notification {NotificationId} ({Kind}, {Channel}, correlation {CorrelationId}, to …{RecipientHint}) sent after {Attempts} attempts")]
    public static partial void Sent(
        ILogger logger, Guid notificationId, string kind, NotificationChannel channel, Guid correlationId, string recipientHint, int attempts);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Notification {NotificationId} ({Kind}, {Channel}, correlation {CorrelationId}, to …{RecipientHint}) retried after attempt {Attempts}: {Code}")]
    public static partial void Retried(
        ILogger logger, Guid notificationId, string kind, NotificationChannel channel, Guid correlationId, string recipientHint, int attempts, string code);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error,
        Message = "Notification {NotificationId} ({Kind}, {Channel}, correlation {CorrelationId}, to …{RecipientHint}) failed after {Attempts} attempts: {Code}")]
    public static partial void Failed(
        ILogger logger, Guid notificationId, string kind, NotificationChannel channel, Guid correlationId, string recipientHint, int attempts, string code);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information,
        Message = "Notification {NotificationId} ({Kind}, {Channel}, correlation {CorrelationId}, to …{RecipientHint}) expired before delivery")]
    public static partial void Expired(
        ILogger logger, Guid notificationId, string kind, NotificationChannel channel, Guid correlationId, string recipientHint);

    [LoggerMessage(EventId = 5, Level = LogLevel.Critical,
        Message = "Required notification {NotificationId} ({Kind}, {Channel}, correlation {CorrelationId}, to …{RecipientHint}) will not be delivered: {Code}")]
    public static partial void RequiredFailed(
        ILogger logger, Guid notificationId, string kind, NotificationChannel channel, Guid correlationId, string recipientHint, string code);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning,
        Message = "No {Channel} channel is registered; pending {Channel} notifications wait in the outbox")]
    public static partial void ChannelNotConfigured(ILogger logger, NotificationChannel channel);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "The notification delivery cycle failed")]
    public static partial void CycleFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "The delivered-notification purge failed")]
    public static partial void PurgeFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Purged {Count} terminal notifications created before {Cutoff}")]
    public static partial void Purged(ILogger logger, int count, DateTimeOffset cutoff);
}
