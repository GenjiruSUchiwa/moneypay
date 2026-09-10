namespace MoniPay.Notifications.Channels;

internal abstract record ChannelResult
{
    public const string ProviderTimeout = "provider-timeout";

    private ChannelResult()
    {
    }

    public sealed record Accepted(string ProviderReference) : ChannelResult;

    public sealed record Retry(string Code) : ChannelResult;

    public sealed record Rejected(string Code) : ChannelResult;
}
