using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MoniPay.Api;
using MoniPay.Notifications;
using MoniPay.Notifications.Channels;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Notifications;

public sealed class NotificationsStartupTests(MoniPayApi api)
{
    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("dG9vLXNob3J0")]
    public void The_host_refuses_to_start_without_a_valid_data_key(string keyBase64)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(MoniPayEnvironments.Testing);
                builder.UseSetting("ConnectionStrings:MoniPay", api.ConnectionString);
                builder.UseTestKeys();
                MoniPayApi.UseNotificationTestSettings(builder);
                builder.UseSetting(NotificationsOptions.Keys.DataKeyBase64, keyBase64);
            });

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(factory.CreateClient);

        Assert.Contains(NotificationsOptions.Keys.DataKeyBase64, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_host_refuses_to_start_without_a_valid_email_base_url() =>
        AssertEmailSettingFails(NotificationsOptions.Keys.EmailBaseUrl, "not-a-url", NotificationsOptions.Keys.EmailBaseUrl);

    [Fact]
    public void The_host_refuses_to_start_without_a_safe_email_api_key() =>
        AssertEmailSettingFails(NotificationsOptions.Keys.EmailApiKey, "", NotificationsOptions.Keys.EmailApiKey);

    [Fact]
    public void The_host_refuses_to_start_without_a_valid_email_sender() =>
        AssertEmailSettingFails(NotificationsOptions.Keys.EmailFromAddress, "not-an-email", NotificationsOptions.Keys.EmailFromAddress);

    private void AssertEmailSettingFails(string setting, string value, string expectedKey)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(MoniPayEnvironments.Testing);
                builder.UseSetting("ConnectionStrings:MoniPay", api.ConnectionString);
                builder.UseTestKeys();
                builder.UseSetting(NotificationsOptions.Keys.WorkerEnabled, "false");
                builder.UseSetting(setting, value);
            });

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(factory.CreateClient);

        Assert.Contains(expectedKey, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_host_refuses_to_start_without_sms_settings()
    {
        using WebApplicationFactory<Program> factory = HostWithSmsOverride(
            api, NotificationsOptions.Keys.SmsBaseUrl, string.Empty);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(factory.CreateClient);

        Assert.Contains(NotificationsOptions.Keys.SmsBaseUrl, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(NotificationsOptions.Keys.SmsBaseUrl, "http://sms-tests.monipay.example")]
    [InlineData(NotificationsOptions.Keys.SmsBaseUrl, "sms-tests.monipay.example")]
    [InlineData(NotificationsOptions.Keys.SmsBaseUrl, "https://user:secret@sms-tests.monipay.example")]
    public void The_host_refuses_to_start_with_invalid_sms_settings(string key, string value)
    {
        using WebApplicationFactory<Program> factory = HostWithSmsOverride(api, key, value);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(factory.CreateClient);

        Assert.Contains(key, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(NotificationsOptions.Keys.SmsApiKey)]
    [InlineData(NotificationsOptions.Keys.SmsSenderId)]
    public void The_host_starts_without_sms_when_only_one_credential_is_set(string key)
    {
        using WebApplicationFactory<Program> factory = HostWithSmsOverride(api, key, string.Empty);
        _ = factory.CreateClient();

        Assert.Null(factory.Services.GetKeyedService<INotificationChannel>(NotificationChannel.Sms));
    }

    [Fact]
    public void A_validation_error_names_the_key_without_echoing_credentials()
    {
        using WebApplicationFactory<Program> factory = HostWithSmsOverride(
            api, NotificationsOptions.Keys.SmsBaseUrl, "https://user:secret@sms-tests.monipay.example");

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(factory.CreateClient);

        Assert.Contains(NotificationsOptions.Keys.SmsBaseUrl, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", exception.Message, StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> HostWithSmsOverride(MoniPayApi api, string key, string value) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(MoniPayEnvironments.Testing);
            builder.UseSetting("ConnectionStrings:MoniPay", api.ConnectionString);
            builder.UseSetting(NotificationsOptions.Keys.WorkerEnabled, "false");
            builder.UseTestKeys();
            builder.UseSetting(key, value);
        });
}
