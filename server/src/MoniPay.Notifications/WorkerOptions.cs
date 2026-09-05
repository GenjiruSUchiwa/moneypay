namespace MoniPay.Notifications;

/// <summary>
/// The worker's settings: how often it wakes, how many rows it claims at once, and how long a
/// claim holds. No worker exists yet; the delivery slice reads these.
/// </summary>
internal sealed class WorkerOptions
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DefaultLeaseDuration = TimeSpan.FromMinutes(1);

    private const int DefaultBatchSize = 50;

    /// <summary>Whether the delivery worker runs. The test host turns it off.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How long the worker waits between cycles when no commit signal woke it.</summary>
    public TimeSpan PollInterval { get; set; } = DefaultPollInterval;

    /// <summary>How many rows one cycle claims at most.</summary>
    public int BatchSize { get; set; } = DefaultBatchSize;

    /// <summary>How long a claimed row stays claimed; a crashed worker's lease expires.</summary>
    public TimeSpan LeaseDuration { get; set; } = DefaultLeaseDuration;
}
