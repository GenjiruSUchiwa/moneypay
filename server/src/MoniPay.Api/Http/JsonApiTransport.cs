using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MediaTypeWithQuality = System.Net.Http.Headers.MediaTypeWithQualityHeaderValue;

namespace MoniPay.Api.Http;

/// <summary>
/// The JSON:API transport rules the host enforces before minimal-API binding reads a body. A
/// request with a body must be <c>application/vnd.api+json</c> with only <c>ext</c> or
/// <c>profile</c> parameters; the body must be at most 8 KiB, must be valid JSON, and must have
/// the generic envelope (<c>data</c>, a string <c>data.type</c>, an object <c>data.attributes</c>).
/// <c>Accept</c> must allow a JSON:API success representation. Every rejection is a refusal the
/// host formats as Problem Details.
/// </summary>
internal static class JsonApiTransport
{
    /// <summary>The largest request body a JSON:API route accepts, in bytes.</summary>
    internal const int MaximumBodyBytes = 8 * 1024;

    private const string ApplicationWildcard = "application/*";
    private const string AnyWildcard = "*/*";
    private const string ExtensionParameter = "ext";
    private const string ProfileParameter = "profile";

    public static bool CanHaveBody(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        bool? feature = request.HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody;
        if (feature is { } canHaveBody)
        {
            return canHaveBody;
        }

        return request.ContentLength is > 0
            || request.Headers.ContainsKey(HeaderNames.TransferEncoding);
    }

    public static void ValidateContentType(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryParseContentType(request, out MediaTypeHeaderValue? contentType)
            || !IsJsonApiMediaType(contentType.MediaType.Value)
            || HasUnsupportedParameter(contentType))
        {
            throw Refusal(MoniPayErrorTypes.UnsupportedMediaType);
        }
    }

    public static void ValidateAccept(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Headers.TryGetValue(HeaderNames.Accept, out StringValues values) || values.Count == 0)
        {
            return;
        }

        if (BestMatch(ParseAccept(values)) is not { } match || match.Quality is <= 0)
        {
            throw Refusal(MoniPayErrorTypes.NotAcceptable);
        }
    }

    public static async Task<byte[]> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ContentLength is { } declared && declared > MaximumBodyBytes)
        {
            throw Refusal(MoniPayErrorTypes.ContentTooLarge);
        }

        using MemoryStream buffer = new();
        byte[] chunk = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            int read;
            while ((read = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
            {
                buffer.Write(chunk, 0, read);
                if (buffer.Length > MaximumBodyBytes)
                {
                    throw Refusal(MoniPayErrorTypes.ContentTooLarge);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }

        return buffer.ToArray();
    }

    public static void ValidateDocument(ReadOnlyMemory<byte> body, JsonApiResourceType? expected)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            throw Refusal(MoniPayErrorTypes.MalformedJson);
        }

        using (document)
        {
            if (!HasValidEnvelope(document.RootElement, expected))
            {
                throw Refusal(MoniPayErrorTypes.JsonApiDocumentInvalid);
            }
        }
    }

    private static bool TryParseContentType(
        HttpRequest request,
        [NotNullWhen(true)] out MediaTypeHeaderValue? contentType)
    {
        contentType = null;

        if (!request.Headers.TryGetValue(HeaderNames.ContentType, out StringValues values) || values.Count == 0)
        {
            return false;
        }

        return MediaTypeHeaderValue.TryParse(values.ToString(), out contentType);
    }

    private static bool HasUnsupportedParameter(MediaTypeHeaderValue contentType) =>
        contentType.Parameters.Any(parameter => !IsExtensionParameter(parameter.Name.Value));

    private static List<MediaTypeWithQuality> ParseAccept(StringValues values)
    {
        List<MediaTypeWithQuality> ranges = [];

        foreach (string? value in values)
        {
            if (value is null)
            {
                continue;
            }

            foreach (string token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (MediaTypeWithQuality.TryParse(token, out MediaTypeWithQuality? range) && range is not null)
                {
                    ranges.Add(range);
                }
            }
        }

        return ranges;
    }

    private static MediaTypeWithQuality? BestMatch(IReadOnlyList<MediaTypeWithQuality> ranges)
    {
        MediaTypeWithQuality? best = null;
        int bestSpecificity = -1;

        foreach (MediaTypeWithQuality range in ranges)
        {
            if (range.Parameters.Any(parameter => IsExtensionParameter(parameter.Name)))
            {
                continue;
            }

            int specificity = Specificity(range.MediaType);
            if (specificity > bestSpecificity)
            {
                best = range;
                bestSpecificity = specificity;
            }
        }

        return best;
    }

    private static int Specificity(string? mediaType)
    {
        if (IsJsonApiMediaType(mediaType))
        {
            return 2;
        }

        if (string.Equals(mediaType, ApplicationWildcard, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return string.Equals(mediaType, AnyWildcard, StringComparison.OrdinalIgnoreCase) ? 0 : -1;
    }

    private static bool IsExtensionParameter(string? name) =>
        name is not null
        && (name.Equals(ExtensionParameter, StringComparison.OrdinalIgnoreCase)
            || name.Equals(ProfileParameter, StringComparison.OrdinalIgnoreCase));

    private static bool IsJsonApiMediaType(string? mediaType) =>
        string.Equals(mediaType, MoniPayMediaTypes.JsonApi, StringComparison.OrdinalIgnoreCase);

    private static bool HasValidEnvelope(JsonElement root, JsonApiResourceType? expected)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!root.TryGetProperty("data", out JsonElement data) || data.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!TryResourceType(data, out string? type) || !HasAttributesObject(data))
        {
            return false;
        }

        return expected is null || string.Equals(type, expected.Value, StringComparison.Ordinal);
    }

    private static bool TryResourceType(JsonElement data, out string? type)
    {
        type = null;

        if (!data.TryGetProperty("type", out JsonElement value) || value.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        type = value.GetString();

        return !string.IsNullOrEmpty(type);
    }

    private static bool HasAttributesObject(JsonElement data) =>
        data.TryGetProperty("attributes", out JsonElement attributes)
        && attributes.ValueKind == JsonValueKind.Object;

    private static RefusalException Refusal(ProblemType type) => new(type);
}
