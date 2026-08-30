using System.Text.Json.Serialization;

namespace MoniPay.Kernel.Http;

/// <summary>
/// A JSON:API resource identifier: <c>type</c> and <c>id</c> as strings. Foreign keys belong here,
/// not inside <c>attributes</c>.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record JsonApiResourceIdentifier
{
    /// <summary>The resource type, a lowercase plural word such as <c>signups</c>.</summary>
    public required string Type { get; init; }

    /// <summary>The resource identifier. Never repeated inside <c>attributes</c>.</summary>
    public required string Id { get; init; }
}
