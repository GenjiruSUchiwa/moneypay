using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MoniPay.Tests.Support;

/// <summary>Shared assertions for successful JSON:API resource documents.</summary>
public static class JsonApiAssertions
{
    private const string JsonApiMediaType = "application/vnd.api+json";

    public static async Task<JsonElement> ReadJsonApiAsync(this HttpResponseMessage response)
    {
        Assert.InRange((int)response.StatusCode, 200, 299);
        Assert.Equal(JsonApiMediaType, response.Content.Headers.ContentType?.ToString());

        JsonElement document = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        JsonElement jsonApi = document.GetProperty("jsonapi");
        Assert.Equal("1.1", jsonApi.GetProperty("version").GetString());

        JsonElement data = document.GetProperty("data");
        Assert.Equal(JsonValueKind.String, data.GetProperty("type").ValueKind);
        Assert.Equal(JsonValueKind.String, data.GetProperty("id").ValueKind);

        JsonElement links = document.GetProperty("links");
        JsonElement self = links.GetProperty("self");
        Assert.Equal(JsonValueKind.String, self.ValueKind);
        string selfValue = self.GetString() ?? throw new Xunit.Sdk.XunitException(
            "JSON:API links.self must be a string.");
        Assert.NotEmpty(selfValue);

        return document;
    }

    public static async Task<JsonElement> ReadJsonApiAsync(
        this HttpResponseMessage response,
        string expectedType,
        string expectedId)
    {
        JsonElement document = await response.ReadJsonApiAsync();
        JsonElement data = document.GetProperty("data");

        Assert.Equal(expectedType, data.GetProperty("type").GetString());
        Assert.Equal(expectedId, data.GetProperty("id").GetString());

        return document;
    }

}
