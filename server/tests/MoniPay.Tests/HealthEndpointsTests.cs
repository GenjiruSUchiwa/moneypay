using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MoniPay.Api.Contracts;
using MoniPay.Api.Endpoints;
using Xunit;

namespace MoniPay.Tests;

public class HealthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public HealthEndpointsTests(WebApplicationFactory<Program> factory) => this.factory = factory;

    [Fact]
    public async Task Liveness_answers_without_touching_a_dependency()
    {
        // HealthRoutes.Live is what the Dockerfile HEALTHCHECK probes: it must answer even
        // with no database reachable, which is the case in this test.
        var response = await factory.CreateClient()
            .GetAsync(new Uri(HealthRoutes.Live, UriKind.Relative), TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HealthResponse.Healthy, body.GetProperty("status").GetString());
    }
}
