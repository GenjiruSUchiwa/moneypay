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
    [Fact]
    public async Task Every_response_of_a_no_store_group_is_uncacheable()
    {
        using HttpClient client = Api.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/test/no-store", Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.True(response.Headers.TryGetValues("Pragma", out IEnumerable<string>? pragma));
        Assert.Equal("no-cache", Assert.Single(pragma!));
    }

    [Fact]
    public async Task A_group_without_the_marker_is_not_touched()
    {
        using HttpClient client = Api.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/test/secure", Cancellation);

        // 401, and not uncacheable: the convention marks the credential routes, not the host.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual("no-store", response.Headers.CacheControl?.ToString());
    }
}
