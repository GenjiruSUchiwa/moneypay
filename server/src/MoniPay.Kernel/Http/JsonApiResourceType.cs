namespace MoniPay.Kernel.Http;

public sealed record JsonApiResourceType
{
    public string Value { get; }

    public JsonApiResourceType(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        Value = value;
    }
}
