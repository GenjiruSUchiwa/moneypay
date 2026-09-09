using System.Text.Json.Serialization;

namespace MoniPay.Kernel.Http;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record JsonApiRequest<TResource>
{
    public required TResource Data { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record JsonApiRequestResource<TAttributes>
{
    public required string Type { get; init; }

    public required TAttributes Attributes { get; init; }

    public IReadOnlyDictionary<string, JsonApiRelationship>? Relationships { get; init; }
}
