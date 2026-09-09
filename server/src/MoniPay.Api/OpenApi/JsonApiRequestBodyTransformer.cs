using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using MoniPay.Kernel.Http;

namespace MoniPay.Api.OpenApi;

/// <summary>
/// Publishes the JSON:API request body media type. A JSON:API endpoint also declares the wildcard
/// content type, so the routing matcher never answers a media-type rejection of its own and the
/// host's transport check stays the only owner of that decision. The contract the client reads
/// must name <c>application/vnd.api+json</c> alone, so this transformer replaces whatever the
/// generator inferred from the wildcard.
/// </summary>
internal sealed class JsonApiRequestBodyTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        if (operation.RequestBody?.Content is not { Count: > 0 } content
            || !IsJsonApi(context.Description.ActionDescriptor.EndpointMetadata))
        {
            return Task.CompletedTask;
        }

        OpenApiMediaType body = content.First().Value;
        content.Clear();
        content[MoniPayMediaTypes.JsonApi] = body;

        return Task.CompletedTask;
    }

    private static bool IsJsonApi(IList<object>? metadata) =>
        metadata is not null && metadata.OfType<string>().Contains(MoniPayConventions.JsonApi);
}
