using MoniPay.Kernel.Security;

namespace MoniPay.Notifications;

internal sealed class NotificationsOptions
{
    public const string SectionName = "MoniPay:Notifications";

    private static readonly TimeSpan DefaultRetention = TimeSpan.FromDays(30);
    private static readonly TimeSpan DefaultProviderTimeout = TimeSpan.FromSeconds(10);

    public string DataKeyBase64 { get; set; } = string.Empty;

    public TimeSpan Retention { get; set; } = DefaultRetention;

    public WorkerOptions Worker { get; set; } = new();

    public TimeSpan ProviderTimeout { get; set; } = DefaultProviderTimeout;

    public byte[] DataKey => Base64Key.Decode(DataKeyBase64);

    public bool IsWithinBounds() =>
        Retention > TimeSpan.Zero
        && ProviderTimeout > TimeSpan.Zero
        && Worker.BatchSize >= 1
        && Worker.PollInterval > TimeSpan.Zero
        && Worker.LeaseDuration > TimeSpan.Zero;

    public static class Keys
    {
        private const string Worker = $"{SectionName}:Worker";

        public const string DataKeyBase64 = $"{SectionName}:{nameof(DataKeyBase64)}";
        public const string Retention = $"{SectionName}:{nameof(Retention)}";
        public const string WorkerEnabled = $"{Worker}:{nameof(WorkerOptions.Enabled)}";
        public const string WorkerPollInterval = $"{Worker}:{nameof(WorkerOptions.PollInterval)}";
        public const string WorkerBatchSize = $"{Worker}:{nameof(WorkerOptions.BatchSize)}";
        public const string WorkerLeaseDuration = $"{Worker}:{nameof(WorkerOptions.LeaseDuration)}";
        public const string ProviderTimeout = $"{SectionName}:{nameof(ProviderTimeout)}";
    }
}
