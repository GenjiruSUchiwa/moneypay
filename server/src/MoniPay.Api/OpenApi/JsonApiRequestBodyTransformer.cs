using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using MoniPay.Kernel.Http;

namespace MoniPay.Api.OpenApi;

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
