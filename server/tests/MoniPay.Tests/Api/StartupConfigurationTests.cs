using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MoniPay.Api;
using MoniPay.Kernel;
using MoniPay.Notifications;
using MoniPay.Sessions;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Api;

public sealed class StartupConfigurationTests(MoniPayApi api)
{
    private const string RequiredKey = NotificationsOptions.Keys.DataKeyBase64;

    private static readonly Uri Widget = new("/test/jsonapi/widgets", UriKind.Relative);

    [Fact]
    public void The_host_refuses_to_start_when_a_required_key_is_absent()
    {
        using WebApplicationFactory<Program> factory = api.CreateHost(
            builder => builder.UseSetting(RequiredKey, null));

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(factory.CreateClient);

        Assert.Contains(RequiredKey, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_host_refuses_to_start_with_a_malformed_supported_country()
    {
        using WebApplicationFactory<Program> factory = api.CreateHost(
            builder => builder.UseSetting($"{SessionsOptions.Keys.SupportedCountries}:0:LocalLength", "0"));

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(factory.CreateClient);

        Assert.Contains("MoniPay:Sessions bounds", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_host_starts_when_the_optional_previous_signing_key_is_empty()
    {
        using WebApplicationFactory<Program> factory = api.CreateHost(
            builder => builder.UseSetting(SessionsOptions.Keys.PreviousSigningKeyBase64, string.Empty));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            new Uri("/health", UriKind.Relative),
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public void The_configured_country_rules_bind_to_phone_rules()
    {
        using WebApplicationFactory<Program> factory = api.CreateHost();
        _ = factory.CreateClient();

        SessionsOptions sessions = factory.Services.GetRequiredService<IOptions<SessionsOptions>>().Value;

        Assert.Equal(
            [
                new CountryPhoneRule("237", 9),
                new CountryPhoneRule("225", 10),
                new CountryPhoneRule("221", 9),
                new CountryPhoneRule("241", 8),
                new CountryPhoneRule("243", 9),
                new CountryPhoneRule("229", 8),
            ],
            sessions.CountryRules);
    }

    [Fact]
    public void The_runtime_defaults_keep_startup_migrations_off_and_allow_every_host()
    {
        using WebApplicationFactory<Program> factory = api.CreateHost();
        _ = factory.CreateClient();

        IConfiguration configuration = factory.Services.GetRequiredService<IConfiguration>();

        Assert.False(configuration.GetValue(MoniPayConfiguration.ApplyMigrationsOnStartup, defaultValue: true));
        Assert.Equal("*", configuration["AllowedHosts"]);
    }

    [Fact]
    public async Task Kestrel_serves_a_normal_request_and_rejects_a_body_over_the_limit()
    {
        using WebApplicationFactory<Program> factory = api.CreateHost();
        factory.UseKestrel(port: 0);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage accepted = await TestWidgets.PostAsync(client, Widget, TestWidgets.ValidDocument);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        using HttpResponseMessage rejected = await TestWidgets.PostAsync(
            client,
            Widget,
            TestWidgets.DocumentOfSize(MoniPayRequestLimits.MaximumBodyBytes + 1),
            unknownLength: true);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, rejected.StatusCode);
    }
}
