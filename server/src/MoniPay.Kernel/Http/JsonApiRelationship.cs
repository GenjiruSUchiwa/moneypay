using System.Text.Json.Serialization;

namespace MoniPay.Kernel.Http;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record JsonApiRelationship
{
    public JsonApiResourceIdentifier? Data { get; init; }

    public JsonApiLinks? Links { get; init; }
}
