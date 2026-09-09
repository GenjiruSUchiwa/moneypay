using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MoniPay.Api.OpenApi;

internal sealed class StringEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        Type type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (type.IsEnum && schema.Enum is { Count: > 0 } && IsStringConverted(type))
        {
            schema.Type = JsonSchemaType.String;
        }

        return Task.CompletedTask;
    }

    private static bool IsStringConverted(Type type) =>
        type.GetCustomAttributes(typeof(JsonConverterAttribute), inherit: false)
            .OfType<JsonConverterAttribute>()
            .Any(attribute => attribute.ConverterType?.IsGenericType == true
                && attribute.ConverterType.GetGenericTypeDefinition() == typeof(JsonStringEnumConverter<>));
}
