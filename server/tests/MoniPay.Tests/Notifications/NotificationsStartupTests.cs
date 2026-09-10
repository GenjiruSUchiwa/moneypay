using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using MoniPay.Api;
using MoniPay.Notifications;
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
    [InlineData(NotificationsOptions.Keys.SmsApiKey, "")]
    [InlineData(NotificationsOptions.Keys.SmsApiKey, "   ")]
    [InlineData(NotificationsOptions.Keys.SmsSenderId, "")]
    [InlineData(NotificationsOptions.Keys.SmsSenderId, "   ")]
    public void The_host_refuses_to_start_with_invalid_sms_settings(string key, string value)
    {
        using WebApplicationFactory<Program> factory = HostWithSmsOverride(api, key, value);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(factory.CreateClient);

        Assert.Contains(key, exception.Message, StringComparison.Ordinal);
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
            builder.UseTestKeys();
            builder.UseSetting(key, value);
        });
}
