using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Kernel.Security;
using MoniPay.Notifications.Channels;
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
            .Validate(
                options => options.IsSmsConfigured(),
                $"The {NotificationsOptions.Keys.SmsBaseUrl}, {NotificationsOptions.Keys.SmsApiKey} "
                + $"and {NotificationsOptions.Keys.SmsSenderId} settings are invalid: the base URL must be "
                + "an absolute https URL without credentials, and the API key and sender ID must be set.")
            .RequireKey(options => options.DataKeyBase64, NotificationsOptions.Keys.DataKeyBase64)
            .ValidateOnStart();

        services.AddHttpClient<BirdSmsChannel>()
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                NotificationsOptions.SmsOptions sms = serviceProvider
                    .GetRequiredService<IOptions<NotificationsOptions>>().Value.Sms;
                client.BaseAddress = sms.BaseUrl;
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        services.AddKeyedTransient<INotificationChannel>(
            NotificationChannel.Sms,
            (serviceProvider, _) => serviceProvider.GetRequiredService<BirdSmsChannel>());

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
