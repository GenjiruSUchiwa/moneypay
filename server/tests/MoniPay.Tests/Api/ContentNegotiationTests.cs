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
/// representation, an explicit exclusion beats a wildcard, a range the client did not really send
/// never counts, and the error body stays Problem Details even when the client did not accept that
/// media type.
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
    [InlineData("text/html;q=0.9, application/vnd.api+json;q=0.1")]
    // No JSON:API instance, but the wildcard still accepts anything.
    [InlineData("text/html, */*")]
    // JSON:API 1.1: a profile the server does not recognize is ignored, not refused.
    [InlineData("application/vnd.api+json;profile=\"https://example.test/profile\"")]
    // One plain JSON:API instance is enough, even next to an unsupported extension.
    [InlineData("application/vnd.api+json;ext=\"https://example.test/ext\", application/vnd.api+json")]
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
    // The comma inside the quoted parameter is part of the parameter value, not a range
    // separator: this header asks for text/plain only.
    [InlineData("text/plain;note=\",application/vnd.api+json,\"")]
    // JSON:API 1.1: an instance modified by any other parameter is ignored, and a header that
    // leaves only such instances is refused even when it also carries a wildcard.
    [InlineData("application/vnd.api+json;foo=bar")]
    [InlineData("application/vnd.api+json;foo=bar, */*")]
    // No extension is implemented, so every extension instance is unsupported; the same rule
    // applies when they are the only JSON:API instances in the header.
    [InlineData("application/vnd.api+json;ext=\"https://example.test/ext\"")]
    [InlineData("application/vnd.api+json;ext=\"https://example.test/ext\", */*")]
    public async Task An_unacceptable_accept_is_rejected_before_the_endpoint(string accept)
    {
        using HttpResponseMessage response = await PostAsync(accept);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.NotAcceptable);

        Assert.Equal(MoniPayErrorTypes.NotAcceptable.Urn, problem.Type);
    }

    [Theory]
    [InlineData(";q=1", false)]
    [InlineData(";q=1", true)]
    [InlineData("", false)]
    [InlineData("", true)]
    public async Task Equal_specificity_ranges_use_the_highest_quality(string quality, bool reverse)
    {
        const string excluded = "application/vnd.api+json;profile=\"https://example.test/a\";q=0";
        string accepted = "application/vnd.api+json;profile=\"https://example.test/b\"" + quality;
        string accept = reverse ? $"{accepted}, {excluded}" : $"{excluded}, {accepted}";

        using HttpResponseMessage response = await PostAsync(accept);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task An_accept_header_the_parser_refuses_is_not_acceptable()
    {
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();

        using HttpRequestMessage request = new(HttpMethod.Post, Widget)
        {
            Content = new StringContent(ValidDocument, Encoding.UTF8),
        };

        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);

        // Sent without validation: the point is a header the framework's list parser rejects.
        Assert.True(request.Headers.TryAddWithoutValidation("Accept", "not a media type"));

        using HttpResponseMessage response = await client.SendAsync(request, Cancellation);

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

    /// <summary>
    /// Sends the exact ranges the case names. The test client carries the JSON:API
    /// <c>Accept</c> by default, so it is cleared first: an absent header must really be absent,
    /// and an explicit list must be the only thing the host reads.
    /// </summary>
    private async Task<HttpResponseMessage> PostAsync(params string[] accept)
    {
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();

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
