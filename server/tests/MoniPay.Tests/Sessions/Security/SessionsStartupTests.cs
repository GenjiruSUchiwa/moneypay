using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using MoniPay.Api;
using MoniPay.Sessions;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

public sealed class SessionsStartupTests(MoniPayApi api)
{
    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("dG9vLXNob3J0")]
    public void The_host_refuses_to_start_without_a_valid_verification_code_key(string keyBase64)
    {
        OptionsValidationException exception = StartWithKey(SessionsOptions.Keys.VerificationCodeKeyBase64, keyBase64);

        Assert.Contains(SessionsOptions.Keys.VerificationCodeKeyBase64, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("dG9vLXNob3J0")]
    public void The_host_refuses_to_start_without_a_valid_personal_data_key(string keyBase64)
    {
        OptionsValidationException exception = StartWithKey(SessionsOptions.Keys.PersonalDataKeyBase64, keyBase64);

        Assert.Contains(SessionsOptions.Keys.PersonalDataKeyBase64, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("dG9vLXNob3J0")]
    public void The_host_refuses_to_start_without_a_valid_signing_key(string keyBase64)
    {
        OptionsValidationException exception = StartWithKey(SessionsOptions.Keys.SigningKeyBase64, keyBase64);

        Assert.Contains(SessionsOptions.Keys.SigningKeyBase64, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(SessionsOptions.Keys.Issuer)]
    [InlineData(SessionsOptions.Keys.Audience)]
    public void The_host_refuses_to_start_without_a_token_issuer_and_an_audience(string keyName)
    {
        OptionsValidationException exception = StartWithKey(keyName, "");

        Assert.Contains(keyName, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(SessionsOptions.Keys.AccessTokenLifetime, "00:00:00")]
    [InlineData(SessionsOptions.Keys.AccessTokenLifetime, "-00:01:00")]
    [InlineData(SessionsOptions.Keys.RefreshTokenLifetime, "00:00:00")]
    [InlineData(SessionsOptions.Keys.ClockSkew, "00:00:00")]
    [InlineData(SessionsOptions.Keys.CleanupInterval, "00:00:00")]
    [InlineData(SessionsOptions.Keys.CleanupBatchSize, "0")]
    public void The_host_refuses_to_start_with_a_non_positive_lifetime(string keyName, string value)
    {
        OptionsValidationException exception = StartWithKey(keyName, value);

        Assert.Contains("MoniPay:Sessions bounds", exception.Message, StringComparison.Ordinal);
    }

    private OptionsValidationException StartWithKey(string keyName, string keyBase64)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(MoniPayEnvironments.Testing);
                builder.UseSetting("ConnectionStrings:MoniPay", api.ConnectionString);
                builder.UseSetting(MoniPayConfiguration.ApplyMigrationsOnStartup, "true");
                builder.UseTestKeys();
                builder.UseSetting(keyName, keyBase64);
            });

        return Assert.Throws<OptionsValidationException>(factory.CreateClient);
    }
}
