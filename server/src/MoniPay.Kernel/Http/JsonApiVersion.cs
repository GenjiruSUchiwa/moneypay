namespace MoniPay.Kernel.Http;

/// <summary>The JSON:API version object of a success document.</summary>
public sealed record JsonApiVersion
{
    /// <summary>JSON:API 1.1, the version MoniPay success documents declare.</summary>
    public const string Current = "1.1";

    /// <summary>The JSON:API version string, always <see cref="Current"/> on documents this API writes.</summary>
    public string Version { get; init; } = Current;
}
