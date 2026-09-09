namespace MoniPay.Kernel.Http;

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
