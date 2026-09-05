namespace MoniPay.Sessions.Providers;

/// <summary>
/// Where the latest verification-code message stands, as the delivery side reports it. The
/// client polls it to tell a slow network from a failed delivery before it offers a resend.
/// </summary>
public enum CodeDeliveryState
{
    Queued,
    Sent,
    Failed,
    Expired,
}
