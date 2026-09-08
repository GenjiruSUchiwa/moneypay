namespace MoniPay.Kernel.Http;

/// <summary>
/// The JSON:API resource type an endpoint expects in <c>data.type</c>. A slice attaches it as
/// endpoint metadata; the host transport check compares it before binding, because the generic
/// request envelope cannot know which resource a route owns.
/// </summary>
public sealed record JsonApiResourceType
{
    /// <summary>The expected lowercase plural resource type, such as <c>signups</c>.</summary>
    public string Value { get; }

    public JsonApiResourceType(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        Value = value;
    }
}
