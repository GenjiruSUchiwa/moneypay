using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Security;

namespace MoniPay.Api.OpenApi;

/// <summary>The credential schemes, declared once so the generated contract names them.</summary>
internal static class MoniPaySecuritySchemes
{
    public const string SignUp = SessionsSchemes.SignUp;

    public const string Registration = SessionsSchemes.Registration;

    /// <summary>The access JWT, the scheme of every session and user route.</summary>
    public const string Bearer = MoniPayHeaders.Bearer;
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
        document.Components.SecuritySchemes[MoniPaySecuritySchemes.Bearer] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = MoniPaySecuritySchemes.Bearer.ToLowerInvariant(),
            BearerFormat = "JWT",
            Description = "The access credential: Authorization: Bearer <jwt>.",
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
        operation.Security ??= [];

        // An authorized endpoint that names no workflow scheme is a bearer route: the access JWT
        // is the only credential the remaining policies accept.
        if (schemes.Count == 0)
        {
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(MoniPaySecuritySchemes.Bearer, context.Document)] = [],
            });

            return Task.CompletedTask;
        }

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
