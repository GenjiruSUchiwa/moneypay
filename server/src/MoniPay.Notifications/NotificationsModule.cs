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

/// <summary>
/// The composition of the delivery module: the outbox producers enqueue into, the key that
/// protects what they enqueue, and the commit interceptors that wake the worker after the
/// commit. It maps no route — the status read is a method on <see cref="NotificationOutbox"/>,
/// consumed through a host adapter, and the worker that claims the rows arrives with the
/// delivery slice.
/// </summary>
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

        // No INotificationChannel is keyed yet: a placeholder that pretends to send is forbidden.
        // The processor resolves them with GetKeyedService and leaves the rows waiting until #101 / #102.
        services.AddScoped<NotificationProcessor>();
        services.AddHostedService<NotificationWorker>();
        services.AddScoped<NotificationPurger>();
        services.AddHostedService<NotificationPurgeWorker>();

        return services;
    }
}
