using System.Text.Json.Serialization;

namespace MoniPay.Kernel.Http;

/// <summary>
/// A JSON:API 1.1 request document. Each slice supplies its own <typeparamref name="TResource"/>.
/// Unknown members are rejected so a misspelled field cannot bind as empty.
/// </summary>
/// <typeparam name="TResource">The request resource object, typically a <see cref="JsonApiRequestResource{TAttributes}"/>.</typeparam>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record JsonApiRequest<TResource>
{
    /// <summary>The primary request resource. Required; omitting it is a malformed document.</summary>
    public required TResource Data { get; init; }
}

/// <summary>
/// A JSON:API request resource. <c>id</c> is not a member: create requests identify by
/// <c>type</c> and <c>attributes</c>, and commands against an existing resource use
/// <see cref="Relationships"/>.
/// </summary>
/// <typeparam name="TAttributes">The slice-owned attributes object.</typeparam>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record JsonApiRequestResource<TAttributes>
{
    /// <summary>The resource type, a lowercase plural word such as <c>signups</c>.</summary>
    public required string Type { get; init; }

    /// <summary>The slice-owned attributes. Resource identifiers do not belong here.</summary>
    public required TAttributes Attributes { get; init; }

    /// <summary>Named relationships, when the command targets an existing resource.</summary>
    public IReadOnlyDictionary<string, JsonApiRelationship>? Relationships { get; init; }
}
