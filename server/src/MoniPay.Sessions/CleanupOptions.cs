namespace MoniPay.Sessions;

/// <summary>How the expired-credential sweep runs, bound from <c>MoniPay:Sessions:Cleanup</c>.</summary>
internal sealed class CleanupOptions
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(10);
    private const int DefaultBatchSize = 500;

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = DefaultInterval;

    public int BatchSize { get; set; } = DefaultBatchSize;

    public bool IsWithinBounds() => Interval > TimeSpan.Zero && BatchSize >= 1;
}
