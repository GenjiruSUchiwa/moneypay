namespace MoniPay.Kernel.Http;

/// <summary>
/// HTTP media types for MoniPay success and error bodies. Clients send <see cref="Accept"/>.
/// Do not append a <c>charset</c> parameter to <see cref="JsonApi"/>.
/// </summary>
public static class MoniPayMediaTypes
{
    /// <summary>JSON:API 1.1 success request and response bodies.</summary>
    public const string JsonApi = "application/vnd.api+json";

    /// <summary>RFC 9457 Problem Details error bodies.</summary>
    public const string ProblemJson = "application/problem+json";

    /// <summary>The <c>Accept</c> value clients send: JSON:API success and Problem Details errors.</summary>
    public const string Accept = $"{JsonApi}, {ProblemJson}";
}
