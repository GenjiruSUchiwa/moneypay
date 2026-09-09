namespace MoniPay.Api;

public static class MoniPayConfiguration
{
    public const string SectionName = "MoniPay";

    public const string ApplyMigrationsOnStartup = $"{SectionName}:{nameof(ApplyMigrationsOnStartup)}";

    public const string OpenApiEnabled = $"{SectionName}:OpenApi:Enabled";

    public const string ForwardedHeadersKnownProxies = $"{SectionName}:ForwardedHeaders:KnownProxies";
}

public static class MoniPayEnvironments
{
    public const string Testing = nameof(Testing);
}
