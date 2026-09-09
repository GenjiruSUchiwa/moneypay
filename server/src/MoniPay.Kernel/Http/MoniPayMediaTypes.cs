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

    /// <summary>
    /// Every media type. A JSON:API endpoint declares it alongside <see cref="JsonApi"/> so the
    /// routing matcher does not answer a <c>415</c> of its own before the host's transport check
    /// runs; that check stays the only owner of the media-type decision, and the OpenAPI
    /// transformer publishes <see cref="JsonApi"/> alone.
    /// </summary>
    public const string AnyContentType = "*/*";

    /// <summary>The <c>Accept</c> value clients send: JSON:API success and Problem Details errors.</summary>
    public const string Accept = $"{JsonApi}, {ProblemJson}";
}
