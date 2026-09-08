using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MoniPay.Api.Http;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Api;

/// <summary>
/// The JSON:API transport contract, proven through the real host: the marked group rejects a bad
/// media type, an oversized body, invalid JSON and an invalid document before the endpoint runs,
/// and a valid document reaches it. The probe carries the expected resource type and a strict
/// attribute record, which is how a slice integrates with the generic envelope.
/// </summary>
public sealed class JsonApiTransportTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static readonly Uri Widget = new("/test/jsonapi/widgets", UriKind.Relative);
    private static readonly Uri WidgetRead = new("/test/jsonapi/widgets/read", UriKind.Relative);

    private const string ValidDocument = """{"data":{"type":"widgets","attributes":{"name":"W"}}}""";

    [Fact]
    public async Task A_valid_document_reaches_the_endpoint()
    {
        using HttpResponseMessage response = await PostAsync(ValidDocument);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public void The_host_throws_on_a_bad_request()
    {
        Assert.True(Api.Services.GetRequiredService<IOptions<RouteHandlerOptions>>().Value.ThrowOnBadRequest);
    }

    [Fact]
    public async Task A_bodyless_jsonapi_route_does_not_require_a_content_type()
    {
        using HttpResponseMessage response = await Client.GetAsync(WidgetRead, Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Malformed_json_is_malformed_json()
    {
        using HttpResponseMessage response = await PostAsync("""{"data":""");

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);

        Assert.Equal(MoniPayErrorTypes.MalformedJson.Urn, problem.Type);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("\"scalar\"")]
    [InlineData("{}")]
    [InlineData("""{"data":null}""")]
    [InlineData("""{"data":[]}""")]
    [InlineData("""{"data":{"attributes":{"name":"W"}}}""")]
    [InlineData("""{"data":{"type":"widgets"}}""")]
    [InlineData("""{"data":{"type":"widgets","attributes":[]}}""")]
    [InlineData("""{"data":{"type":"gadgets","attributes":{"name":"W"}}}""")]
    [InlineData("""{"data":{"type":"widgets","attributes":{"name":"W","extra":1}}}""")]
    [InlineData("""{"data":{"type":"widgets","attributes":{"name":"W"}},"included":[]}""")]
    public async Task An_invalid_document_is_a_document_error(string body)
    {
        using HttpResponseMessage response = await PostAsync(body);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);

        Assert.Equal(MoniPayErrorTypes.JsonApiDocumentInvalid.Urn, problem.Type);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("text/plain")]
    [InlineData("application/vnd.api+json; charset=utf-8")]
    [InlineData("application/vnd.api+json; foo=bar")]
    public async Task An_unsupported_content_type_is_rejected(string contentType)
    {
        using HttpResponseMessage response = await PostAsync(ValidDocument, contentType);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.UnsupportedMediaType, requireNoStore: false);

        Assert.Equal(MoniPayErrorTypes.UnsupportedMediaType.Urn, problem.Type);
    }

    [Fact]
    public async Task A_missing_content_type_with_a_body_is_rejected()
    {
        using HttpResponseMessage response = await PostAsync(ValidDocument, contentType: null);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.UnsupportedMediaType);

        Assert.Equal(MoniPayErrorTypes.UnsupportedMediaType.Urn, problem.Type);
    }

    [Theory]
    [InlineData("application/vnd.api+json; ext=\"https://example.test/ext\"")]
    [InlineData("application/vnd.api+json; profile=\"https://example.test/profile\"")]
    [InlineData("Application/VND.api+JSON")]
    public async Task An_allowed_parameter_or_mixed_case_is_accepted(string contentType)
    {
        using HttpResponseMessage response = await PostAsync(ValidDocument, contentType);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_body_of_exactly_the_limit_is_accepted()
    {
        using HttpResponseMessage response = await PostAsync(DocumentOfSize(JsonApiTransport.MaximumBodyBytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_body_one_byte_over_the_limit_is_rejected()
    {
        using HttpResponseMessage response = await PostAsync(DocumentOfSize(JsonApiTransport.MaximumBodyBytes + 1));

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.RequestEntityTooLarge);

        Assert.Equal(MoniPayErrorTypes.ContentTooLarge.Urn, problem.Type);
    }

    [Fact]
    public async Task An_unknown_length_body_over_the_limit_is_rejected()
    {
        using HttpResponseMessage response = await PostAsync(
            DocumentOfSize(JsonApiTransport.MaximumBodyBytes + 1),
            unknownLength: true);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.RequestEntityTooLarge);

        Assert.Equal(MoniPayErrorTypes.ContentTooLarge.Urn, problem.Type);
    }

    private async Task<HttpResponseMessage> PostAsync(
        string body,
        string? contentType = MoniPayMediaTypes.JsonApi,
        bool unknownLength = false)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, Widget)
        {
            Content = unknownLength
                ? new UnknownLengthContent(body)
                : new StringContent(body, Encoding.UTF8),
        };

        if (contentType is not null)
        {
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        }
        else
        {
            request.Content.Headers.ContentType = null;
        }

        return await Client.SendAsync(request, Cancellation);
    }

    private static string DocumentOfSize(int size)
    {
        const string prefix = "{\"data\":{\"type\":\"widgets\",\"attributes\":{\"name\":\"";
        const string suffix = "\"}}}";
        int padding = size - prefix.Length - suffix.Length;

        return prefix + new string('a', padding) + suffix;
    }

    private sealed class UnknownLengthContent(string body) : HttpContent
    {
        private readonly byte[] bytes = Encoding.UTF8.GetBytes(body);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(bytes).AsTask();
    }
}
