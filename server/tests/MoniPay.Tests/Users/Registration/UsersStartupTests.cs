using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using MoniPay.Api;
using MoniPay.Tests.Support;
using MoniPay.Users;
using Xunit;

namespace MoniPay.Tests.Users.Registration;

public sealed class UsersStartupTests(MoniPayApi api)
{
    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("dG9vLXNob3J0")]
    public void The_host_refuses_to_start_without_a_valid_personal_data_key(string keyBase64)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(MoniPayEnvironments.Testing);
                builder.UseSetting("ConnectionStrings:MoniPay", api.ConnectionString);
                builder.UseTestKeys();
                builder.UseSetting(UsersOptions.Keys.PersonalDataKeyBase64, keyBase64);
            });

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(factory.CreateClient);

        Assert.Contains(UsersOptions.Keys.PersonalDataKeyBase64, exception.Message, StringComparison.Ordinal);
    }
}
