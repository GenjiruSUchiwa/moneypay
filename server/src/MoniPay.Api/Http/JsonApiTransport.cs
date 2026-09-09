using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;

namespace MoniPay.Api.Http;

/// <summary>
/// The JSON:API transport rules the host enforces before minimal-API binding reads a body. A
/// request with a body must be <c>application/vnd.api+json</c>; a <c>profile</c> parameter is
/// ignored and every other parameter is refused. The body must be at most 8 KiB, must be valid
/// JSON, and must have the generic envelope (<c>data</c>, a string <c>data.type</c>, an object
/// <c>data.attributes</c>). <c>Accept</c> must allow a JSON:API success representation. Every
/// rejection is a refusal the host formats as Problem Details.
///
/// This type is the only owner of those decisions. A JSON:API endpoint declares the wildcard
/// content type as well, so the routing matcher never answers a media-type rejection of its own
/// before the security middleware and this check have run.
///
/// The envelope member names are read case-sensitively, and the minimal-API binding is
/// configured the same way (<c>PropertyNameCaseInsensitive = false</c>), so the document this
/// type approves is the document the endpoint receives: JSON:API member names are case sensitive,
/// and a <c>Type</c> member must not be able to pass validation as <c>type</c> and then bind.
/// </summary>
internal static class JsonApiTransport
{
    /// <summary>The largest request body a JSON:API route accepts, in bytes.</summary>
    internal const int MaximumBodyBytes = 8 * 1024;

    private const string ApplicationWildcard = "application/*";
    private const string AnyWildcard = "*/*";
    private const string ExtensionParameter = "ext";
    private const string ProfileParameter = "profile";
    private const string QualityParameter = "q";

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

        IList<MediaTypeHeaderValue> ranges = AcceptedRanges(values);

        if (RefusesEveryJsonApiInstance(ranges)
            || BestMatch(ranges) is not { } match
            || match.Quality is <= 0)
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

    /// <summary>
    /// JSON:API 1.1 accepts <c>ext</c> and <c>profile</c> on the request media type and refuses
    /// every other parameter with a <c>415</c>. This host implements no extension, so an
    /// <c>ext</c> parameter always names an unsupported one and is refused with the rest; a
    /// <c>profile</c> is accepted and ignored, which is what the specification requires of a
    /// profile the server does not recognize.
    /// </summary>
    private static bool HasUnsupportedParameter(MediaTypeHeaderValue contentType) =>
        contentType.Parameters.Any(parameter => !IsProfileParameter(parameter.Name.Value));

    /// <summary>
    /// The accepted ranges, split by the framework's list parser: it is the only reader that gets
    /// a quoted parameter such as <c>text/plain;note=",application/vnd.api+json,"</c> right, where
    /// a comma split would invent a range the client never asked for. A header the parser refuses
    /// yields no range, so it names no acceptable representation.
    /// </summary>
    private static IList<MediaTypeHeaderValue> AcceptedRanges(StringValues values) =>
        MediaTypeHeaderValue.TryParseList(
            [.. values.OfType<string>()],
            out IList<MediaTypeHeaderValue>? ranges)
            ? ranges
            : [];

    /// <summary>
    /// Whether the header names the JSON:API media type only through instances the server must
    /// ignore: every instance carries a parameter other than <c>ext</c> or <c>profile</c>, or
    /// every instance asks for an extension this host does not implement. JSON:API 1.1 then
    /// requires a <c>406</c>, and a wildcard in the same header does not lift that requirement.
    /// </summary>
    private static bool RefusesEveryJsonApiInstance(IEnumerable<MediaTypeHeaderValue> ranges)
    {
        List<MediaTypeHeaderValue> jsonApi =
            [.. ranges.Where(range => IsJsonApiMediaType(range.MediaType.Value))];

        return jsonApi.Count > 0
            && (jsonApi.TrueForAll(HasForbiddenRepresentationParameter)
                || jsonApi.TrueForAll(HasExtensionParameter));
    }

    private static MediaTypeHeaderValue? BestMatch(IEnumerable<MediaTypeHeaderValue> ranges)
    {
        MediaTypeHeaderValue? best = null;
        int bestSpecificity = -1;

        foreach (MediaTypeHeaderValue range in ranges)
        {
            if (HasUnservableRepresentationParameter(range))
            {
                continue;
            }

            int specificity = Specificity(range.MediaType.Value);
            if (specificity > bestSpecificity
                || (specificity == bestSpecificity
                    && best is not null
                    && (range.Quality ?? 1) > (best.Quality ?? 1)))
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

    /// <summary>
    /// Whether the range carries a representation parameter this host cannot serve. The quality
    /// weight is not a representation parameter and a profile is ignored, so neither rules the
    /// range out; an extension is not implemented, so it does.
    /// </summary>
    private static bool HasUnservableRepresentationParameter(MediaTypeHeaderValue range) =>
        range.Parameters.Any(parameter =>
            !IsQualityParameter(parameter.Name.Value) && !IsProfileParameter(parameter.Name.Value));

    private static bool HasForbiddenRepresentationParameter(MediaTypeHeaderValue range) =>
        range.Parameters.Any(parameter =>
            !IsQualityParameter(parameter.Name.Value)
            && !IsExtensionParameter(parameter.Name.Value)
            && !IsProfileParameter(parameter.Name.Value));

    private static bool HasExtensionParameter(MediaTypeHeaderValue range) =>
        range.Parameters.Any(parameter => IsExtensionParameter(parameter.Name.Value));

    private static bool IsQualityParameter(string? name) =>
        name is not null && name.Equals(QualityParameter, StringComparison.OrdinalIgnoreCase);

    private static bool IsExtensionParameter(string? name) =>
        name is not null && name.Equals(ExtensionParameter, StringComparison.OrdinalIgnoreCase);

    private static bool IsProfileParameter(string? name) =>
        name is not null && name.Equals(ProfileParameter, StringComparison.OrdinalIgnoreCase);

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

        try
        {
            type = value.GetString();
        }
        catch (InvalidOperationException)
        {
            // A lone surrogate escape parses as a string token but cannot be decoded to UTF-16.
            // Decoding belongs to this boundary, so the document is refused as invalid here
            // instead of reaching the exception handler as an unexpected 500.
            return false;
        }

        return !string.IsNullOrEmpty(type);
    }

    private static bool HasAttributesObject(JsonElement data) =>
        data.TryGetProperty("attributes", out JsonElement attributes)
        && attributes.ValueKind == JsonValueKind.Object;

    private static RefusalException Refusal(ProblemType type) => new(type);
}
