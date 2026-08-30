using Microsoft.Extensions.DependencyInjection;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests;

public sealed class MoniPayApiTests(MoniPayApi api)
{
    [Fact]
    public void The_host_uses_the_controllable_test_clock()
    {
        DateTimeOffset initial = api.Time.GetUtcNow();

        try
        {
            api.Time.Advance(TimeSpan.FromMinutes(1));
            TimeProvider registered = api.Services.GetRequiredService<TimeProvider>();

            Assert.Same(api.Time, registered);
            Assert.Equal(initial.AddMinutes(1), registered.GetUtcNow());
        }
        finally
        {
            api.Time.Reset();
        }
    }
}
