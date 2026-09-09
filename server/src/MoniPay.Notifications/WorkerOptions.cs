namespace MoniPay.Notifications;

internal sealed class WorkerOptions
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DefaultLeaseDuration = TimeSpan.FromMinutes(1);

    private const int DefaultBatchSize = 50;

    public bool Enabled { get; set; } = true;

    public TimeSpan PollInterval { get; set; } = DefaultPollInterval;

    public int BatchSize { get; set; } = DefaultBatchSize;

    public TimeSpan LeaseDuration { get; set; } = DefaultLeaseDuration;
}
