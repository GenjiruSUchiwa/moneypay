namespace MoniPay.Notifications.Channels;

internal static class BirdSmsCodes
{
    public const string TransportError = "sms-transport-error";
    public const string ProtocolError = "sms-protocol-error";
    public const string Rejected = "sms-rejected";
    public const string Unauthorized = "sms-unauthorized";
    public const string InsufficientBalance = "sms-insufficient-balance";
    public const string InvalidRecipient = "sms-invalid-recipient";
    public const string SenderRejected = "sms-sender-rejected";
    public const string DuplicateInflight = "sms-duplicate-inflight";
    public const string RateLimited = "sms-rate-limited";
    public const string Unavailable = "sms-unavailable";
}
