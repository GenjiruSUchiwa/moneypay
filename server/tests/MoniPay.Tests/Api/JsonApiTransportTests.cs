using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MoniPay.Api;
using MoniPay.Api.Errors;
using MoniPay.Api.Hosting;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Api;

public sealed class JsonApiTransportTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static readonly Uri Widget = new("/test/jsonapi/widgets", UriKind.Relative);
    private static readonly Uri WidgetRead = new("/test/jsonapi/widgets/read", UriKind.Relative);
    private static readonly Uri WidgetSecure = new("/test/jsonapi/widgets/secure", UriKind.Relative);
    private static readonly Uri WidgetLimited = new("/test/jsonapi/widgets/limited", UriKind.Relative);

    [Fact]
    public async Task A_valid_document_reaches_the_endpoint_with_the_validated_values()
    {
        using HttpResponseMessage response = await PostAsync(TestWidgets.ValidDocument);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        TestProbes.ReceivedWidget received = await ReadWidgetAsync(response);
        Assert.Equal("widgets", received.Type);
        Assert.Equal("W", received.Name);
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
    [InlineData("""{"data":{"type":"widgets","attributes":{"name":"\uD800"}}}""")]
    public async Task An_invalid_document_is_a_document_error(string body)
    {
        using HttpResponseMessage response = await PostAsync(body);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);

        Assert.Equal(MoniPayErrorTypes.JsonApiDocumentInvalid.Urn, problem.Type);
    }

    [Theory]
    [InlineData("""{"data":{"type":"widgets","Type":"gadgets","attributes":{"name":"W"}}}""")]
    [InlineData("""{"data":{"type":"widgets","attributes":{"name":"W"}},"Data":null}""")]
    [InlineData("""{"data":{"type":"widgets","attributes":{"name":"W"},"Attributes":null}}""")]
    public async Task A_member_the_case_sensitive_binder_cannot_read_is_a_document_error(string body)
    {
        using HttpResponseMessage response = await PostAsync(body);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);

        Assert.Equal(MoniPayErrorTypes.JsonApiDocumentInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task A_repeated_member_resolves_the_way_the_binder_resolves_it()
    {
        using HttpResponseMessage response = await PostAsync(
            """{"data":{"type":"gadgets","type":"widgets","attributes":{"name":"W"}}}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TestProbes.ReceivedWidget received = await ReadWidgetAsync(response);
        Assert.Equal("widgets", received.Type);
    }

    [Fact]
    public async Task A_resource_type_that_cannot_be_decoded_is_a_document_error_not_a_500()
    {
        using HttpResponseMessage response = await PostAsync(
            """{"data":{"type":"\uD800","attributes":{"name":"W"}}}""");

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);

        Assert.Equal(MoniPayErrorTypes.JsonApiDocumentInvalid.Urn, problem.Type);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("text/plain")]
    [InlineData("text/html")]
    [InlineData("application/vnd.api+json; charset=utf-8")]
    [InlineData("application/vnd.api+json; foo=bar")]
    [InlineData("application/vnd.api+json; ext=\"https://example.test/ext\"")]
    public async Task An_unsupported_content_type_is_rejected_by_the_transport_check(string contentType)
    {
        int before = Api.Logs.Entries.Count;

        using HttpResponseMessage response = await PostAsync(TestWidgets.ValidDocument, contentType);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.UnsupportedMediaType);

        Assert.Equal(MoniPayErrorTypes.UnsupportedMediaType.Urn, problem.Type);
        Assert.Contains(
            Api.Logs.Entries.Skip(before),
            entry => entry.Category == typeof(MoniPayExceptionHandler).FullName
                && entry.Message.Contains("unsupported-media-type", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_missing_content_type_with_a_body_is_rejected()
    {
        using HttpResponseMessage response = await PostAsync(TestWidgets.ValidDocument, contentType: null);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.UnsupportedMediaType);

        Assert.Equal(MoniPayErrorTypes.UnsupportedMediaType.Urn, problem.Type);
    }

    [Theory]
    [InlineData("application/vnd.api+json; profile=\"https://example.test/profile\"")]
    [InlineData("Application/VND.api+JSON")]
    public async Task An_allowed_parameter_or_mixed_case_is_accepted(string contentType)
    {
        using HttpResponseMessage response = await PostAsync(TestWidgets.ValidDocument, contentType);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_body_of_exactly_the_limit_is_accepted()
    {
        using HttpResponseMessage response = await PostAsync(TestWidgets.DocumentOfSize(MoniPayRequestLimits.MaximumBodyBytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_body_one_byte_over_the_limit_is_rejected()
    {
        using HttpResponseMessage response = await PostAsync(TestWidgets.DocumentOfSize(MoniPayRequestLimits.MaximumBodyBytes + 1));

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.RequestEntityTooLarge);

        Assert.Equal(MoniPayErrorTypes.ContentTooLarge.Urn, problem.Type);
    }

    [Fact]
    public async Task An_unknown_length_body_over_the_limit_is_rejected()
    {
        using HttpResponseMessage response = await PostAsync(
            TestWidgets.DocumentOfSize(MoniPayRequestLimits.MaximumBodyBytes + 1),
            unknownLength: true);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.RequestEntityTooLarge);

        Assert.Equal(MoniPayErrorTypes.ContentTooLarge.Urn, problem.Type);
    }

    [Fact]
    public async Task Authentication_refuses_before_the_transport_check_on_a_jsonapi_route()
    {
        using HttpResponseMessage response = await TestWidgets.PostAsync(
            Client,
            WidgetSecure,
            "{ not json",
            contentType: "text/plain");

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized);

        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task Rate_limiting_refuses_before_the_transport_check_on_a_jsonapi_route()
    {
        using WebApplicationFactory<Program> host = Api.CreateHost(
            builder => builder.UseSetting(RateLimitOptions.Keys.StartPerHour, "1"));
        using HttpClient client = host.CreateClient();

        using HttpResponseMessage accepted = await TestWidgets.PostAsync(client, WidgetLimited, TestWidgets.ValidDocument);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        using HttpResponseMessage rejected = await TestWidgets.PostAsync(
            client,
            WidgetLimited,
            "{ not json",
            contentType: "text/plain");

        ProblemDetails problem = await rejected.ReadProblemAsync(HttpStatusCode.TooManyRequests);

        Assert.Equal(MoniPayErrorTypes.RateLimited.Urn, problem.Type);
    }

    private Task<HttpResponseMessage> PostAsync(
        string body,
        string? contentType = MoniPayMediaTypes.JsonApi,
        bool unknownLength = false) =>
        TestWidgets.PostAsync(Client, Widget, body, contentType, unknownLength);

    private static async Task<TestProbes.ReceivedWidget> ReadWidgetAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<TestProbes.ReceivedWidget>(TestContext.Current.CancellationToken)
        ?? throw new Xunit.Sdk.XunitException("The widget probe returned no resource.");
}
