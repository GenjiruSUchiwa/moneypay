using MoniPay.Kernel;

namespace MoniPay.Sessions.Providers;

public interface IVerificationCodeSender
{
    Task EnqueueAsync(VerificationCodeMessage message, CancellationToken cancellationToken);

    Task<CodeDeliveryState?> GetLatestDeliveryAsync(SignUpId signUpId, CancellationToken cancellationToken);
}
