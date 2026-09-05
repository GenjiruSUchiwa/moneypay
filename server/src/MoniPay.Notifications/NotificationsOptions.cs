using MoniPay.Kernel.Security;

namespace MoniPay.Notifications;

/// <summary>
/// The module's configuration, bound from <c>MoniPay:Notifications</c>. The data key protects
/// everything a message carries — recipient, subject, body — and is refused at startup rather
/// than at the first enqueue: a host that cannot encrypt must not come up.
/// </summary>
internal sealed class NotificationsOptions
{
    public const string SectionName = "MoniPay:Notifications";

    private static readonly TimeSpan DefaultRetention = TimeSpan.FromDays(30);
    private static readonly TimeSpan DefaultProviderTimeout = TimeSpan.FromSeconds(10);

    /// <summary>The 32-byte AES key protecting recipient, subject and body, base64-encoded.</summary>
    public string DataKeyBase64 { get; set; } = string.Empty;

    /// <summary>How long delivered, failed and expired rows stay before the purge removes them.</summary>
    public TimeSpan Retention { get; set; } = DefaultRetention;

    /// <summary>How the delivery worker claims rows. Read by the delivery slice.</summary>
    public WorkerOptions Worker { get; set; } = new();

    /// <summary>The per-call timeout a provider channel gets.</summary>
    public TimeSpan ProviderTimeout { get; set; } = DefaultProviderTimeout;

    /// <summary>The decoded data key. Valid only once the options are validated.</summary>
    public byte[] DataKey => Base64Key.Decode(DataKeyBase64);

    /// <summary>Reports whether the configured bounds would produce a workable delivery.</summary>
    public bool IsWithinBounds() =>
        Retention > TimeSpan.Zero
        && ProviderTimeout > TimeSpan.Zero
        && Worker.BatchSize >= 1
        && Worker.PollInterval > TimeSpan.Zero
        && Worker.LeaseDuration > TimeSpan.Zero;

    /// <summary>The configuration keys, declared so a mistyped key is a compile error.</summary>
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
