using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Features.SignUps.Get;
using MoniPay.Sessions.Features.SignUps.ResendCode;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Features.SignUps.VerifyPhone;
using MoniPay.Sessions.Ports;
using MoniPay.Sessions.Providers;

namespace MoniPay.Tests.Support;

/// <summary>
/// Registers the fakes standing in for the two Sessions ports the host adapters will implement,
/// and the sign-up handlers that depend on them. The module registers the handlers itself once
/// the endpoints need them and the host provides the ports; until then, only the tests resolve
/// them, and registering them earlier would fail a Development host at startup.
/// </summary>
public static class TestPorts
{
    public static IWebHostBuilder UseTestPorts(this IWebHostBuilder builder, MoniPayApi api)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(api);

        return builder.ConfigureServices(services =>
        {
            services.AddSingleton<IVerificationCodeSender>(api.Sender);
            services.AddSingleton<IRegisteredPhoneLookup>(api.RegisteredPhones);

            // One use case each; scoped because each owns the request's DbContext transaction.
            services.AddScoped<StartSignUpHandler>();
            services.AddScoped<GetSignUpHandler>();
            services.AddScoped<CreateVerificationCodeDeliveryHandler>();
            services.AddScoped<CreatePhoneVerificationHandler>();
            services.AddScoped<CreateSignUpCompletionHandler>();
        });
    }
}
