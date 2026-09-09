using System.Text.Json.Serialization;

namespace MoniPay.Kernel.Http;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record JsonApiLinks
{
    public string? Self { get; init; }

    public string? Related { get; init; }
}
