using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel.Http;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Api;

public sealed class JsonApiEndpointConventionTests(MoniPayApi api)
{
    [Fact]
    public void Every_jsonapi_endpoint_keeps_the_media_type_decision_out_of_routing()
    {
        List<Endpoint> marked =
        [
            .. api.Services.GetRequiredService<EndpointDataSource>().Endpoints
                .Where(endpoint => endpoint.Metadata.OfType<string>().Contains(MoniPayConventions.JsonApi)),
        ];

        Assert.NotEmpty(marked);
        Assert.All(marked, endpoint => Assert.True(
            AcceptsAnyContentType(endpoint),
            $"{endpoint.DisplayName} lets the routing matcher answer the media-type rejection."));
    }

    private static bool AcceptsAnyContentType(Endpoint endpoint)
    {
        IAcceptsMetadata? accepts = endpoint.Metadata.GetMetadata<IAcceptsMetadata>();

        return accepts is null
            || accepts.ContentTypes.Count == 0
            || accepts.ContentTypes.Contains(MoniPayMediaTypes.AnyContentType, StringComparer.OrdinalIgnoreCase);
    }
}
