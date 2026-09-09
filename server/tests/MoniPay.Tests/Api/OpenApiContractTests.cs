using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MoniPay.Api;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Api;

/// <summary>
/// The published contract for a JSON:API endpoint. The endpoint declares the wildcard content
/// type so routing never answers the media-type rejection itself; the document the iOS client is
/// built from must name <c>application/vnd.api+json</c> alone.
/// </summary>
public sealed class OpenApiContractTests(MoniPayApi api)
{
    [Fact]
    public async Task A_jsonapi_endpoint_documents_the_jsonapi_request_media_type_alone()
    {
        using WebApplicationFactory<Program> host = api.CreateHost(
            builder => builder.UseSetting(MoniPayConfiguration.OpenApiEnabled, "true"));
        using HttpClient client = host.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/openapi/v1.json",
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        JsonElement document = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        JsonElement content = document
            .GetProperty("paths")
            .GetProperty("/test/jsonapi/widgets")
            .GetProperty("post")
            .GetProperty("requestBody")
            .GetProperty("content");

        Assert.Equal(
            [MoniPayMediaTypes.JsonApi],
            content.EnumerateObject().Select(entry => entry.Name));
        Assert.Equal(
            "#/components/schemas/JsonApiRequestOfJsonApiRequestResourceOfWidgetAttributes",
            content.GetProperty(MoniPayMediaTypes.JsonApi).GetProperty("schema").GetProperty("$ref").GetString());
    }

    [Fact]
    public async Task Workflow_security_is_or_for_read_and_absent_for_start()
    {
        using WebApplicationFactory<Program> host = api.CreateHost(
            builder => builder.UseSetting(MoniPayConfiguration.OpenApiEnabled, "true"));
        using HttpClient client = host.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/openapi/v1.json",
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        JsonElement document = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        JsonElement signUps = document.GetProperty("paths");
        JsonElement start = signUps.GetProperty("/signups").GetProperty("post");
        JsonElement read = signUps.GetProperty("/signups/{signUpId}").GetProperty("get");

        Assert.False(start.TryGetProperty("security", out _));

        string[] schemes = read
            .GetProperty("security")
            .EnumerateArray()
            .Select(requirement => requirement.EnumerateObject().Single().Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[] { SessionsSchemes.Registration, SessionsSchemes.SignUp }
                .OrderBy(name => name, StringComparer.Ordinal),
            schemes);
    }
}
