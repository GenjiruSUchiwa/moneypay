using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MoniPay.Api;
using MoniPay.Api.OpenApi;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Sessions.Features.SignUps;
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
        JsonElement document = await ReadOpenApiDocumentAsync();
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
        JsonElement document = await ReadOpenApiDocumentAsync();
        JsonElement signUps = document.GetProperty("paths");
        JsonElement start = signUps.GetProperty(SignUpRoutes.Group).GetProperty("post");
        // OpenAPI paths carry no route constraint, so the by-id template is rebuilt without ":guid".
        JsonElement read = signUps.GetProperty(SignUpRoutes.Group + "/{signUpId}").GetProperty("get");

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

    [Fact]
    public async Task The_refresh_route_is_anonymous_and_documents_both_sessions_schemas()
    {
        JsonElement document = await ReadOpenApiDocumentAsync();
        JsonElement refresh = document.GetProperty("paths").GetProperty(SessionRoutes.Refreshes).GetProperty("post");

        // The refresh credential travels in the body, so no scheme authorizes the operation.
        Assert.False(refresh.TryGetProperty("security", out _));
        Assert.Equal(
            [MoniPayMediaTypes.JsonApi],
            refresh.GetProperty("requestBody").GetProperty("content").EnumerateObject().Select(entry => entry.Name));
        Assert.Equal(
            "#/components/schemas/JsonApiRequestOfJsonApiRequestResourceOfCreateSessionRefreshAttributes",
            refresh.GetProperty("requestBody").GetProperty("content")
                .GetProperty(MoniPayMediaTypes.JsonApi).GetProperty("schema").GetProperty("$ref").GetString());
        Assert.Equal(
            "#/components/schemas/JsonApiResponseOfJsonApiResponseResourceOfSessionCredentialsAttributes",
            refresh.GetProperty("responses").GetProperty("200").GetProperty("content")
                .GetProperty(MoniPayMediaTypes.JsonApi).GetProperty("schema").GetProperty("$ref").GetString());
    }

    [Fact]
    public async Task The_session_routes_are_bearer_only_and_the_revocation_declares_no_content()
    {
        JsonElement document = await ReadOpenApiDocumentAsync();
        JsonElement current = document.GetProperty("paths").GetProperty(SessionRoutes.Group + SessionRoutes.Current);

        foreach (string method in new[] { "get", "delete" })
        {
            JsonElement requirement = Assert.Single(
                current.GetProperty(method).GetProperty("security").EnumerateArray());
            Assert.Equal(
                MoniPaySecuritySchemes.Bearer,
                Assert.Single(requirement.EnumerateObject()).Name);
        }

        JsonElement revoked = current.GetProperty("delete").GetProperty("responses").GetProperty("204");
        Assert.False(revoked.TryGetProperty("content", out _));

        JsonElement read = current.GetProperty("get").GetProperty("responses").GetProperty("200").GetProperty("content");
        Assert.Equal(
            "#/components/schemas/JsonApiResponseOfJsonApiResponseResourceOfReadSessionAttributes",
            read.GetProperty(MoniPayMediaTypes.JsonApi).GetProperty("schema").GetProperty("$ref").GetString());
    }

    [Theory]
    [InlineData("SignUpCommandResourceOfCreatePhoneVerificationAttributes")]
    public async Task A_sign_up_command_requires_its_named_relationship(string resourceSchema)
    {
        JsonElement schemas = (await ReadOpenApiDocumentAsync()).GetProperty("components").GetProperty("schemas");
        Assert.Contains("relationships", schemas.GetProperty(resourceSchema).GetProperty("required")
            .EnumerateArray().Select(member => member.GetString()));
        Assert.Contains("signUp", schemas.GetProperty(nameof(SignUpRelationships)).GetProperty("required")
            .EnumerateArray().Select(member => member.GetString()));
        Assert.Contains("data", schemas.GetProperty(nameof(SignUpRelationship)).GetProperty("required")
            .EnumerateArray().Select(member => member.GetString()));
    }

    [Theory]
    [InlineData(nameof(SignUpStatusValue))]
    [InlineData(nameof(CodeDeliveryValue))]
    public async Task A_string_enum_is_typed_as_a_string_in_the_contract(string schema)
    {
        JsonElement document = await ReadOpenApiDocumentAsync();

        JsonElement definition = document.GetProperty("components").GetProperty("schemas").GetProperty(schema);

        Assert.Equal("string", definition.GetProperty("type").GetString());
        Assert.NotEmpty(definition.GetProperty("enum").EnumerateArray());
    }

    private async Task<JsonElement> ReadOpenApiDocumentAsync()
    {
        using WebApplicationFactory<Program> host = api.CreateHost(
            builder => builder.UseSetting(MoniPayConfiguration.OpenApiEnabled, "true"));
        using HttpClient client = host.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/openapi/v1.json",
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }
}
