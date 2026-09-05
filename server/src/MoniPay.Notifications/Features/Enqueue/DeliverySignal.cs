namespace MoniPay.Notifications;

/// <summary>
/// The commit nudge for the notification worker. A hint, never a queue: what is worth sending
/// is already an outbox row, so a signal lost with the process that raised it costs one poll
/// interval, not the message. One slot, so a burst of commits is one wake-up.
/// </summary>
internal sealed class DeliverySignal
{
    private readonly SemaphoreSlim pending = new(0, 1);

    /// <summary>Records that at least one notification was committed.</summary>
    public void Raise()
    {
        try
        {
            pending.Release();
        }
        catch (SemaphoreFullException)
        {
        }
    }

    /// <summary>
    /// Returns whether a commit happened since the last wait, or gives up after
    /// <paramref name="timeout"/>.
    /// </summary>
    public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return await pending.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
    }
}
