using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel;
using MoniPay.Kernel.Security;
using MoniPay.Notifications.Features.Deliver;
using MoniPay.Notifications.Features.Purge;
using MoniPay.Notifications.Persistence;
using MoniPay.Notifications.Security;
using MoniPay.Persistence;

namespace MoniPay.Notifications;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<NotificationsOptions>()
            .Bind(configuration.GetSection(NotificationsOptions.SectionName))
            .Validate(
                options => options.IsWithinBounds(),
                "The MoniPay:Notifications bounds are invalid: check the retention, the provider "
                + "timeout and the worker bounds.")
            .RequireKey(options => options.DataKeyBase64, NotificationsOptions.Keys.DataKeyBase64)
            .ValidateOnStart();

        services.AddSingleton<DeliverySignal>();
        services.AddSingleton<RecipientProtector>();
        services.AddSingleton<NotificationCommitInterceptor>();
        services.AddSingleton<IDbContextOptionsContributor>(serviceProvider =>
            serviceProvider.GetRequiredService<NotificationCommitInterceptor>());

        services.AddScoped(serviceProvider => new NotificationOutbox(
            serviceProvider.GetRequiredService<MoniPayDbContext>(),
            serviceProvider.GetRequiredService<RecipientProtector>(),
            serviceProvider.GetRequiredService<TimeProvider>()));

        services.AddScoped<NotificationProcessor>();
        services.AddHostedService<NotificationWorker>();
        services.AddScoped<NotificationPurger>();
        services.AddHostedService<NotificationPurgeWorker>();

        return services;
    }
}
