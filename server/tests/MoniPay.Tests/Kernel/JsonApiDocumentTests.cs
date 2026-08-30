using System.Text.Json;
using MoniPay.Kernel.Http;
using Xunit;

namespace MoniPay.Tests.Kernel;

public sealed class JsonApiDocumentTests
{
    private static readonly JsonSerializerOptions Web = JsonSerializerOptions.Web;

    [Fact]
    public void A_success_document_round_trips_with_string_type_and_id_and_jsonapi_1_1()
    {
        JsonApiResponse<JsonApiResponseResource<SampleAttributes>> document = CreateSampleDocument();

        string json = JsonSerializer.Serialize(document, Web);
        using JsonDocument parsed = JsonDocument.Parse(json);
        JsonElement root = parsed.RootElement;

        Assert.Equal(JsonApiVersion.Current, root.GetProperty("jsonapi").GetProperty("version").GetString());

        JsonElement data = root.GetProperty("data");
        Assert.Equal(JsonValueKind.String, data.GetProperty("type").ValueKind);
        Assert.Equal(JsonValueKind.String, data.GetProperty("id").ValueKind);
        Assert.Equal("signups", data.GetProperty("type").GetString());
        Assert.Equal("1f7d854a-3996-4a6b-8cc8-1fe3e83bc205", data.GetProperty("id").GetString());
        Assert.False(data.GetProperty("attributes").TryGetProperty("id", out _));

        JsonApiResponse<JsonApiResponseResource<SampleAttributes>>? roundTripped =
            JsonSerializer.Deserialize<JsonApiResponse<JsonApiResponseResource<SampleAttributes>>>(json, Web);

        Assert.NotNull(roundTripped);
        Assert.Equal(document.JsonApi, roundTripped.JsonApi);
        Assert.Equal(document.Links, roundTripped.Links);
        Assert.Equal(document.Data.Type, roundTripped.Data.Type);
        Assert.Equal(document.Data.Id, roundTripped.Data.Id);
        Assert.Equal(document.Data.Attributes, roundTripped.Data.Attributes);
        Assert.Equal(document.Data.Links, roundTripped.Data.Links);
        Assert.NotNull(roundTripped.Data.Relationships);
        JsonApiRelationship user = Assert.Contains("user", roundTripped.Data.Relationships);
        Assert.Equal("users", user.Data?.Type);
        Assert.Equal("22961289-422c-4b91-a5eb-1365b1e7c67b", user.Data?.Id);
        Assert.Equal("/users/me", user.Links?.Related);
    }

    [Fact]
    public void Member_names_serialize_as_camel_case()
    {
        string json = JsonSerializer.Serialize(CreateSampleDocument(), Web);
        using JsonDocument parsed = JsonDocument.Parse(json);
        JsonElement root = parsed.RootElement;
        JsonElement data = root.GetProperty("data");

        Assert.True(root.TryGetProperty("jsonapi", out _));
        Assert.True(root.TryGetProperty("data", out _));
        Assert.True(root.TryGetProperty("links", out _));
        Assert.True(data.TryGetProperty("type", out _));
        Assert.True(data.TryGetProperty("id", out _));
        Assert.True(data.TryGetProperty("attributes", out _));
        Assert.True(data.TryGetProperty("relationships", out _));
        Assert.True(data.TryGetProperty("links", out _));
        Assert.True(data.GetProperty("attributes").TryGetProperty("phone", out _));
        Assert.True(data.GetProperty("links").TryGetProperty("self", out _));
        Assert.True(
            data.GetProperty("relationships").GetProperty("user").GetProperty("links").TryGetProperty("related", out _));
        Assert.False(root.TryGetProperty("JsonApi", out _));
        Assert.False(root.TryGetProperty("jsonApi", out _));
    }

    [Fact]
    public void An_unknown_member_on_a_request_document_throws()
    {
        const string json =
            """{"data":{"type":"signups","attributes":{"phone":"237699123456"}},"included":[]}""";

        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<JsonApiRequest<JsonApiRequestResource<SampleAttributes>>>(json, Web));
    }

    [Fact]
    public void An_unknown_member_on_a_request_resource_throws()
    {
        const string json =
            """{"data":{"type":"signups","attributes":{"phone":"237699123456"},"lid":"not-id"}}""";

        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<JsonApiRequest<JsonApiRequestResource<SampleAttributes>>>(json, Web));
    }

    [Fact]
    public void A_document_missing_data_throws()
    {
        const string json = "{}";

        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<JsonApiRequest<JsonApiRequestResource<SampleAttributes>>>(json, Web));
    }

    [Fact]
    public void A_document_missing_data_type_throws()
    {
        const string json = """{"data":{"attributes":{"phone":"237699123456"}}}""";

        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<JsonApiRequest<JsonApiRequestResource<SampleAttributes>>>(json, Web));
    }

    [Fact]
    public void A_document_missing_data_attributes_throws()
    {
        const string json = """{"data":{"type":"signups"}}""";

        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<JsonApiRequest<JsonApiRequestResource<SampleAttributes>>>(json, Web));
    }

    [Fact]
    public void Timestamps_serialize_as_rfc3339_utc()
    {
        DateTimeOffset createdAt = new(2026, 8, 29, 19, 5, 0, TimeSpan.Zero);
        JsonApiResponseResource<TimestampAttributes> resource = new()
        {
            Type = "signups",
            Id = "1f7d854a-3996-4a6b-8cc8-1fe3e83bc205",
            Attributes = new TimestampAttributes { CreatedAt = createdAt },
        };

        string json = JsonSerializer.Serialize(resource, Web);
        using JsonDocument parsed = JsonDocument.Parse(json);
        JsonElement createdAtElement = parsed.RootElement.GetProperty("attributes").GetProperty("createdAt");

        Assert.Equal(JsonValueKind.String, createdAtElement.ValueKind);
        Assert.Equal("2026-08-29T19:05:00+00:00", createdAtElement.GetString());
    }

    [Fact]
    public void An_unknown_member_on_request_links_throws()
    {
        const string json =
            """
            {"data":{"type":"signups","attributes":{"phone":"237699123456"},
            "relationships":{"user":{"links":{"selph":"/users/me"}}}}}
            """;

        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<JsonApiRequest<JsonApiRequestResource<SampleAttributes>>>(json, Web));
    }

    private static JsonApiResponse<JsonApiResponseResource<SampleAttributes>> CreateSampleDocument() => new()
    {
        Data = new JsonApiResponseResource<SampleAttributes>
        {
            Type = "signups",
            Id = "1f7d854a-3996-4a6b-8cc8-1fe3e83bc205",
            Attributes = new SampleAttributes { Phone = "237699123456" },
            Relationships = new Dictionary<string, JsonApiRelationship>
            {
                ["user"] = new()
                {
                    Data = new JsonApiResourceIdentifier
                    {
                        Type = "users",
                        Id = "22961289-422c-4b91-a5eb-1365b1e7c67b",
                    },
                    Links = new JsonApiLinks { Related = "/users/me" },
                },
            },
            Links = new JsonApiLinks { Self = "/signups/1f7d854a-3996-4a6b-8cc8-1fe3e83bc205" },
        },
        Links = new JsonApiLinks { Self = "/signups/1f7d854a-3996-4a6b-8cc8-1fe3e83bc205" },
    };

    private sealed record SampleAttributes
    {
        public string Phone { get; init; } = string.Empty;
    }

    private sealed record TimestampAttributes
    {
        public DateTimeOffset CreatedAt { get; init; }
    }
}
