using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MoniPay.Api;
using MoniPay.Api.Contracts;
using MoniPay.Api.Endpoints;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests;

public sealed class HealthEndpointsTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task Liveness_answers_without_touching_a_dependency()
    {
        // The personal-data key is mandatory configuration, not a dependency: the host refuses
        // to start without it, by design. Liveness still reaches no database or provider.
        await using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseTestKeys().UseTestPorts(Api));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(
            new Uri(HealthRoutes.Live, UriKind.Relative),
            Cancellation);

        response.EnsureSuccessStatusCode();
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);

        Assert.Equal(HealthResponse.Healthy, body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Readiness_checks_the_real_database()
    {
        using HttpResponseMessage response = await Client.GetAsync(
            new Uri(HealthRoutes.Ready, UriKind.Relative),
            Cancellation);

        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync(Cancellation);

        Assert.Equal(HealthStatus.Healthy.ToString(), body);
    }
}
