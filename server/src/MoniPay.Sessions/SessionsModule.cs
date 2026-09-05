using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MoniPay.Sessions;

/// <summary>
/// The composition of the sign-up and session machinery: the module's own services, registered
/// by the module itself. It maps no route yet; the handlers arrive with their slices.
/// </summary>
public static class SessionsModule
{
    public static IServiceCollection AddSessionsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SessionsOptions>()
            .Bind(configuration.GetSection(SessionsOptions.SectionName))
            .Validate(
                options => options.IsWithinBounds(),
                "The MoniPay:Sessions bounds are invalid: check the country rules, the code length, "
                + "the lifetimes and the attempt and resend limits.")
            .ValidateOnStart();

        return services;
    }
}
