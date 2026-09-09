using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MoniPay.Api.OpenApi;

internal sealed class SortedOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (document.Paths is { Count: > 0 })
        {
            List<KeyValuePair<string, IOpenApiPathItem>> sorted = document.Paths
                .OrderBy(path => path.Key, StringComparer.Ordinal)
                .ToList();
            document.Paths.Clear();

            foreach ((string key, IOpenApiPathItem value) in sorted)
            {
                document.Paths.Add(key, value);
            }
        }

        return Task.CompletedTask;
    }
}
