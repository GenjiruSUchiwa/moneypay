using System.Text.Json.Serialization;

namespace MoniPay.Kernel.Http;

public sealed record JsonApiResponse<TResource>
{
    [JsonPropertyName("jsonapi")]
    public JsonApiVersion JsonApi { get; init; } = new();

    public required TResource Data { get; init; }

    public JsonApiLinks? Links { get; init; }
}

public sealed record JsonApiResponseResource<TAttributes> where TAttributes : notnull
{
    public required string Type { get; init; }

    public required string Id { get; init; }

    public required TAttributes Attributes { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, JsonApiRelationship>? Relationships { get; init; }

    public JsonApiLinks? Links { get; init; }
}
