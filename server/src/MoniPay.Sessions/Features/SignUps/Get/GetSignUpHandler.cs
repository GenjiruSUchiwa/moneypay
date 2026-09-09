using MoniPay.Kernel;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Providers;

namespace MoniPay.Sessions.Features.SignUps.Get;

internal sealed class GetSignUpHandler(MoniPayDbContext database, IVerificationCodeSender sender)
{
    public async Task<SignUpView> HandleAsync(SignUpId signUpId, CancellationToken cancellationToken)
    {
        SignUp signUp = await database.ReadSignUpAsync(signUpId, cancellationToken).ConfigureAwait(false);
        CodeDeliveryState? delivery = await sender.GetLatestDeliveryAsync(signUpId, cancellationToken).ConfigureAwait(false);

        return new(signUp.Status, delivery, signUp.CodeExpiresAt, signUp.CanResendAt, signUp.ExpiresAt);
    }
}
