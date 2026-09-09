using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;

namespace MoniPay.Sessions.Features.SignUps;

/// <summary>
/// The <c>signUp</c> relationship a command against an existing sign-up carries, and the one
/// check the transport cannot make for it: the route names the sign-up and the body must name the
/// same one. The route is the authority — a body never selects the workflow — so a difference is a
/// conflict, while a relationship that is missing, mistyped or not a well-formed identifier is a
/// malformed document.
/// </summary>
internal static class SignUpRelationships
{
    /// <summary>The relationship name, as the HTTP contract spells it.</summary>
    public const string SignUp = "signUp";

    public static void RequireMatch(
        SignUpId routeId,
        IReadOnlyDictionary<string, JsonApiRelationship>? relationships)
    {
        if (relationships is null
            || !relationships.TryGetValue(SignUp, out JsonApiRelationship? relationship)
            || relationship.Data is not { } identity
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
