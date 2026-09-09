namespace MoniPay.Kernel.Http;

public sealed record JsonApiVersion
{
    public const string Current = "1.1";

    public string Version { get; init; } = Current;
}
