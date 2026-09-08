using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Features.SignUps.Get;
using MoniPay.Sessions.Features.SignUps.ResendCode;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Features.SignUps.VerifyPhone;

namespace MoniPay.Tests.Support;

/// <summary>
/// Registers the sign-up handlers that depend on the ports. The module
/// registers the handlers itself once the endpoints need them;
/// until then, only the tests resolve them.
/// </summary>
public static class TestPorts
{
    public static IWebHostBuilder UseTestPorts(this IWebHostBuilder builder, MoniPayApi api)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(api);

        return builder.ConfigureServices(services =>
        {
            services.AddScoped<StartSignUpHandler>();
            services.AddScoped<GetSignUpHandler>();
            services.AddScoped<CreateVerificationCodeDeliveryHandler>();
            services.AddScoped<CreatePhoneVerificationHandler>();
            services.AddScoped<CreateSignUpCompletionHandler>();
        });
    }
}
