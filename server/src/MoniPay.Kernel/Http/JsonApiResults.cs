using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MoniPay.Kernel.Http;

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
