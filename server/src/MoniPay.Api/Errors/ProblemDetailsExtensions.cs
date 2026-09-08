using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MoniPay.Api.Errors;

/// <summary>
/// Wires the one error pipeline: the shared writer, the shared text resolver, and the single
/// exception handler. The writer is registered before the framework's default so it wins the
/// first-writer-wins selection, and it is always writable because an error body uses Problem
/// Details even when the client did not accept that media type.
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

        return services;
    }
}
