using MoniPay.Kernel;

namespace MoniPay.Sessions.Features.SignUps.Complete;

/// <summary>
/// The one Users operation completion needs. The host implements it; Sessions never references
/// MoniPay.Users. Implementations join the caller's transaction and never commit.
/// </summary>
public interface IUserProvisioning
{
    Task<UserId> ProvisionAsync(ProvisionUserRequest request, CancellationToken cancellationToken);
}
