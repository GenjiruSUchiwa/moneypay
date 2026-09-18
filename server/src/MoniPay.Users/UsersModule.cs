using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoniPay.Kernel;
using MoniPay.Kernel.Security;
using MoniPay.Persistence;
using MoniPay.Users.Features.Contact;
using MoniPay.Users.Features.CurrentUser;
using MoniPay.Users.Features.Registration;
using MoniPay.Users.Providers;
using MoniPay.Users.Security;

namespace MoniPay.Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<UsersOptions>()
            .Bind(configuration.GetSection(UsersOptions.SectionName))
            .RequireKey(options => options.PersonalDataKeyBase64, UsersOptions.Keys.PersonalDataKeyBase64)
            .ValidateOnStart();

        services.AddSingleton<UserPersonalDataProtector>();
        services.AddSingleton<UserLookupDigest>();
        services.AddSingleton<WelcomeMessageRenderer>();

        services.AddScoped(provider => new RegisterUserHandler(
            provider.GetRequiredService<MoniPayDbContext>(),
            provider.GetRequiredService<UserPersonalDataProtector>(),
            provider.GetRequiredService<UserLookupDigest>(),
            provider.GetRequiredService<IWelcomeMessageSender>(),
            provider.GetRequiredService<WelcomeMessageRenderer>(),
            provider.GetRequiredService<ILogger<RegisterUserHandler>>()));

        services.AddScoped(provider => new PhoneRegistrationLookup(
            provider.GetRequiredService<MoniPayDbContext>(),
            provider.GetRequiredService<UserLookupDigest>()));

        services.AddScoped(provider => new UserContactLookup(
            provider.GetRequiredService<MoniPayDbContext>(),
            provider.GetRequiredService<UserPersonalDataProtector>()));

        services.AddScoped<GetCurrentUserHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapUsersModule(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapGetCurrentUser();

        return routes;
    }
}
