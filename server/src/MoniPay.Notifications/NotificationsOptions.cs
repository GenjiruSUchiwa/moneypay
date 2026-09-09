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

    public required SmsOptions Sms { get; set; }

    public byte[] DataKey => Base64Key.Decode(DataKeyBase64);

    public bool IsWithinBounds() =>
        Retention > TimeSpan.Zero
        && ProviderTimeout > TimeSpan.Zero
        && Worker.BatchSize >= 1
        && Worker.PollInterval > TimeSpan.Zero
        && Worker.LeaseDuration > TimeSpan.Zero;

    public bool IsSmsConfigured() => Sms is SmsOptions sms && sms.IsConfigured();

    public sealed class SmsOptions
    {
        public required Uri BaseUrl { get; set; }

        public string ApiKey { get; set; } = string.Empty;

        public string SenderId { get; set; } = string.Empty;

        public bool IsConfigured() =>
            BaseUrl is { IsAbsoluteUri: true, Scheme: "https", UserInfo: "" }
            && !string.IsNullOrWhiteSpace(ApiKey)
            && !string.IsNullOrWhiteSpace(SenderId);
    }

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

        private const string Sms = $"{SectionName}:{nameof(Sms)}";

        public const string SmsBaseUrl = $"{Sms}:{nameof(SmsOptions.BaseUrl)}";
        public const string SmsApiKey = $"{Sms}:{nameof(SmsOptions.ApiKey)}";
        public const string SmsSenderId = $"{Sms}:{nameof(SmsOptions.SenderId)}";
    }
}
