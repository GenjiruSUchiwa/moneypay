using System.Net;
using System.Net.Http.Headers;
using System.Text;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

/// <summary>
/// A route group that carries the no-store marker answers with response headers no shared cache
/// may ignore: credentials travel there. The middleware owns the policy, so the headers hold for a
/// normal response, for a request the security middleware short-circuits, for a transport refusal
/// and for a response the exception handler formats.
/// </summary>
public sealed class NoStoreConventionTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Theory]
    [InlineData("/test/no-store", HttpStatusCode.OK)]
    [InlineData("/test/signups/00000000-0000-0000-0000-000000000001", HttpStatusCode.Unauthorized)]
    [InlineData("/test/errors/unhandled", HttpStatusCode.InternalServerError)]
    public async Task Every_response_of_a_no_store_group_is_uncacheable(string route, HttpStatusCode expected)
    {
        using HttpClient client = Api.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(route, Cancellation);

        AssertUncacheable(response, expected);
    }

    [Fact]
    public async Task A_transport_refusal_of_a_no_store_group_is_uncacheable()
    {
        using HttpClient client = Api.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Post, "/test/jsonapi/widgets")
        {
            Content = new StringContent("""{"data":{"type":"widgets","attributes":{"name":"W"}}}""", Encoding.UTF8),
        };

        // The transport check's own refusal: a media type it reads and rejects.
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

        using HttpResponseMessage response = await client.SendAsync(request, Cancellation);

        AssertUncacheable(response, HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task Unrelated_string_metadata_does_not_enable_the_convention()
    {
        using HttpClient client = Api.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/test/secure", Cancellation);

        // 401, and not uncacheable: the convention marks the credential routes, not the host.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual("no-store", response.Headers.CacheControl?.ToString());
    }

    private static void AssertUncacheable(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.True(response.Headers.TryGetValues("Pragma", out IEnumerable<string>? pragma));
        Assert.NotNull(pragma);
        Assert.Equal("no-cache", Assert.Single(pragma));
    }
}
