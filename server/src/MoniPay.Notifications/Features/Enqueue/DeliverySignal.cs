namespace MoniPay.Notifications;

internal sealed class DeliverySignal
{
    private readonly SemaphoreSlim pending = new(0, 1);

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

    public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return await pending.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
    }
}
