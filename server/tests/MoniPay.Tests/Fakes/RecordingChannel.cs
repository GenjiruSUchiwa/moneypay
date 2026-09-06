using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using MoniPay.Notifications.Channels;

namespace MoniPay.Tests.Fakes;

/// <summary>
/// A branch-free channel: records every call and answers with <see cref="Result"/>. A test that
/// needs a hung provider names one <see cref="SlowRecipient"/>; that call waits on the token it
/// was given, so a timeout or a cancelled cycle surfaces exactly as it would from a real
/// transport, while rows left by other tests still complete at once.
/// </summary>
public sealed class RecordingChannel(string providerReference) : INotificationChannel
{
    private readonly ConcurrentQueue<ChannelCall> calls = new();

    public sealed record ChannelCall(string Recipient, string? Subject, string Body, string IdempotencyKey, CancellationToken Token);

    internal ChannelResult Result { get; set; } = new ChannelResult.Accepted(providerReference);

    public string? SlowRecipient { get; set; }

    async Task<ChannelResult> INotificationChannel.SendAsync(string recipient, string? subject, string body, string idempotencyKey, CancellationToken cancellationToken)
    {
        calls.Enqueue(new ChannelCall(recipient, subject, body, idempotencyKey, cancellationToken));
        if (recipient == SlowRecipient)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        return Result;
    }

    public IReadOnlyList<ChannelCall> CallsFor(string recipient) => calls.Where(call => call.Recipient == recipient).ToArray();

    /// <summary>The six digits of the last body sent to a recipient: the only place a test reads a code from.</summary>
    public string CodeFor(string recipient) => Regex.Match(calls.Last(call => call.Recipient == recipient).Body, "[0-9]{6}").Value;
}
