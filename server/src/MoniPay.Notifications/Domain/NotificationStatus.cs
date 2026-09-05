namespace MoniPay.Notifications;

/// <summary>
/// Where a message stands on its way to the recipient. Public because
/// <see cref="NotificationOutbox.FindLatestStatusAsync"/> returns it to a producing module
/// through a host adapter; the rest of the row stays internal.
/// </summary>
public enum NotificationStatus
{
    Pending,
    Sent,
    Failed,
    Expired,
}
