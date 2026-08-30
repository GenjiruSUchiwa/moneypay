using System.Net.Http.Json;
using System.Text.Json;
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
        HttpResponseMessage response = await Client.GetAsync(
            new Uri(HealthRoutes.Live, UriKind.Relative),
            Cancellation);

        response.EnsureSuccessStatusCode();
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);

        Assert.Equal(HealthResponse.Healthy, body.GetProperty("status").GetString());
    }
}
