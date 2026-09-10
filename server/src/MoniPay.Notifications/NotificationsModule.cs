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

        OptionsBuilder<NotificationsOptions> notifications = services.AddOptions<NotificationsOptions>()
            .Bind(configuration.GetSection(NotificationsOptions.SectionName))
            .Validate(
                options => options.IsWithinBounds(),
                "The MoniPay:Notifications bounds are invalid: check the retention, the provider "
                + "timeout and the worker bounds.")
            .RequireKey(options => options.DataKeyBase64, NotificationsOptions.Keys.DataKeyBase64)
            .Validate(
                options => options.Email.HasValidBaseUrl(),
                $"The {NotificationsOptions.Keys.EmailBaseUrl} value is invalid: it must be an https base address.")
            .Validate(
                options => options.Email.HasValidApiKey(),
                $"The {NotificationsOptions.Keys.EmailApiKey} value is invalid: it must be a header-safe key.")
            .Validate(
                options => options.Email.HasValidFromAddress(),
                $"The {NotificationsOptions.Keys.EmailFromAddress} value is invalid: it must be a verified sender address.")
            .ValidateOnStart();

        AddSmsChannel(services, configuration, notifications);

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

        AddEmailChannel(services);

        return services;
    }

    private static void AddSmsChannel(
        IServiceCollection services,
        IConfiguration configuration,
        OptionsBuilder<NotificationsOptions> notifications)
    {
        NotificationsOptions.SmsOptions sms = new();
        configuration.GetSection(NotificationsOptions.Keys.SmsSection).Bind(sms);
        if (sms.IsRequested())
        {
            notifications.Validate(
                options => options.IsSmsConfigured(),
                $"The {NotificationsOptions.Keys.SmsBaseUrl}, {NotificationsOptions.Keys.SmsApiKey} "
                + $"and {NotificationsOptions.Keys.SmsSenderId} settings are invalid: the base URL must be "
                + "an absolute https URL without credentials, and the API key and sender ID must be set.");

            services.AddHttpClient<BirdSmsChannel>()
                .ConfigureHttpClient((serviceProvider, client) =>
                {
                    client.BaseAddress = serviceProvider
                        .GetRequiredService<IOptions<NotificationsOptions>>().Value.Sms?.NormalizedBaseUrl;
                    client.Timeout = BirdSmsChannel.HttpTimeout;
                    client.MaxResponseContentBufferSize = BirdSmsChannel.MaxResponseBufferBytes;
                })
                .ConfigurePrimaryHttpMessageHandler(BirdSmsChannel.CreatePrimaryHandler);
            services.AddKeyedTransient<INotificationChannel>(
                NotificationChannel.Sms,
                (serviceProvider, _) => serviceProvider.GetRequiredService<BirdSmsChannel>());
        }
    }

    private static void AddEmailChannel(IServiceCollection services)
    {
        services.AddHttpClient<BirdEmailChannel>()
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                NotificationsOptions options = serviceProvider.GetRequiredService<IOptions<NotificationsOptions>>().Value;
                client.BaseAddress = new Uri(options.Email.BaseUrl, UriKind.Absolute);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false })
            .RedactLoggedHeaders(["Authorization"]);
        services.AddKeyedScoped<INotificationChannel>(
            NotificationChannel.Email,
            (serviceProvider, _) => serviceProvider.GetRequiredService<BirdEmailChannel>());
    }
}
