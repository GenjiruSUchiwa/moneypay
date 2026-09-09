using Xunit;

namespace MoniPay.Tests.Support;

/// <summary>Shared assertions for the no-store convention the credential routes carry.</summary>
public static class CacheAssertions
{
    /// <summary>Asserts the response forbids every cache, in both the modern and the legacy header.</summary>
    public static void AssertNoStore(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.True(response.Headers.TryGetValues("Pragma", out IEnumerable<string>? pragma));
        Assert.NotNull(pragma);
        Assert.Equal("no-cache", Assert.Single(pragma));
    }
}
