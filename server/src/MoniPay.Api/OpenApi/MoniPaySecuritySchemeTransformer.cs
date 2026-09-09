using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using MoniPay.Sessions.Security;

namespace MoniPay.Api.OpenApi;

/// <summary>The workflow credential schemes, declared once so the generated contract names them.</summary>
internal static class MoniPaySecuritySchemes
{
    public const string SignUp = SessionsSchemes.SignUp;

    public const string Registration = SessionsSchemes.Registration;
}

/// <summary>Adds the Sign-up and Registration HTTP schemes to the document components.</summary>
internal sealed class MoniPaySecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[MoniPaySecuritySchemes.SignUp] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = MoniPaySecuritySchemes.SignUp,
            Description = "The sign-up workflow credential: Authorization: SignUp <token>.",
        };
        document.Components.SecuritySchemes[MoniPaySecuritySchemes.Registration] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = MoniPaySecuritySchemes.Registration,
            Description = "The registration workflow credential: Authorization: Registration <token>.",
        };

        return Task.CompletedTask;
    }
}

/// <summary>
/// Publishes the workflow credentials as alternative security requirements on the endpoints that
/// require them. An anonymous endpoint keeps no requirement; an endpoint accepting either workflow
/// credential lists both, so the contract shows the OR.
/// </summary>
internal sealed class MoniPaySecurityOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyList<AuthorizeAttribute> authorize = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<AuthorizeAttribute>()
            .ToArray();
        if (authorize.Count == 0)
        {
            return Task.CompletedTask;
        }

        List<string> schemes = authorize
            .SelectMany(attribute => (attribute.AuthenticationSchemes ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.Ordinal)
            .Where(scheme =>
                string.Equals(scheme, MoniPaySecuritySchemes.SignUp, StringComparison.Ordinal)
                || string.Equals(scheme, MoniPaySecuritySchemes.Registration, StringComparison.Ordinal))
            .ToList();
        if (schemes.Count == 0)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        foreach (string scheme in schemes)
        {
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(scheme, context.Document)] = [],
            });
        }

        return Task.CompletedTask;
    }
}
