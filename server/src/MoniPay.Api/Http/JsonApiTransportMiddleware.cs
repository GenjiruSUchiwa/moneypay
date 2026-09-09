using Microsoft.AspNetCore.Http;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;

namespace MoniPay.Api.Http;

/// <summary>
/// Enforces the JSON:API transport contract on the groups that carry
/// <see cref="MoniPayConventions.JsonApi"/>, after routing and authentication but before
/// minimal-API binding reads a body, so a rejected request never reaches the endpoint. It reads a
/// bounded body once, validates the generic envelope and the expected resource type, then hands
/// the buffered body to the binder.
/// </summary>
internal sealed class JsonApiTransportMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!IsJsonApi(context))
        {
            await next(context);
            return;
        }

        bool hasBody = JsonApiTransport.CanHaveBody(context.Request);
        if (hasBody)
        {
            JsonApiTransport.ValidateContentType(context.Request);
        }

        // A route that takes no body is named by its path and its credential alone: a body, even a
        // valid JSON:API one, is an unsupported representation of this command and is refused
        // before it is read, so nothing in it is ever parsed.
        if (hasBody && context.GetEndpoint()?.Metadata.GetMetadata<JsonApiNoBody>() is not null)
        {
            throw new RefusalException(MoniPayErrorTypes.UnsupportedMediaType);
        }

        JsonApiTransport.ValidateAccept(context.Request);

        if (hasBody)
        {
            byte[] body = await JsonApiTransport.ReadBodyAsync(context.Request, context.RequestAborted);
            JsonApiTransport.ValidateDocument(body, context.GetEndpoint()?.Metadata.GetMetadata<JsonApiResourceType>());

            context.Request.Body = new MemoryStream(body, writable: false);
            context.Request.ContentLength = body.Length;
        }

        try
        {
            await next(context);
        }
        catch (BadHttpRequestException exception)
        {
            throw exception.StatusCode == StatusCodes.Status413RequestEntityTooLarge
                ? new RefusalException(MoniPayErrorTypes.ContentTooLarge)
                : new RefusalException(MoniPayErrorTypes.JsonApiDocumentInvalid);
        }
    }

    private static bool IsJsonApi(HttpContext context) =>
        context.GetEndpoint()?.Metadata.OfType<string>().Contains(MoniPayConventions.JsonApi) == true;
}
