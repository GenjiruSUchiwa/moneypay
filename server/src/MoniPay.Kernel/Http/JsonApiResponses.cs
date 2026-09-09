namespace MoniPay.Kernel.Http;

/// <summary>
/// Builds the success document for a read of one resource: the resource as <c>data</c> with the
/// document-level <c>self</c> link repeating the resource's own. Every read endpoint answers the
/// same envelope, so it is built once here instead of once per slice.
/// </summary>
public static class JsonApiResponses
{
    public static JsonApiResponse<JsonApiResponseResource<TAttributes>> Document<TAttributes>(
        JsonApiResponseResource<TAttributes> resource)
        where TAttributes : notnull
    {
        ArgumentNullException.ThrowIfNull(resource);

        return new JsonApiResponse<JsonApiResponseResource<TAttributes>>
        {
            Data = resource,
            Links = new JsonApiLinks { Self = resource.Links?.Self },
        };
    }
}
