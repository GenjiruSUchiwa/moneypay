using System.Text.Json.Serialization;

namespace MoniPay.Kernel.Http;

/// <summary>
/// A JSON:API 1.1 success document. Each slice supplies its own <typeparamref name="TResource"/>.
/// Error bodies are Problem Details, not this envelope.
/// </summary>
/// <typeparam name="TResource">The response resource object, typically a <see cref="JsonApiResponseResource{TAttributes}"/>.</typeparam>
public sealed record JsonApiResponse<TResource>
{
    /// <summary>The JSON:API version object. Serialized as <c>jsonapi</c>, the JSON:API member name.</summary>
    [JsonPropertyName("jsonapi")]
    public JsonApiVersion JsonApi { get; init; } = new();

    /// <summary>The primary response resource.</summary>
    public required TResource Data { get; init; }

    /// <summary>Document-level links, when the representation has a canonical URI.</summary>
    public JsonApiLinks? Links { get; init; }
}

/// <summary>
/// A JSON:API response resource. <see cref="Id"/> is a sibling of <see cref="Attributes"/>,
/// never a field inside it.
/// </summary>
/// <typeparam name="TAttributes">The slice-owned attributes object.</typeparam>
public sealed record JsonApiResponseResource<TAttributes>
{
    /// <summary>The resource type, a lowercase plural word such as <c>signups</c>.</summary>
    public required string Type { get; init; }

    /// <summary>The resource identifier, serialized as a string.</summary>
    public required string Id { get; init; }

    /// <summary>The slice-owned attributes. Resource identifiers do not belong here.</summary>
    public required TAttributes Attributes { get; init; }

    /// <summary>Named relationships to other resources.</summary>
    public IReadOnlyDictionary<string, JsonApiRelationship>? Relationships { get; init; }

    /// <summary>Resource-level links, typically <c>self</c>.</summary>
    public JsonApiLinks? Links { get; init; }
}
