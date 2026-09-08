using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Api;

/// <summary>
/// The response-negotiation contract for a marked group: the client must accept a JSON:API success
/// representation, an explicit exclusion beats a wildcard, and the error body stays Problem
/// Details even when the client did not accept that media type.
/// </summary>
public sealed class ContentNegotiationTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static readonly Uri Widget = new("/test/jsonapi/widgets", UriKind.Relative);

    private const string ValidDocument = """{"data":{"type":"widgets","attributes":{"name":"W"}}}""";

    [Fact]
    public async Task An_absent_accept_header_is_acceptable()
    {
        using HttpResponseMessage response = await PostAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("*/*")]
    [InlineData("application/*")]
    [InlineData("application/vnd.api+json")]
    [InlineData("application/vnd.api+json, application/problem+json")]
    public async Task An_acceptable_range_is_accepted(string accept)
    {
        using HttpResponseMessage response = await PostAsync(accept);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("application/problem+json")]
    [InlineData("text/html")]
    [InlineData("application/vnd.api+json;q=0")]
    [InlineData("*/*;q=1, application/vnd.api+json;q=0")]
    [InlineData("application/vnd.api+json;q=0, */*;q=1")]
    public async Task An_unacceptable_accept_is_rejected_before_the_endpoint(string accept)
    {
        using HttpResponseMessage response = await PostAsync(accept);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.NotAcceptable);

        Assert.Equal(MoniPayErrorTypes.NotAcceptable.Urn, problem.Type);
    }

    [Fact]
    public async Task Repeated_accept_values_are_combined()
    {
        using HttpResponseMessage response = await PostAsync("text/html", "application/vnd.api+json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_406_body_is_problem_json_even_when_accept_excludes_it()
    {
        using HttpResponseMessage response = await PostAsync("text/html");

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.NotAcceptable);

        Assert.Equal(MoniPayErrorTypes.NotAcceptable.Urn, problem.Type);
    }

    private async Task<HttpResponseMessage> PostAsync(params string[] accept)
    {
        using HttpClient client = Api.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Post, Widget)
        {
            Content = new StringContent(ValidDocument, Encoding.UTF8),
        };

        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);
        foreach (string value in accept)
        {
            request.Headers.Accept.ParseAdd(value);
        }

        return await client.SendAsync(request, Cancellation);
    }
}
