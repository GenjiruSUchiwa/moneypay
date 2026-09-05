namespace MoniPay.Notifications;

/// <summary>
/// The delivery channel a message travels by. Only the channels a provider backs exist; push
/// and in-app need their own design after device registration lands.
/// </summary>
public enum NotificationChannel
{
    Sms,
    Email,
}
