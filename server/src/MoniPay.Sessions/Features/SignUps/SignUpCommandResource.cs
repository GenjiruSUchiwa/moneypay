using System.Text.Json.Serialization;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;

namespace MoniPay.Sessions.Features.SignUps;

/// <summary>
/// Commands identify an existing sign-up through a required relationship. The route remains
/// authoritative; the document cannot select a different workflow.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record SignUpCommandResource<TAttributes>
{
    public required string Type { get; init; }

    public required TAttributes Attributes { get; init; }

    public required SignUpRelationships Relationships { get; init; }

    public void RequireMatch(SignUpId routeId)
    {
        if (Relationships?.SignUp?.Data is not { } identity
            || !string.Equals(identity.Type, SignUpResourceTypes.SignUps, StringComparison.Ordinal)
            || !Guid.TryParse(identity.Id, out Guid relatedId))
        {
            throw new RefusalException(MoniPayErrorTypes.JsonApiDocumentInvalid);
        }

        if (relatedId != routeId.Value)
        {
            throw new RefusalException(MoniPayErrorTypes.ResourceIdentityMismatch);
        }
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record SignUpRelationships
{
    public required SignUpRelationship SignUp { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record SignUpRelationship
{
    public required JsonApiResourceIdentifier Data { get; init; }

    public JsonApiLinks? Links { get; init; }
}
