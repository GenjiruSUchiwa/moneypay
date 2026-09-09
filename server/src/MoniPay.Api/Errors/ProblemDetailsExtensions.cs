using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MoniPay.Api.Errors;

/// <summary>
/// Wires the one error pipeline: the shared writer, the shared text resolver, and the single
/// exception handler. It also pins the two binding behaviours the transport contract depends on,
/// because a binding failure is an error this pipeline has to format. The writer is registered
/// before the framework's default so it wins the first-writer-wins selection, and it is always
/// writable because an error body uses Problem Details even when the client did not accept that
/// media type.
/// </summary>
internal static class ProblemDetailsExtensions
{
    public static IServiceCollection AddMoniPayProblemDetails(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<MoniPayProblemText>();
        services.AddSingleton<MoniPayProblemDetailsWriter>();
        services.AddSingleton<IProblemDetailsWriter>(
            provider => provider.GetRequiredService<MoniPayProblemDetailsWriter>());

        services.AddProblemDetails();
        services.AddExceptionHandler<MoniPayExceptionHandler>();

        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

        // JSON:API member names are case sensitive. JsonApiTransport reads the envelope that way,
        // so the binder must too: with the web default, `Type` would pass validation as `type`
        // and then overwrite it, and a trailing `Data: null` would blank a valid `data`.
        services.Configure<JsonOptions>(
            options => options.SerializerOptions.PropertyNameCaseInsensitive = false);

        return services;
    }
}
