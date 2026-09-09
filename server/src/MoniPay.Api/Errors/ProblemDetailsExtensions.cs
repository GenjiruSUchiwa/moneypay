using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MoniPay.Api.Errors;

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

        services.Configure<JsonOptions>(
            options => options.SerializerOptions.PropertyNameCaseInsensitive = false);

        return services;
    }
}
