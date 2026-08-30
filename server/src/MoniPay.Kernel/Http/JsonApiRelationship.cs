using System.Text.Json.Serialization;

namespace MoniPay.Kernel.Http;

/// <summary>
/// A JSON:API relationship object. Empty optional relationships set <see cref="Data"/> to
/// <see langword="null"/>.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record JsonApiRelationship
{
    /// <summary>The related resource identifier, or <see langword="null"/> when empty.</summary>
    public JsonApiResourceIdentifier? Data { get; init; }

    /// <summary>Links for the relationship, when the related URI is advertised.</summary>
    public JsonApiLinks? Links { get; init; }
}
