using System.Text.Json.Serialization;

namespace MoniPay.Kernel.Http;

/// <summary>
/// JSON:API link members. <see cref="Self"/> identifies this representation;
/// <see cref="Related"/> identifies a related resource. Values are URI references, often relative.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record JsonApiLinks
{
    /// <summary>The URI of this representation, when the resource has one.</summary>
    public string? Self { get; init; }

    /// <summary>The URI of a related resource, when the relationship is a link only.</summary>
    public string? Related { get; init; }
}
