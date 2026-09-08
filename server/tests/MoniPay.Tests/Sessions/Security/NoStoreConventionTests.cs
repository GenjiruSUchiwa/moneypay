using System.Net;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

/// <summary>
/// A route group that carries the no-store marker answers with response headers no shared cache
/// may ignore: credentials travel there.
/// </summary>
public sealed class NoStoreConventionTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Theory]
    [InlineData("/test/no-store", HttpStatusCode.OK)]
    [InlineData("/test/signups/00000000-0000-0000-0000-000000000001", HttpStatusCode.Unauthorized)]
    public async Task Every_response_of_a_no_store_group_is_uncacheable(string route, HttpStatusCode expected)
    {
        using HttpClient client = Api.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(route, Cancellation);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.True(response.Headers.TryGetValues("Pragma", out IEnumerable<string>? pragma));
        Assert.NotNull(pragma);
        Assert.Equal("no-cache", Assert.Single(pragma));
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
}
