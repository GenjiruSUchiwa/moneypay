namespace MoniPay.Notifications.Channels;

/// <summary>
/// What a provider channel answers, reduced to the three outcomes the worker acts on. The channel
/// maps every provider status onto one of these; the processor never reads a provider payload.
/// </summary>
internal abstract record ChannelResult
{
    private ChannelResult()
    {
    }

    /// <summary>The provider took the message; <paramref name="ProviderReference"/> is its handle.</summary>
    public sealed record Accepted(string ProviderReference) : ChannelResult;

    /// <summary>A transient failure — a timeout, a 5xx, a lost response — worth another attempt.</summary>
    public sealed record Retry(string Code) : ChannelResult;

    /// <summary>A permanent refusal — a bad recipient, a blocked destination — not worth another attempt.</summary>
    public sealed record Rejected(string Code) : ChannelResult;
}
