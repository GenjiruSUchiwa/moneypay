namespace MoniPay.Sessions;

/// <summary>How the expired-credential sweep runs, bound from <c>MoniPay:Sessions:Cleanup</c>.</summary>
internal sealed class CleanupOptions
{
    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(10);

    public int BatchSize { get; set; } = 500;

    public bool IsWithinBounds() => Interval > TimeSpan.Zero && BatchSize >= 1;
}
