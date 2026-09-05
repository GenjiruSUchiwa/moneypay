using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps.Get;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Providers;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Get;

public sealed class GetSignUpTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task The_view_reports_the_state_the_timing_and_the_latest_delivery()
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        Api.Sender.Delivery = CodeDeliveryState.Sent;

        try
        {
            SignUpView view = await Api.GetSignUpAsync(started.SignUpId);

            Assert.Equal(SignUpStatus.CodePending, view.Status);
            Assert.Equal(CodeDeliveryState.Sent, view.CodeDelivery);
            Assert.Equal(started.CodeExpiresAt, view.CodeExpiresAt);
            Assert.Equal(started.CanResendAt, view.CanResendAt);
            Assert.Equal(started.SignUpExpiresAt, view.SignUpExpiresAt);
        }
        finally
        {
            Api.Sender.Delivery = CodeDeliveryState.Queued;
        }
    }

    [Fact]
    public async Task An_unknown_sign_up_is_an_invalid_token()
    {
        await SignUpFlow.RefusedAsync(Api.GetSignUpAsync(SignUpId.New()), MoniPayErrorTypes.SignUpTokenInvalid);
    }
}
