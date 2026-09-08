using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MoniPay.Kernel.Http;
using Xunit;

namespace MoniPay.Tests.Support;

/// <summary>Shared assertions for RFC 9457 Problem Details responses.</summary>
public static class ProblemDetailsAssertions
{
    public static Task<ProblemDetails> ReadProblemAsync(this HttpResponseMessage response) =>
        response.ReadProblemAsync(requireNoStore: true);

    public static async Task<ProblemDetails> ReadProblemAsync(
        this HttpResponseMessage response,
        bool requireNoStore)
    {
        Assert.Equal(MoniPayMediaTypes.ProblemJson, response.Content.Headers.ContentType?.ToString());

        if (requireNoStore)
        {
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            Assert.NotNull(response.Headers.RetryAfter);
        }

        JsonElement document = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        string type = RequiredString(document, "type");
        string title = RequiredString(document, "title");
        string instance = RequiredString(document, "instance");
        string traceId = RequiredString(document, "traceId");
        JsonElement statusElement = document.GetProperty("status");
        Assert.Equal(JsonValueKind.Number, statusElement.ValueKind);
        int status = statusElement.GetInt32();
        Assert.Equal((int)response.StatusCode, status);

        ProblemDetails problem = new()
        {
            Type = type,
            Title = title,
            Status = status,
            Instance = instance,
        };

        if (document.TryGetProperty("detail", out JsonElement detail))
        {
            Assert.Equal(JsonValueKind.String, detail.ValueKind);
            problem.Detail = detail.GetString();
        }

        problem.Extensions["traceId"] = traceId;

        if (document.TryGetProperty("errors", out JsonElement errors))
        {
            Assert.Equal(JsonValueKind.Array, errors.ValueKind);
            foreach (JsonElement error in errors.EnumerateArray())
            {
                Assert.Equal(JsonValueKind.Object, error.ValueKind);
                RequiredString(error, "pointer");
            }

            problem.Extensions["errors"] = errors.Clone();
        }

        return problem;
    }

    public static async Task<ProblemDetails> ReadProblemAsync(
        this HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        bool requireNoStore = true)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        return await response.ReadProblemAsync(requireNoStore);
    }

    private static string RequiredString(JsonElement document, string propertyName)
    {
        JsonElement property = document.GetProperty(propertyName);
        Assert.Equal(JsonValueKind.String, property.ValueKind);
        string? value = property.GetString();

        if (value is null || string.IsNullOrWhiteSpace(value))
        {
            throw new Xunit.Sdk.XunitException(
                $"Problem Details member '{propertyName}' must be a non-empty string.");
        }

        return value;
    }
}
