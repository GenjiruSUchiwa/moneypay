using System.Reflection;
using MoniPay.Api.Composition;
using MoniPay.Kernel;
using MoniPay.Notifications;
using MoniPay.Persistence;
using MoniPay.Sessions;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Ports;
using MoniPay.Sessions.Providers;
using MoniPay.Users;
using MoniPay.Users.Providers;
using MoniPay.Wallet;
using MoniPay.Wallet.Endpoints;

namespace MoniPay;

internal static class MoniPayModules
{
    internal static readonly Assembly[] ModuleAssemblies =
    [
        typeof(SessionsModule).Assembly,
        typeof(UsersModule).Assembly,
        typeof(WalletModule).Assembly,
        typeof(NotificationsModule).Assembly,
    ];

    public static IServiceCollection AddMoniPayModules(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddKernelModule(configuration)
            .AddDataModule(configuration, ModuleAssemblies)
            .AddSessionsModule(configuration)
            .AddUsersModule(configuration)
            .AddWalletModule(configuration)
            .AddNotificationsModule(configuration)
            .AddScoped<IUserProvisioning, UserProvisioningAdapter>()
            .AddScoped<IRegisteredPhoneLookup, RegisteredPhoneLookupAdapter>()
            .AddScoped<IVerificationCodeSender, VerificationCodeDeliveryAdapter>()
            .AddScoped<IWelcomeMessageSender, WelcomeMessageDeliveryAdapter>()
            .AddScoped<ISecurityAlertSender, SecurityAlertDeliveryAdapter>();
    public static WebApplication MapMoniPayModules(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapSessionsModule();
        app.MapUsersModule();
        app.MapWalletEndpoints();

        return app;
    }
}
