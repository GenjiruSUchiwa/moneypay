using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MoniPay.Kernel.Http;

/// <summary>
/// The one way a slice answers with a JSON:API document: the envelope, the media type and the
/// status in a single call, so no endpoint hand-builds the triple and none of them can drift.
/// </summary>
public static class JsonApiResults
{
    public static IResult Json<TAttributes>(
        JsonApiResponseResource<TAttributes> resource,
        int statusCode = StatusCodes.Status200OK)
        where TAttributes : notnull =>
        TypedResults.Json(
            JsonApiResponses.Document(resource),
            contentType: MoniPayMediaTypes.JsonApi,
            statusCode: statusCode);
}
