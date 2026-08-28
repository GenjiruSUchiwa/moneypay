namespace MoniPay.Api;

/// <summary>
/// The configuration keys the host reads. A mistyped key does not fail: it silently returns the
/// default, so a production host would quietly skip its migrations or expose its OpenAPI
/// document. Declaring the keys turns that into a compile error.
/// Keys are colon-separated here and double-underscore-separated as environment variables:
/// <c>MoniPay:ApplyMigrationsOnStartup</c> is <c>MoniPay__ApplyMigrationsOnStartup</c>.
/// </summary>
public static class MoniPayConfiguration
{
    /// <summary>The section every MoniPay key sits under.</summary>
    public const string SectionName = "MoniPay";

    /// <summary>Apply the EF Core migrations when the host starts.</summary>
    public const string ApplyMigrationsOnStartup = $"{SectionName}:{nameof(ApplyMigrationsOnStartup)}";

    /// <summary>Serve the OpenAPI document. Defaults to on in Development only.</summary>
    public const string OpenApiEnabled = $"{SectionName}:OpenApi:Enabled";
}

/// <summary>Environment names the host branches on, beyond the ones ASP.NET Core defines.</summary>
public static class MoniPayEnvironments
{
    /// <summary>Set by the integration test harness so logs go to the test output.</summary>
    public const string Testing = nameof(Testing);
}
