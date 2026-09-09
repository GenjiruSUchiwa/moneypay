namespace MoniPay.Sessions;

internal sealed class CleanupOptions
{
    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(10);

    public int BatchSize { get; set; } = 500;

    public bool IsWithinBounds() => Interval > TimeSpan.Zero && BatchSize >= 1;
}
