using Xunit;

namespace MoniPay.Tests.Support;

public static class CacheAssertions
{
    public static void AssertNoStore(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.True(response.Headers.TryGetValues("Pragma", out IEnumerable<string>? pragma));
        Assert.NotNull(pragma);
        Assert.Equal("no-cache", Assert.Single(pragma));
    }
}
