using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.Complete;

public interface IUserProvisioning
{
    Task<UserId> ProvisionAsync(ProvisionUserRequest request, CancellationToken cancellationToken);
}
