using System.Text.Json.Serialization;

namespace MoniPay.Kernel.Http;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record JsonApiResourceIdentifier
{
    public required string Type { get; init; }

    public required string Id { get; init; }
}
